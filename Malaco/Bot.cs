using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Malaco
{
    public class Bot
    {
        public DiscordSocketClient _client;
        public CommandHandler _handler;

        public User_Interface _ui;

        public Bot(User_Interface ui)
        {
            _ui = ui;
            return;
        }

        public async Task StartAsync(string botToken)
        {
            _client = new DiscordSocketClient();
            _ui.PostToConsole($"Successfully Initialized Client, Logging in...", System.Drawing.Color.Lime);
            while (_client.LoginState != LoginState.LoggedIn)
            {
                try
                {
                    await _client.LoginAsync(TokenType.Bot, botToken);
                    await _client.StartAsync();
                }
                catch (Exception e)
                {
                    _ui.PostToConsole($"WARNING!! An error occurred while logging in. Exception: {e.Message}", System.Drawing.Color.Red);
                    Thread.Sleep(100);
                }
            }
            while (_client.ConnectionState != ConnectionState.Connected)
            {
                switch (_client.ConnectionState)
                {
                    case ConnectionState.Disconnected:
                        _ui.PostToConsole(_client.ConnectionState.ToString(), System.Drawing.Color.IndianRed, false);
                        break;
                    case ConnectionState.Disconnecting:
                        _ui.PostToConsole(_client.ConnectionState.ToString(), System.Drawing.Color.OrangeRed, false);
                        break;
                    case ConnectionState.Connecting:
                        _ui.PostToConsole(_client.ConnectionState.ToString(), System.Drawing.Color.Yellow, false);
                        break;
                    case ConnectionState.Connected:
                        break;
                }
                Thread.Sleep(100);
            }
            _ui.PostToConsole(_client.ConnectionState.ToString(), System.Drawing.Color.LightGreen, false);
            _ui.PostToConsole($"Success! Logged in as bot user {_client.CurrentUser.Username}.", System.Drawing.Color.Lime);
            _ui.PostToConsole($"Starting Command Handler....", System.Drawing.Color.Lime);
            _handler = new CommandHandler(_client, _ui);
            _ui.UpdateElements(_client);
            _ui.Invoke(new Action(_ui.Unlock));
            _ui.PostToConsole($"Success! Bot user {_client.CurrentUser.Username} is active and ready for your commands.", System.Drawing.Color.Lime);
            Thread.Sleep(-1);
        }
    }
}
