using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Malaco.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Victoria;
using Victoria.Entities;

namespace Malaco.Modules
{
    public class Music : ModuleBase<SocketCommandContext>
    {
        private MusicService _musicService;
        public Music(MusicService musicService)
        {
            _musicService = musicService;
        }

        [Command("join")]
        public async Task Join()
        {
            var user = Context.User as SocketGuildUser;
            if (user.VoiceChannel == null)
            {
                await ReplyAsync($"{user.Mention}, Please connect to a Voice Channel to use Music Commands!");
                return;
            }
            else
            {
                await _musicService.ConnectAsync(user.VoiceChannel, Context.Channel as ITextChannel);
                await ReplyAsync($"Now Connected! (channel: {user.VoiceChannel.Name})");
            }
        }

        [Command("leave")]
        public async Task Leave()
        {
            var user = Context.User as SocketGuildUser;
            if (user.VoiceChannel == null)
            {
                await ReplyAsync("Please join the channel the bot is in to make it leave.");
            }
            else
            {
                await _musicService.LeaveAsync(user.VoiceChannel);
                await ReplyAsync($"Bot has now left {user.VoiceChannel.Name}");
            }
        }

        [Command("play")]
        public async Task Play([Remainder] string query)
        {
            var user = Context.User as SocketGuildUser;
            if (Context.Guild.GetUser(Context.Client.CurrentUser.Id).VoiceChannel == null)
            {
                await _musicService.ConnectAsync(user.VoiceChannel, Context.Channel as ITextChannel);
                await ReplyAsync($"Now Connected! (channel: {user.VoiceChannel.Name})");
            }
            LavaTrack info = await _musicService.PlayAsync(query, Context.Guild.Id);
            if (info != null)
            {
                if (_musicService._lavaSocketClient.GetPlayer(Context.Guild.Id).CurrentTrack.Id == info.Id)
                {
                    TimeSpan t = info.Length;
                    EmbedBuilder e = new EmbedBuilder()
                    {
                        Description = $"Now Playing - [{info.Title}]({info.Uri})\n\nDuration: `{t.ToString()}`\n\nSource: {info.Provider}",
                        ThumbnailUrl = await info.FetchThumbnailAsync()
                    };
                    await ReplyAsync(embed: e.Build());
                }
                else
                {
                    EmbedBuilder e = new EmbedBuilder()
                    {
                        Description = $"Added track to the Queue! - [{info.Title}]({info.Uri})\n\nDuration: `{info.Length.ToString()}`\n\nSource: {info.Provider}",
                        ThumbnailUrl = await info.FetchThumbnailAsync()
                    };
                    await ReplyAsync(embed: e.Build());
                }
            }
            else await ReplyAsync($"Query returned no results.");
        }

        [Command("stop"), RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task Stop()
        {
            await ReplyAsync(await _musicService.StopAsync(Context.Guild.Id));
        }

        [Command("skip")]
        public async Task Skip()
        {
            var _player = _musicService._lavaSocketClient.GetPlayer(Context.Guild.Id);
            LavaTrack oldtrack = _player.CurrentTrack;
            LavaTrack newtrack = await _musicService.SkipAsync(Context.Guild.Id);
            EmbedBuilder e = new EmbedBuilder()
            {
                Description = $"Skipped! [{oldtrack.Title}]({oldtrack.Uri})",
                ThumbnailUrl = await oldtrack.FetchThumbnailAsync()
            };
            await ReplyAsync(embed: e.Build());
            EmbedBuilder en = new EmbedBuilder()
            {
                Description = $"Now Playing - [{newtrack.Title}]({newtrack.Uri})",
                ThumbnailUrl = await newtrack.FetchThumbnailAsync()
            };
            await ReplyAsync(embed: en.Build());
        }

        [Command("volume"), Alias("vol")]
        public async Task Volume(int vol)
        {
            await ReplyAsync(await _musicService.SetVolumeAsync(vol, Context.Guild.Id));
        }


        [Command("pause"), Alias("pse")]
        public async Task Pause()
        {
            await ReplyAsync(await _musicService.PauseOrResumeAsync(Context.Guild.Id));
        }

        [Command("resume"), Alias("res")]
        public async Task Resume()
        {
            await ReplyAsync(await _musicService.ResumeAsync(Context.Guild.Id));
        }
    }
}
