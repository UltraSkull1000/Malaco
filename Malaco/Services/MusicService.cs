using Discord;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;
using Victoria;
using Victoria.Entities;

namespace Malaco.Services
{
    //Abstracted from https://github.com/DraxCodes/StreamMusicBot
    public class MusicService
    {
        public readonly LavaRestClient _lavaRestClient;
        public readonly LavaSocketClient _lavaSocketClient;
        private readonly DiscordSocketClient _client;
        public MusicService(LavaRestClient lavaRestClient, DiscordSocketClient client,
            LavaSocketClient lavaSocketClient)
        {
            _client = client;
            _lavaRestClient = lavaRestClient;
            _lavaSocketClient = lavaSocketClient;
        }

        public Task InitializeAsync()
        {
            _client.Ready += ClientReadyAsync;
            _lavaSocketClient.OnTrackFinished += TrackFinished;
            return Task.CompletedTask;
        }

        public async Task ConnectAsync(SocketVoiceChannel voiceChannel, ITextChannel textChannel)
        {
            await _lavaSocketClient.ConnectAsync(voiceChannel, textChannel);
        }

        public async Task LeaveAsync(SocketVoiceChannel voiceChannel) { await _lavaSocketClient.DisconnectAsync(voiceChannel); }

        public async Task<LavaTrack> PlayAsync(string query, ulong guildId)
        {
            LavaPlayer _player = _lavaSocketClient.GetPlayer(guildId);
            SearchResult results = await _lavaRestClient.SearchYouTubeAsync(query);

            if (results.LoadType == LoadType.NoMatches || results.LoadType == LoadType.LoadFailed)
            {
                return null;
            }

            LavaTrack track = results.Tracks.FirstOrDefault();

            if (_player.IsPlaying)
            {
                _player.Queue.Enqueue(track);
            }
            else
            {
                await _player.PlayAsync(track);
            }
            return track;
        }

        public async Task<string> StopAsync(ulong guildId)
        {
            var _player = _lavaSocketClient.GetPlayer(guildId);
            if (_player is null)
                return "Error with Player";
            await _player.StopAsync();
            return "Music Playback Stopped.";
        }

        public async Task<LavaTrack> SkipAsync(ulong guildId)
        {
            var _player = _lavaSocketClient.GetPlayer(guildId);
            if (_player is null || _player.Queue.Items.Count() is 0)
            {
                return null;
            }

            var oldTrack = _player.CurrentTrack;
            await _player.SkipAsync();
            return _player.CurrentTrack;
        }

        public async Task<string> SetVolumeAsync(int vol, ulong guildId)
        {
            var _player = _lavaSocketClient.GetPlayer(guildId);
            if (_player is null)
                return "Player isn't playing.";

            if (vol > 150 || vol <= 2)
            {
                return "Please use a number between 2 - 150";
            }

            await _player.SetVolumeAsync(vol);
            return $"Volume set to: {vol}";
        }

        public async Task<string> PauseOrResumeAsync(ulong guildId)
        {
            var _player = _lavaSocketClient.GetPlayer(guildId);
            if (_player is null)
                return "Player isn't playing.";

            if (!_player.IsPaused)
            {
                await _player.PauseAsync();
                return "Player is Paused.";
            }
            else
            {
                await _player.ResumeAsync();
                return "Playback resumed.";
            }
        }

        public async Task<string> ResumeAsync(ulong guildId)
        {
            var _player = _lavaSocketClient.GetPlayer(guildId);
            if (_player is null)
                return "Player isn't playing.";

            if (_player.IsPaused)
            {
                await _player.ResumeAsync();
                return "Playback resumed.";
            }

            return "Player is not paused.";
        }


        private async Task ClientReadyAsync()
        {
            await _lavaSocketClient.StartAsync(_client);
        }

        private async Task TrackFinished(LavaPlayer player, LavaTrack track, TrackEndReason reason)
        {
            if (!reason.ShouldPlayNext())
                return;

            if (!player.Queue.TryDequeue(out var item) || !(item is LavaTrack nextTrack))
            {
                await player.TextChannel.SendMessageAsync("There are no more tracks in the queue.");
                return;
            }

            await player.PlayAsync(nextTrack);
        }
    }
}