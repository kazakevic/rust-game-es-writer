using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using System.Diagnostics;
using Oxide.Game.Rust.Libraries;
using Server = Rust.Server;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("ES writer", "Darjus Kazakevic", "0.1.0")]
    [Description("Writes data to ElasticSearch")]
    class EsWriter : CovalencePlugin
    {
        private void Init()
        {
            Puts("Initialized ES writer");
        }

        void OnPlayerInit(BasePlayer player)
        {
            var esPlayer = new SerializablePlayer();
            esPlayer.name = player.displayName;
            esPlayer.isOnline = player.IsConnected;
            CreateOrUpdatePlayer(player.userID, JsonConvert.SerializeObject(esPlayer));
        }

        private void OnServerSave()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                var esPlayer = new SerializablePlayer();
                var stats = new PlayerStats();
                esPlayer.name = player.displayName;
                esPlayer.isOnline = player.IsConnected;
                ServerStatistics.Storage storage = ServerStatistics.Get(player.userID);
                stats.kills = storage.Get("kill_player");
                stats.deaths = (storage.Get("deaths") - storage.Get("death_suicide"));
                stats.headShots = storage.Get("headshot");
                stats.suicides = storage.Get("death_suicide");
                esPlayer.stats = stats;
                CreateOrUpdatePlayer(player.userID, JsonConvert.SerializeObject(esPlayer));
            }

        }

        void CreateOrUpdatePlayer(ulong id, string data)
        {
            Dictionary<string, string> headers = new Dictionary<string, string> {{"Content-Type", "application/json"}};
            webrequest.Enqueue("http://localhost:9200/players/_doc/" + id, data, (code, response) =>
            {
                if (code != 200 || response == null)
                {
                    Puts($"not good response!");
                    return;
                }

                Puts($"Answered: {response}");
            }, this, RequestMethod.PUT, headers);
        }

        class SerializablePlayer
        {
            public ulong id;
            public string name;
            public bool isOnline;
            public PlayerStats stats;
        }

        class PlayerStats
        {
            public int kills;
            public int deaths;
            public int headShots;
            public int suicides;
        }
    }
}
