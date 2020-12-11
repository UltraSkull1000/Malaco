using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using Malaco;
using Malaco.Serializables;

namespace Malaco.Modules
{
    public class General : ModuleBase<SocketCommandContext>
    {
        private const ulong ownerID = 252188962511257600;
        Discord.Color col = new Discord.Color(255, 110, 110);

        [Command("info", RunMode = RunMode.Async)]
        public async Task Info()
        {
            int usercount = 0;
            foreach (SocketGuild g in Context.Client.Guilds) usercount += g.MemberCount;

            EmbedFooterBuilder efb = new EmbedFooterBuilder()
            {
                IconUrl = Context.User.GetAvatarUrl(),
                Text = $"Requested by {Context.User.Username} | Use {User_Interface.prefix}help to get a list of commands! |"
            };
            EmbedBuilder e = new EmbedBuilder()
            {
                Title = Context.Client.CurrentUser.Username,
                Color = col,
                ImageUrl = Context.Client.CurrentUser.GetAvatarUrl(),
                Footer = efb
            };

            e.WithDescription($"{Context.Client.CurrentUser.Username} -- Created by UltraSkull1000#7470\n\n Looking to get a bot of your own? Check out his Fiverr page [here!](https://www.fiverr.com/ultraskull/make-custom-discord-bots)");

            await Context.Channel.SendMessageAsync("", embed: e.Build());
        }

        [Command("help")]
        public async Task Help()
        {

            EmbedFooterBuilder efb = new EmbedFooterBuilder()
            {
                Text = "{} Required Value | [] Optional Value | + Requires 'Manage Messages' Permission"
            };
            EmbedBuilder e = new EmbedBuilder()
            {
                Title = $"{Context.Client.CurrentUser.Username} - Help",
                Color = col,
                ThumbnailUrl = Context.Client.CurrentUser.GetAvatarUrl(),
                Footer = efb
            };
            EmbedFieldBuilder one = new EmbedFieldBuilder()
            {
                Name = "__Commands__",
                Value =
                $"`{User_Interface.prefix}info` - *Sends you information on the bot* \n" +
                $"`{User_Interface.prefix}help` - *Sends you a list of commands* \n" +
                $"`{User_Interface.prefix}stats` - *Sends you your dice roll averages* \n" +
                $"`{User_Interface.prefix}stats {{@user}}` - *Sends you that user's dice roll averages* \n" +
                $"`{User_Interface.prefix}roll {{query}}` - *Rolls your query* \n" +
                $"Dice Roll Query Examples: `d20`, `3d8`, `8d6+4`, `d4 +2`\n" +
                $"`{User_Interface.prefix}rollstats` - *Asks what method you would like, then Rolls your stats!* \n" +
                $"`{User_Interface.prefix}rollstats {{query}}` - *Rolls your stats using the specified method* \n" 
            };
            e.AddField(one);

            await ReplyAsync("", embed: e.Build());
        }

        [Command("stats")]
        public async Task Stats()
        {
            await ReplyAsync(embed: UserData.GetUserData(Context.User.Id)._rollTracker.GetStatsEmbed(Context));
        }
        [Command("stats")]
        public async Task Stats(SocketGuildUser user)
        {
            await ReplyAsync(embed: UserData.GetUserData(user.Id)._rollTracker.GetStatsEmbed(Context));
        }
    }
}
