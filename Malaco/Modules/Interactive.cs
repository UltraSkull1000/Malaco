using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Malaco.Modules
{
    public class Interactive : InteractiveBase
    {
        [Command("roll", RunMode = RunMode.Async)]
        public async Task Roll([Remainder] string query = "")
        {
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
                foreach (string x in query.Split(' ').Where(x=>x!=""))
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
                                    rollStringResults.Add($"`({roll})`");
                                    rollresults.Add(roll);
                                }
                                else
                                {
                                    List<int> rolls = Dice.RollMultipleDice(sides, count);
                                    rollStringResults.Add($"`({string.Join(", ", rolls)}) = {GetTotal(rolls)}`");
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
            }
            else
            {
                await ReplyAsync("Input was Invalid!");
            }
        }

        public string[] methods = new string[] { "d20", "3d6", "4d6", "2d6+6"};
        [Command("rollstats", RunMode=RunMode.Async), Alias("statrolls")]
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
    }
}
