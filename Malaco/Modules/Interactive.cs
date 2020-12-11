using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Malaco.Serializables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Malaco.Modules
{
    public class Interactive : InteractiveBase
    {
        [Command("roll", RunMode = RunMode.Async)]
        public async Task Roll([Remainder] string query = "")
        {
            UserData userdata = UserData.GetUserData(Context.User.Id);
            ServerData serverdata = ServerData.GetServerData(Context.Guild.Id);
            List<string> inp = new List<string>();
            query = query.ToLower();
            if (query == "")
            {
                query = $"1d20";
            }
            query = query.Replace("d ", "d");
            query = query.Replace(" d", "d");
            query = query.Replace("+", " ");
            query = query.Replace("-", " -");
            if (query.Contains(' '))
            {
                foreach (string x in query.Split(' ').Where(x => x != ""))
                {
                    if (x.Contains("d") || int.TryParse(x, out _)) inp.Add(x);
                }
            }
            else if (query.Contains("d")) inp.Add(query);

            if (inp.Count > 0)
            {
                List<string> rollStringResults = new List<string>();
                List<int> rollresults = new List<int>();
                int diecount = 0;
                foreach (string x in inp)
                {
                    switch (x)
                    {
                        case string n when (n.Contains('d')):
                            string[] split = n.Split('d');
                            if (split.Count() > 2)
                            {
                                await ReplyAsync($"You cant use 4-dimensional dice! That breaks all the laws of reality!");
                                return;
                            }
                            string pre = split.First();
                            string suf = split.Last();
                            if (int.TryParse(suf, out int sides))
                            {
                                int count = 1;
                                if (int.TryParse(pre, out int z)) count = z;
                                if (count == 1)
                                {
                                    int roll = Dice.RollDie(sides);

                                    userdata._rollTracker.AddToTracker(roll, sides);
                                    if (serverdata.TryGetTrackers(Context.User.Id, out var trackers))
                                    {
                                        foreach (var t in trackers)
                                        {
                                            t.AddToTracker(roll, sides);
                                            serverdata.SaveTracker(t);
                                        }
                                    }

                                    rollStringResults.Add($"`({roll})`");
                                    rollresults.Add(roll);
                                }
                                else
                                {
                                    int[] rolls = Dice.RollMultipleDice(sides, count).ToArray();

                                    userdata._rollTracker.AddToTracker(rolls, sides);
                                    if (serverdata.TryGetTrackers(Context.User.Id, out var trackers))
                                    {
                                        foreach (var t in trackers)
                                        {
                                            t.AddToTracker(rolls, sides);
                                            serverdata.SaveTracker(t);
                                        }
                                    }

                                    rollStringResults.Add($"`({string.Join(", ", rolls)}) = {GetTotal(rolls.ToList())}`");
                                    rollresults.AddRange(rolls);
                                }
                                diecount += count;
                            }
                            else
                            {
                                await ReplyAsync($"An error occurred while parsing a die.");
                                return;
                            }
                            break;
                        case string n when (int.TryParse(n, out int constant)):
                            rollStringResults.Add($"`[{constant}]`");
                            rollresults.Add(constant);
                            diecount++;
                            break;
                    }
                }
                int total = GetTotal(rollresults);
                switch (diecount)
                {
                    case int n when (n == 1):
                        await ReplyAsync($"{Context.User.Mention} rolled a {total}!");
                        break;
                    case int n when (n > 1):
                        await ReplyAsync($"{Context.User.Mention} rolled a total of {total}! {{{string.Join(" + ", rollStringResults)}}}");
                        break;
                }
                userdata.SaveData();
                serverdata.SaveData();
            }
            else
            {
                await ReplyAsync("Input was Invalid!");
            }
        }

        public string[] methods = new string[] { "d20", "3d6", "4d6", "2d6+6" };
        [Command("rollstats", RunMode = RunMode.Async), Alias("statrolls")]
        public async Task Stats()
        {
            await ReplyAsync($"How would you like to roll your stats? {string.Join(", ", methods)}");
            SocketMessage message = await NextMessageAsync(true, true, timeout: TimeSpan.FromSeconds(10));
            var m = "4d6";
            if (message != null) m = message.Content;
            else await ReplyAsync($"No message detected, defaulting to 4d6.");
            string ans = methods.OrderBy(x => LevenshteinDistance.Compute(x, message.Content)).First();
            List<string> results = GetRolls(ans);
            await ReplyAsync($"Your Stat Rolls: {string.Join(" ", results)}");
        }

        [Command("rollstats", RunMode = RunMode.Async), Alias("statrolls")]
        public async Task Stats([Remainder] string input)
        {
            string ans = methods.OrderBy(x => LevenshteinDistance.Compute(x, input)).First();
            List<string> results = GetRolls(ans);
            await ReplyAsync($"Your Stat Rolls: {string.Join(" ", results)}");
        }
        public List<string> GetRolls(string ans)
        {
            List<string> results = new List<string>();
            switch (ans)
            {
                case "d20":
                    for (int i = 0; i < 6; i++) { results.Add($"`{Dice.RollDie(20)}`"); }
                    break;
                case "3d6":
                    for (int i = 0; i < 6; i++)
                    {
                        List<int> rolls = Dice.RollMultipleDice(6, 3);
                        results.Add($"`{GetTotal(rolls)} ({string.Join(", ", rolls)})`");
                    }
                    break;
                case "4d6":
                    for (int i = 0; i < 6; i++)
                    {
                        List<int> rolls = Dice.RollMultipleDice(6, 4);
                        int drop = rolls.Min();
                        rolls.Remove(drop);
                        results.Add($"`{GetTotal(rolls)} ({string.Join(", ", rolls)}, {drop.ToString().Strikethrough()})`");
                    }
                    break;
                case "2d6+6":
                    for (int i = 0; i < 6; i++)
                    {
                        List<int> rolls = Dice.RollMultipleDice(6, 2);
                        results.Add($"`{GetTotal(rolls) + 6} ({string.Join(", ", rolls)}, [6])`");
                    }
                    break;
            }
            return results;
        }
        public int GetTotal(List<int> value)
        {
            int total = 0;
            foreach (int res in value) total += res;
            return total;
        }

        [Group("tracker"), Alias("trackers")]
        public class Trackers : InteractiveBase
        {
            public static async Task SetText(IUserMessage msg, string newText, Embed e = null)
            {
                await msg.ModifyAsync(x =>
                {
                    x.Content = newText;
                    if (e != null) x.Embed = e;
                });
            }

            [Command(), Alias("list")]
            public async Task ListTrackers()
            {
                ServerData data = ServerData.GetServerData(Context.Guild.Id);
                if (data._rollTrackers.Count > 0)
                {
                    EmbedBuilder e = new EmbedBuilder()
                    {
                        Title = "Serverwide Roll Trackers",
                        Color = new Color(200, 64, 64)
                    };
                    foreach (var r in data._rollTrackers)
                    {
                        int index = data._rollTrackers.IndexOf(r) + 1;
                        e.AddField($"{index}. {r.name}", $"To view this rolltracker, use the command `{User_Interface.prefix}tracker view {index}`");
                    }
                }
                else await ReplyAsync("No trackers to show!");
            }

            [Command("create", RunMode = RunMode.Async), RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task CreateTracker()
            {
                ServerData data = ServerData.GetServerData(Context.Guild.Id);
                RollTracker tracker = new RollTracker();
                var prompt = await ReplyAsync("Configuring Roll Tracker Creator, Please wait!");
                bool confirmed = false;
                Thread.Sleep(3000);
                while (!confirmed)
                {
                    await SetText(prompt, "What would you like to call this new Roll Tracker?", tracker.GetStatsEmbed(Context));
                    var answer = await NextMessageAsync();
                    if (answer != null)
                    {
                        tracker.name = answer.Content;
                        await answer.DeleteAsync();
                    }
                    else
                    {
                        await ReplyAsync("No Input Detected. Cancelling Roll Tracker Creation!");
                        return;
                    }

                    await SetText(prompt, "How would you like to describe this new Roll Tracker?", tracker.GetStatsEmbed(Context));
                    answer = await NextMessageAsync();
                    if (answer != null)
                    {
                        tracker.description = answer.Content;
                        await answer.DeleteAsync();
                    }
                    else
                    {
                        await ReplyAsync("No Input Detected. Cancelling Roll Tracker Creation!");
                        return;
                    }

                    await SetText(prompt, "What color would you like this new Roll Tracker's side bar to be?", tracker.GetStatsEmbed(Context));
                    answer = await NextMessageAsync();
                    if (answer != null)
                    {
                        tracker.colorHex = answer.Content;
                        await answer.DeleteAsync();
                    }
                    else
                    {
                        await ReplyAsync("No Input Detected. Cancelling Roll Tracker Creation!");
                        return;
                    }

                    await SetText(prompt, "Who will be included on this Roll Tracker? (Please Ping Users and/or a Role to include them on the tracker. The message will be promptly deleted.)", tracker.GetStatsEmbed(Context));
                    answer = await NextMessageAsync(timeout:TimeSpan.FromSeconds(120));
                    if (answer != null)
                    {
                        if (answer.MentionedUsers.Count > 0)
                        {
                            foreach (var u in answer.MentionedUsers) tracker.users.Add(u.Id);
                        }
                        if (answer.MentionedRoles.Count > 0)
                        {
                            foreach (var r in answer.MentionedRoles)
                            {
                                foreach(var u in r.Members) tracker.users.Add(u.Id);
                            }
                        }
                        tracker.users = tracker.users.Distinct().ToList();
                        await answer.DeleteAsync();
                    }
                    else
                    {
                        await ReplyAsync("No Input Detected. Cancelling Roll Tracker Creation!");
                        return;
                    }

                    await SetText(prompt, "Does this look correct? (Y/N)", tracker.GetStatsEmbed(Context));
                    answer = await NextMessageAsync();
                    if (answer != null)
                    {
                        if(answer.Content.ToLower() == "y")
                        {
                            data._rollTrackers.Add(tracker);
                            data.SaveData();
                            await ReplyAsync($"Saved! You can now view this tracker at any time with the command `mctracker view {data._rollTrackers.IndexOf(tracker)+1}`");
                            confirmed = true;
                        }
                        if (answer.Content.ToLower() == "n")
                        {
                            await SetText(prompt, "Restarting Configuration....", tracker.GetStatsEmbed(Context));
                            Thread.Sleep(3000);
                        }
                    }
                    else
                    {
                        await ReplyAsync("No Input Detected. Cancelling Roll Tracker Creation!");
                        return;
                    }
                }
            }
        }
    }
}
