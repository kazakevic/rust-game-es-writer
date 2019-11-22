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

        object OnPlayerDie(BasePlayer player, HitInfo info)
        {
            var attacker = info.InitiatorPlayer;
            if (attacker == null || attacker.IsNpc)
            {
                return null;
            }
            else
            {
                IDictionary<int, string> lastKiller = new Dictionary<int, string>();
                lastKiller.Add(1,attacker.displayName);
                var data = JsonConvert.SerializeObject(lastKiller);
                CreateOrUpdatePlayer(player.userID, data);
            }

            return null;
        }

        void OnPlayerInit(BasePlayer player)
        {
            var data = JsonConvert.SerializeObject(InitPluginPlayer(player));
            CreateOrUpdatePlayer(player.userID, data);
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            UpdatePlayerStatus(player, false);
        }

        void OnPlayerKicked(BasePlayer player, string reason)
        {
            UpdatePlayerStatus(player, false);
        }

        private void OnServerSave()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (!player.IsConnected)
                    continue;
                UpdatePlayerStats(player);
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

        PluginPlayer InitPluginPlayer(BasePlayer player)
        {
            var esPlayer = new PluginPlayer();
            esPlayer.name = player.displayName;
            esPlayer.isOnline = true;
            return esPlayer;
        }

        void UpdatePlayerStats(BasePlayer player)
        {
            ServerStatistics.Storage storage = ServerStatistics.Get(player.userID);
            var stats = new PluginPlayerStats();
            stats.kills = storage.Get("kill_player");
            stats.deaths = (storage.Get("deaths") - storage.Get("death_suicide"));
            stats.headShots = storage.Get("headshot");
            stats.suicides = storage.Get("death_suicide");
            var data = JsonConvert.SerializeObject(stats);
            CreateOrUpdatePlayer(player.userID, data);
        }

        void UpdatePlayerStatus(BasePlayer player,  bool isConnected)
        {
            var pl = new PluginPlayer();
            pl.name = player.displayName;
            pl.isOnline = isConnected;
            var data = JsonConvert.SerializeObject(pl);
            CreateOrUpdatePlayer(player.userID, data);
        }

        class PluginPlayer
        {
            public ulong id;
            public string name;
            public bool isOnline;
            public PluginPlayerStats stats;
        }

        class PluginPlayerStats
        {
            public int kills;
            public int deaths;
            public int headShots;
            public int suicides;
        }
    }
}
