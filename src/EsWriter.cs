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
            CreateOrUpdatePlayer(player.userID, JsonConvert.SerializeObject(getSerializablePlayer(player)));
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            CreateOrUpdatePlayer(player.userID, JsonConvert.SerializeObject(getSerializablePlayer(player)));
        }

        void OnPlayerKicked(BasePlayer player, string reason)
        {
            CreateOrUpdatePlayer(player.userID, JsonConvert.SerializeObject(getSerializablePlayer(player)));
        }

        private void OnServerSave()
        {
            Puts($"total: {BasePlayer.activePlayerList.Capacity}");
            foreach (var player in BasePlayer.activePlayerList)
            {
                Puts($"Player {player.displayName}");
                if (!player.IsConnected)
                    continue;

                CreateOrUpdatePlayer(player.userID, JsonConvert.SerializeObject(getSerializablePlayer(player)));
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

        SerializablePlayer getSerializablePlayer(BasePlayer player)
        {
            var esPlayer = new SerializablePlayer();
            var esStats = new PlayerStats();
            ServerStatistics.Storage storage = ServerStatistics.Get(player.userID);
            esPlayer.name = player.displayName;
            esPlayer.isOnline = player.IsConnected;
            esStats.kills = storage.Get("kill_player");
            esStats.deaths = (storage.Get("deaths") - storage.Get("death_suicide"));
            esStats.headShots = storage.Get("headshot");
            esStats.suicides = storage.Get("death_suicide");
            esPlayer.stats = esStats;
            return esPlayer;
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
