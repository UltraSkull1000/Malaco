using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace Malaco.Serializables
{
    [Serializable]
    public class ServerData
    {
        public ulong _serverId;
        public List<RollTracker> _rollTrackers;
        public ServerData(ulong serverId)
        {
            _serverId = serverId;
            _rollTrackers = new List<RollTracker>();
        }
        public void SaveData()
        {
            if (!Directory.Exists(Environment.CurrentDirectory + "\\data")) Directory.CreateDirectory(Environment.CurrentDirectory + "\\data");
            if (!Directory.Exists(Environment.CurrentDirectory + "\\data\\servers")) Directory.CreateDirectory(Environment.CurrentDirectory + "\\data\\servers");

            JsonSerializer serializer = new JsonSerializer()
            {
                NullValueHandling = NullValueHandling.Ignore,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            };

            using (StreamWriter sw = new StreamWriter(Environment.CurrentDirectory + $"\\data\\servers\\{_serverId}.json"))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, this);
            }
        }
        public static ServerData GetServerData(ulong serverId)
        {
            if (File.Exists(Environment.CurrentDirectory + $"\\data\\servers\\{serverId}.json"))
            {
                JsonSerializer serializer = new JsonSerializer
                {
                    NullValueHandling = NullValueHandling.Include,
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc
                };

                using (StreamReader reader = File.OpenText(Environment.CurrentDirectory + $"\\data\\servers\\{serverId}.json"))
                {
                    return (ServerData)serializer.Deserialize(reader, typeof(ServerData));
                }
            }
            else
            {
                ServerData newServer = new ServerData(serverId);
                newServer.SaveData();
                return newServer;
            }
        }
        public static List<ServerData> GetAllServerData()
        {
            List<ServerData> list = new List<ServerData>();
            foreach (string path in Directory.GetFiles(Environment.CurrentDirectory + $"\\data\\servers"))
            {
                JsonSerializer serializer = new JsonSerializer
                {
                    NullValueHandling = NullValueHandling.Include,
                    DateTimeZoneHandling = DateTimeZoneHandling.Local
                };
                using (StreamReader test = File.OpenText(path))
                {
                    list.Add((ServerData)serializer.Deserialize(test, typeof(ServerData)));
                }
            }
            return list;

        }

        public bool TryGetTrackers(ulong userId, out List<RollTracker> trackers)
        {
            trackers = new List<RollTracker>();
            foreach(RollTracker rt in _rollTrackers)
            {
                if (rt.users.Contains(userId)) trackers.Add(rt);
            }
            if (trackers.Count > 0) return true;
            else return false;
        }
        public void SaveTracker(RollTracker tracker)
        {
            int index= _rollTrackers.IndexOf(_rollTrackers.Find(x => x.id == tracker.id));
            _rollTrackers[index] = tracker;
        }
    }
    [Serializable]
    public class UserData
    {
        public ulong _userId;
        public RollTracker _rollTracker;
        public UserData(ulong userId)
        {
            _userId = userId;
            _rollTracker = new RollTracker();
            _rollTracker.name = "";
        }
        public void SaveData()
        {
            if (!Directory.Exists(Environment.CurrentDirectory + "\\data")) Directory.CreateDirectory(Environment.CurrentDirectory + "\\data");
            if (!Directory.Exists(Environment.CurrentDirectory + "\\data\\users")) Directory.CreateDirectory(Environment.CurrentDirectory + "\\data\\users");

            JsonSerializer serializer = new JsonSerializer()
            {
                NullValueHandling = NullValueHandling.Ignore,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            };

            using (StreamWriter sw = new StreamWriter(Environment.CurrentDirectory + $"\\data\\users\\{_userId}.json"))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, this);
            }
        }
        public static UserData GetUserData(ulong userId)
        {
            if (File.Exists(Environment.CurrentDirectory + $"\\data\\users\\{userId}.json"))
            {
                JsonSerializer serializer = new JsonSerializer
                {
                    NullValueHandling = NullValueHandling.Include,
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc
                };

                using (StreamReader reader = File.OpenText(Environment.CurrentDirectory + $"\\data\\users\\{userId}.json"))
                {
                    return (UserData)serializer.Deserialize(reader, typeof(UserData));
                }
            }
            else
            {
                UserData newUser = new UserData(userId);
                newUser.SaveData();
                return newUser;
            }
        }
        public static List<UserData> GetAllUserData()
        {
            List<UserData> list = new List<UserData>();
            foreach (string path in Directory.GetFiles(Environment.CurrentDirectory + $"\\data\\users"))
            {
                JsonSerializer serializer = new JsonSerializer
                {
                    NullValueHandling = NullValueHandling.Include,
                    DateTimeZoneHandling = DateTimeZoneHandling.Local
                };
                using (StreamReader test = File.OpenText(path))
                {
                    list.Add((UserData)serializer.Deserialize(test, typeof(UserData)));
                }
            }
            return list;

        }
    }
    [Serializable]
    public class RollTracker
    {
        public DateTime timeCreated;

        public ulong id;
        public string name;
        public string description;
        public string colorHex;

        public List<ulong> users;
        public List<DieTracker> _dieTrackers;

        public RollTracker()
        {
            timeCreated = DateTime.UtcNow;
            users = new List<ulong>();
            _dieTrackers = DieTracker.GetStandardSet();

            id = OtherExtensions.LongRandom(100000, 999999);
            name = "New Roll Tracker";
            description = "";
            colorHex = "#ffffff";
        }
        public Embed GetStatsEmbed(SocketCommandContext context)
        {
            EmbedBuilder e = new EmbedBuilder()
            {
                Title = $"{name}",
                Description = description
            };
            e.WithColor((Discord.Color)ColorTranslator.FromHtml(colorHex));
            e.WithTimestamp(timeCreated);
            e.WithFooter($"ID: {id}");

            if(users.Count > 0)
            {
                List<string> names = new List<string>();
                foreach (ulong id in users)
                {
                    names.Add(context.Client.GetUser(id).Mention);
                }
                e.AddField("Users", string.Join(",\n", names));
            }

            List<EmbedFieldBuilder> fields = new List<EmbedFieldBuilder>();
            foreach(var dt in _dieTrackers)
            {
                fields.AddRange(dt.GetFields());
            }

            return e.Build();
        }
        public void AddToTracker(int roll, int sides)
        {
            switch (sides)
            {
                case 4:
                    _dieTrackers[0].AddRoll(roll);
                    break;
                case 6:
                    _dieTrackers[1].AddRoll(roll);
                    break;
                case 8:
                    _dieTrackers[2].AddRoll(roll);
                    break;
                case 10:
                    _dieTrackers[3].AddRoll(roll);
                    break;
                case 12:
                    _dieTrackers[4].AddRoll(roll);
                    break;
                case 20:
                    _dieTrackers[5].AddRoll(roll);
                    break;
                case 100:
                    _dieTrackers[6].AddRoll(roll);
                    break;
                default:
                    _dieTrackers[7].AddRoll(roll);
                    break;
            }
        }
        public void AddToTracker(int[] rolls, int sides)
        {
            foreach(int roll in rolls)
            {
                switch (sides)
                {
                    case 4:
                        _dieTrackers[0].AddRoll(roll);
                        break;
                    case 6:
                        _dieTrackers[1].AddRoll(roll);
                        break;
                    case 8:
                        _dieTrackers[2].AddRoll(roll);
                        break;
                    case 10:
                        _dieTrackers[3].AddRoll(roll);
                        break;
                    case 12:
                        _dieTrackers[4].AddRoll(roll);
                        break;
                    case 20:
                        _dieTrackers[5].AddRoll(roll);
                        break;
                    case 100:
                        _dieTrackers[6].AddRoll(roll);
                        break;
                    default:
                        _dieTrackers[7].AddRoll(roll);
                        break;
                }
            }
        }
    }
    [Serializable]
    public class DieTracker
    {
        public int rollsMade;
        public float average;
        public int faceValue;

        public int numberOfOnes;
        public int numberOfMax;
        public static List<DieTracker> GetStandardSet()
        {
            List<DieTracker> standardSet = new List<DieTracker>()
            {
                new DieTracker(4),    //D4         0
                new DieTracker(6),    //D6         1
                new DieTracker(8),    //D8         2
                new DieTracker(10),   //D10        3
                new DieTracker(12),   //D12        4
                new DieTracker(20),   //D20        5
                new DieTracker(100),  //D100       6
                new DieTracker(0)     //All Else   7
            };
            return standardSet;
        }
        public DieTracker(int faceValue)
        {
            rollsMade = 0;
            average = 0;
            numberOfMax = 0;
            numberOfOnes = 0;

            this.faceValue = faceValue;
        }
        public List<EmbedFieldBuilder> GetFields()
        {
            List<EmbedFieldBuilder> fields = new List<EmbedFieldBuilder>();
            if (rollsMade == 0) { fields.Add(new EmbedFieldBuilder() { Name = $"D{faceValue}", Value = "No Data On Record." }); }
            else
            {
                if(faceValue != 0)
                {
                    fields.Add(new EmbedFieldBuilder() { Name = $"D{faceValue} Rolls", Value = rollsMade.ToString(), IsInline = true });
                    fields.Add(new EmbedFieldBuilder() { Name = $"Average", Value = average.ToString("n2"), IsInline = true });
                    fields.Add(new EmbedFieldBuilder() { Name = $"Nat {faceValue} Rolls", Value = numberOfMax.ToString(), IsInline = true });
                }
                else
                {
                    fields.Add(new EmbedFieldBuilder() { Name = $"Other Rolls", Value = rollsMade.ToString(), IsInline = true });
                    fields.Add(new EmbedFieldBuilder() { Name = $"Average", Value = average.ToString("n2"), IsInline = true });
                }
            }
            return fields;
        }
        public void AddRoll(int roll)
        {
            var p = (average * rollsMade) + roll;
            rollsMade += 1;
            if (roll == 1) numberOfOnes ++;
            if (roll == faceValue) numberOfMax++;
            average = p / rollsMade;
        }
    }
}
