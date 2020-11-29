
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Malaco.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Victoria;

namespace Malaco
{
    public class CommandHandler
    {
        private CommandService _commandService;
        private readonly IServiceProvider _services;
        private DiscordSocketClient _client;

        public User_Interface _ui;

        public System.Timers.Timer updateTimer;
        public System.Timers.Timer playingTimer;

        public CommandHandler(DiscordSocketClient client, User_Interface ui)
        {
            _ui = ui;
            _ui.PostToConsole($"Connected Command Handler to Console UI...", System.Drawing.Color.CornflowerBlue);
            Random rand = new Random();
            _client = client;

            ui.PostToConsole($"Attaching Services...", System.Drawing.Color.CornflowerBlue);

            _commandService = new CommandService();
            _services = SetupServices();

            _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), services: _services);
            _services.GetRequiredService<MusicService>().InitializeAsync().GetAwaiter().GetResult();

            _ui.PostToConsole($"Success! Command Handler is Initialized! Starting Command Handler!", System.Drawing.Color.Lime);
            _client.MessageReceived += HandleCommandAsync;

            _client.Disconnected += Reconnect;
            _client.JoinedGuild += Joined;

            _ui.PostToConsole($"Creating Application Timers...", System.Drawing.Color.CornflowerBlue);
            playingTimer = new System.Timers.Timer(30000);
            playingTimer.AutoReset = true;
            playingTimer.Elapsed += PlayingTimer_Elapsed;
            playingTimer.Enabled = true;
            PlayingTimer_Elapsed(null, null);
            Thread.Sleep(500);
            updateTimer = new System.Timers.Timer(10000);
            updateTimer.AutoReset = true;
            updateTimer.Elapsed += UpdateTimer_Elapsed;
            updateTimer.Enabled = true;

