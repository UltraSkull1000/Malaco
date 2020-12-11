using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Malaco
{
    public partial class User_Interface : Form
    {
        public static string botToken = "";
        public static string webhookURL = "";
        public static string Botname = "Malaco";
        public static string prefix = "mc";

        public string currentPlaying = "";
        public string[] messages;

        public static DiscordWebhookClient webClient;
        public Bot bot;

        List<SocketGuild> guilds;
        ulong selectedGuild;

        public User_Interface()
        {
            InitializeComponent();
            LoadAllElements();
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (bot != null)
            {
                if (MessageBox.Show("Are you sure that you would like to shut down your bot?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    e.Cancel = true;
                }
            }
        }

        private void SaveData()
        {
            using (StreamWriter s = new StreamWriter(File.Open(Environment.CurrentDirectory + "/token.txt", FileMode.Create)))
            {
                s.WriteLine(botToken);
                s.WriteLine(webhookURL);
                s.Dispose();
            }
            using (StreamWriter s = new StreamWriter(File.Open(Environment.CurrentDirectory + "/messages.txt", FileMode.Create)))
            {
                s.Write(statusMessageList.Text);
                s.Dispose();
            }
        }

        public void PostToConsole(string inp, System.Drawing.Color col = new System.Drawing.Color(), bool showTime = true)
        {
            if (!InvokeRequired)
            {
                console.SuspendLayout();
                if (col != new System.Drawing.Color()) console.SelectionColor = col;
                else console.SelectionColor = System.Drawing.Color.White;
                if (showTime) console.AppendText($"{DateTime.Now.ToString("s")}| {inp}\n");
                else console.AppendText($"{inp}\n");
                console.ScrollToCaret();
                console.ResumeLayout();
            }
            else
            {
                Invoke(new Action<string, System.Drawing.Color, bool>(PostToConsole), inp, col, showTime);
            }
        }
        public void PostToMessageBox(SocketMessage message)
        {
            if (!InvokeRequired)
            {
                messagingTextBox.SuspendLayout();
                messagingTextBox.SelectionColor = System.Drawing.Color.PowderBlue;
                messagingTextBox.AppendText($"{DateTime.Now.ToString("s")} | {message.Author.Username} > {message.Content}\n");
                messagingTextBox.ScrollToCaret();
                messagingTextBox.ResumeLayout();
            }
            else
            {
                Invoke(new Action<SocketMessage>(PostToMessageBox), message);
            }
        }
        public void UpdateElements(DiscordSocketClient client)
        {
            if (!InvokeRequired)
            {
                try
                {
                    if (client.CurrentUser != null)
                    {
                        if (client.CurrentUser.GetAvatarUrl() != null) botIcon.LoadAsync(client.CurrentUser.GetAvatarUrl());
                        else botIcon.LoadAsync(client.CurrentUser.GetDefaultAvatarUrl());

                        guildCount.Text = $"{client.Guilds.Count} Guilds";
                        int channelTotal = 0;
                        foreach (var g in client.Guilds)
                        {
                            channelTotal += g.Channels.Count;
                        }
                        channelCount.Text = $"{channelTotal} Channels";
                        statusCombo.SelectedIndex = statusCombo.Items.IndexOf(client.Status);
                        pingBox.Text = $"{client.Latency}ms";
                        if (currentPlaying != "") currentStatusMessage.Text = $"Playing {currentPlaying}";
                        usernameBox.Text = $"{client.CurrentUser.Username}#{client.CurrentUser.Discriminator} | <@{client.CurrentUser.Id}>";

                        guilds = client.Guilds.ToList();
                        guildView.DataSource = guilds;
                    }
                }
                catch { }
            }
            else
            {
                Invoke(new Action<DiscordSocketClient>(UpdateElements), client);
            }
        }

        void LoadAllElements()
        {
            console.Text = "";
            Text = Botname;
            statusCombo.DataSource = Enum.GetValues(typeof(UserStatus));

            guildView.DataSource = new List<SocketGuild>();
            guildView.SelectionChanged += GuildView_SelectionChanged;

            string[] columns = new string[] { "Name", "DefaultChannel", "Id", "MemberCount" };
            foreach (DataGridViewColumn column in guildView.Columns)
            {
                if (!columns.Contains(column.Name)) column.Visible = false;
                else
                {
                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                }
            }

            if (File.Exists(Environment.CurrentDirectory + "/token.txt"))
            {
                string[] tokenFile = File.ReadAllLines(Environment.CurrentDirectory + "/token.txt");
                botToken = tokenFile[0];
                webhookURL = tokenFile[1];
                tokenBox.Text = botToken;
                webhookUrlBox.Text = webhookURL;
            }

            if (File.Exists(Environment.CurrentDirectory + "/messages.txt"))
            {
                string statuses = File.ReadAllText(Environment.CurrentDirectory + "/messages.txt");
                statusMessageList.Text = statuses;
                messages = statuses.Split('\n');
            }

            statusMessageList.TextChanged += StatusMessageList_TextChanged;

            tokenBox.TextChanged += TokenBox_TextChanged;
            webhookUrlBox.TextChanged += WebhookUrlBox_TextChanged;

            if (webhookURL != "") webhooksEnabled.Checked = true;
            WebhooksEnabled_CheckedChanged(null, null);

            loginButton.Click += Login;
            webhooksEnabled.CheckedChanged += WebhooksEnabled_CheckedChanged;

            customInvite.Click += CustomInvite_Click;
            defaultInvite.Click += DefaultInvite_Click;
            allpermsInvite.Click += AllpermsInvite_Click;
            pcLink.Click += PcLink_Click;

            refreshGuilds.Click += RefreshGuilds_Click;
            leaveGuild.Click += LeaveGuild_Click;
            guildInvite.Click += GuildInvite_Click;

            statusCombo.SelectedIndexChanged += StatusCombo_SelectedIndexChanged;

            Lock();
        }

        private void GuildView_SelectionChanged(object sender, EventArgs e)
        {
            if (guildView.SelectedRows.Count > 0)
            {
                var g = bot._client.GetGuild(guilds[guildView.SelectedRows[0].Index].Id);
                guildName.Text = g.Name;
                var o = bot._client.GetUser(g.OwnerId);
                guildOwner.Text = $"{o.Username}#{o.Discriminator}";
                guildChannels.Text = $"{g.Channels.Count} Channels";
                UserCount.Text = $"{g.MemberCount} Members";
                selectedGuild = g.Id;
            }
        }

        private void GuildInvite_Click(object sender, EventArgs ev)
        {
            try
            {
                OpenBrowser(bot._client.GetGuild(selectedGuild).GetInvitesAsync().GetAwaiter().GetResult().First().Url);
            }
            catch (Exception e)
            {
                PostToConsole(e.Message, System.Drawing.Color.Red);
            }
        }

        private void LeaveGuild_Click(object sender, EventArgs ev)
        {
            if (selectedGuild != 0)
            {
                try
                {
                    bot._client.GetGuild(selectedGuild).LeaveAsync().GetAwaiter().GetResult();
                    guilds = bot._client.Guilds.ToList();
                    guildView.DataSource = guilds;
                }
                catch (Exception e)
                {
                    PostToConsole(e.Message, System.Drawing.Color.Red);
                }
            }
        }

        private void RefreshGuilds_Click(object sender, EventArgs e)
        {
            guilds = bot._client.Guilds.ToList();
            guildView.DataSource = guilds;
        }

        private void StatusMessageList_TextChanged(object sender, EventArgs e)
        {
            messages = statusMessageList.Text.Split('\n');
            SaveData();
        }

        void Lock()
        {
            customInvite.Enabled = false;
            defaultInvite.Enabled = false;
            allpermsInvite.Enabled = false;
            pinteger.Enabled = false;

            refreshGuilds.Enabled = false;
            leaveGuild.Enabled = false;
            guildInvite.Enabled = false;

            statusCombo.Enabled = false;
        }

        public void Unlock()
        {
            customInvite.Enabled = true;
            defaultInvite.Enabled = true;
            allpermsInvite.Enabled = true;
            pinteger.Enabled = true;
            pinteger.Text = "0";

            refreshGuilds.Enabled = true;
            leaveGuild.Enabled = true;
            guildInvite.Enabled = true;

            statusCombo.Enabled = true;
        }

        public void Login(object sender, EventArgs ev)
        {
            if (botToken != "")
            {
                try
                {
                    loginButton.Enabled = false;
                    bot = new Bot(this);
                    Task.Run(async () =>
                    {
                        await bot.StartAsync(botToken);
                    });
                }
                catch (Exception e)
                {
                    PostToConsole($"ERROR! {e.Message}", System.Drawing.Color.Red);
                    Lock();
                }
            }
            else
            {
                PostToConsole("Please Enter a Bot Token!", System.Drawing.Color.Red, false);
            }
        }

        private void StatusCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            bot._client.SetStatusAsync((UserStatus)statusCombo.SelectedIndex);
        }

        private void PcLink_Click(object sender, EventArgs e)
        {
            OpenBrowser("https://discordapi.com/permissions.html");
        }

        private void AllpermsInvite_Click(object sender, EventArgs e)
        {
            OpenBrowser($"https://discord.com/oauth2/authorize?client_id={bot._client.CurrentUser.Id}&scope=bot&permissions=2146958847");
        }

        private void DefaultInvite_Click(object sender, EventArgs e)
        {
            OpenBrowser($"https://discord.com/oauth2/authorize?client_id={bot._client.CurrentUser.Id}&scope=bot&permissions=0");
        }

        private void CustomInvite_Click(object sender, EventArgs e)
        {
            OpenBrowser($"https://discord.com/oauth2/authorize?client_id={bot._client.CurrentUser.Id}&scope=bot&permissions={pinteger.Text}");
        }

        private void WebhooksEnabled_CheckedChanged(object sender, EventArgs ev)
        {
            if (webhooksEnabled.Checked && webhookURL != "")
            {
                try
                {
                    webClient = new DiscordWebhookClient(webhookURL);
                    PostToConsole($"Successfully Connected to Webhook!", System.Drawing.Color.Lime);
                }
                catch (Exception e)
                {
                    PostToConsole($"ERROR! {e.Message}", System.Drawing.Color.Red);
                    webhooksEnabled.Checked = false;
                }
            }
            else webClient = null;
        }

        private void WebhookUrlBox_TextChanged(object sender, EventArgs ev)
        {
            webhookURL = webhookUrlBox.Text;
            if (webhooksEnabled.Checked)
            {
                try
                {
                    webClient = new DiscordWebhookClient(webhookURL);
                }
                catch (Exception e)
                {
                    PostToConsole($"ERROR! {e.Message}", System.Drawing.Color.Red);
                }
            }
            SaveData();
        }

        private void TokenBox_TextChanged(object sender, EventArgs e)
        {
            botToken = tokenBox.Text;
            SaveData();
        }

        //pulled from https://brockallen.com/2016/09/24/process-start-for-urls-on-net-core/ to solve urls not opening
        public static void OpenBrowser(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