            _ui.PostToConsole($"Success! All Systems are Up and Running!", System.Drawing.Color.Lime);
        }
        private IServiceProvider SetupServices()
            => new ServiceCollection()
            .AddSingleton(_client)
            .AddSingleton(_commandService)
            .AddSingleton<LavaRestClient>()
            .AddSingleton<LavaSocketClient>()
            .AddSingleton<MusicService>()
            .AddSingleton<InteractiveService>()
            .BuildServiceProvider();

        int index = 0;
        private void PlayingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Task.Run(async () =>
            {
                try
                {
                    string[] messages = _ui.messages.Where(x => x != "").ToArray();
                    if (messages.Count() == 0)
                    {
                        await _client.SetGameAsync("");
                    }
                    else
                    {
                        _ui.currentPlaying = messages[index] + $" | {User_Interface.prefix}help for commands!";
                        await _client.SetGameAsync(messages[index] + $" | {User_Interface.prefix}help for commands!");

                        index++;
                        if (index >= messages.Count()) index = 0;
                    }
                }
                catch (Exception e)
                {
                    _ui.PostToConsole(e.Message, System.Drawing.Color.Red);
                }
            }).GetAwaiter().GetResult();
        }
        private void UpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _ui.UpdateElements(_client);
        }

        private async Task Joined(SocketGuild newguild)
        {
            if (User_Interface.webClient != null)
            {
                EmbedBuilder e = new EmbedBuilder()
                {
                    Title = $"Joined {newguild.Name}",
                    ThumbnailUrl = newguild.IconUrl,
                    ImageUrl = newguild.IconUrl,
                    Color = Color.Blue
                };

                e.AddField("Name", newguild.Name, true);
                e.AddField("User Count", newguild.MemberCount, true);
                var o = newguild.Owner;
                e.AddField("Owner", $"{o.Username}#{o.Discriminator} ({o.Nickname})");

                List<string> mutualnewguildNames = new List<string>();
                foreach (SocketGuild g in newguild.Owner.MutualGuilds)
                {
                    mutualnewguildNames.Add(g.Name);
                }
                EmbedFieldBuilder six = new EmbedFieldBuilder
                {
                    Name = "Owner Shared Servers",
                    IsInline = true,
                    Value = string.Join(", ", mutualnewguildNames)
                };

                List<string> allroles = new List<string>();
                foreach (SocketRole r in newguild.Roles)
                {
                    if (!r.IsEveryone) allroles.Add(r.Name);
                }
                if (allroles.Count > 0)
                {
                    EmbedFieldBuilder seven = new EmbedFieldBuilder
                    {
                        Name = "Roles",
                        IsInline = true,
                        Value = String.Join(", ", allroles)
                    };
                }

                await User_Interface.webClient.SendMessageAsync(embeds: new Embed[] { e.Build() });
            }
            _ui.PostToConsole($"Joined Guild {newguild.Name} with {newguild.MemberCount}", System.Drawing.Color.Yellow);
        }

        private async Task Reconnect(Exception arg)
        {
            int retries = 0;
            _ui.PostToConsole(arg.Message, System.Drawing.Color.Red);
            Thread.Sleep(2000);
            while (_client.LoginState != LoginState.LoggedIn)
            {
                try
                {
                    await _client.LoginAsync(TokenType.Bot, User_Interface.botToken);
                    await _client.StartAsync();
                }
                catch (Exception e)
                {
                    _ui.PostToConsole($"WARNING!! An error occurred while logging in. Exception: {e.Message}", System.Drawing.Color.Red);
                    Thread.Sleep(100);
                }
            }
            while (_client.ConnectionState != ConnectionState.Connected && retries < 30)
            {
                _ui.PostToConsole(_client.ConnectionState.ToString(), showTime: false);
                Thread.Sleep(300);
                retries++;
                if (retries == 30)
                {
                    await _client.LogoutAsync();
                    _client.Dispose();
                    _ui.PostToConsole($"\n\n", showTime: false);
                    _ui.PostToConsole($"Could not restart command handler: Discord requested a full reconnect. Relogging!", System.Drawing.Color.Red);
                    _ui.Login(this, null);
                }
            }
            _ui.PostToConsole(_client.ConnectionState.ToString(), showTime: false);

            return;

        }

        private async Task HandleCommandAsync(SocketMessage s)
        {
            if (!s.Author.IsBot)
            {
                SocketUserMessage msg = s as SocketUserMessage;
                if (msg == null) return;
                var context = new SocketCommandContext(_client, msg);
                int argpos = 0;

                if (msg.HasStringPrefix(User_Interface.prefix, ref argpos) || msg.HasStringPrefix(UppercaseFirst(User_Interface.prefix), ref argpos) || msg.HasStringPrefix(User_Interface.prefix.ToUpper(), ref argpos))
                {
                    await context.Channel.TriggerTypingAsync();
                    if (!context.User.IsBot)
                    {
                        var result = await _commandService.ExecuteAsync(context, argpos, services: _services);

                        if (result.IsSuccess)
                        {
                            await LogMessageAsync(context, LogType.Default, result);
                        }
                        if (!result.IsSuccess)
                        {
                            await LogMessageAsync(context, LogType.Error, result);
                            await context.Channel.SendMessageAsync(result.ErrorReason + " " + result.Error.Value);
                            if (result.ErrorReason == "Unknown command.")
                            {
                                await context.Channel.SendMessageAsync($"Please use **{User_Interface.prefix}help** for a list of commands");
                            }
                        }
                    }
                    return;
                }
            }
        }
        static string UppercaseFirst(string s)
        {
            // Check for empty string.
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            // Return char and concat substring.
            return char.ToUpper(s[0]) + s.Substring(1);
        }

        public enum LogType
        {
            Default,
            Startup,
            Error,
        }

        public async Task<bool> LogMessageAsync(SocketCommandContext context = null, LogType type = LogType.Startup, IResult result = null)
        {
            string message = "";
            try
            {
                EmbedBuilder e = new EmbedBuilder();
                List<string> roles = new List<string>();

                switch (type)
                {
                    case LogType.Startup:
                        e.WithTitle($"{User_Interface.Botname} has Started.");
                        e.WithColor(Color.Green);
                        e.WithCurrentTimestamp();
                        break;
                    case LogType.Default:
                        e.WithTitle($"User {context.User.Username} has run a command.");
                        e.WithColor(new Color(52, 235, 128));

                        e.AddField("Message", $"```{context.Message.Content}```", false);
                        e.AddField("User Mention", $"{context.User.Mention}", true);
                        e.WithCurrentTimestamp();

                        _ui.PostToConsole($"{context.User.Username} ({context.User.Id}) >> Successful. [ {context.Message.Content} ]");
                        break;
                    case LogType.Error:
                        e.WithTitle($"User {context.User.Username} has encountered an error.");
                        e.WithColor(new Color(255, 0, 55));

                        e.AddField("Message", $"```{context.Message.Content}```", false);
                        e.AddField("Error", $"{result.ErrorReason}");
                        e.AddField("User Mention", $"{context.User.Mention}", true);
                        e.WithCurrentTimestamp();

                        _ui.PostToConsole($"{context.User.Username} ({context.User.Id}) > ERROR! {result.ErrorReason}", System.Drawing.Color.Red);
                        _ui.PostToConsole($"    [ {context.Message.Content} ]", System.Drawing.Color.Red, false);
                        break;
                }
                if (e.Title != null && User_Interface.webClient != null) await User_Interface.webClient.SendMessageAsync(text: message, embeds: new[] { e.Build() });
                return true;
            }
            catch (Exception e)
            {
                await context.Channel.SendMessageAsync($"{e.Message}{e.StackTrace}");
                return false;
            }
        }
    }
}



