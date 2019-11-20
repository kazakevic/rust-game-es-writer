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

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            var esPlayer = new SerializablePlayer();
            esPlayer.isOnline = player.IsConnected;
            CreateOrUpdatePlayer(player.userID, JsonConvert.SerializeObject(esPlayer));
        }

        private void OnServerSave()
        {
            foreach (var player in BasePlayer.activePlayerList) {
                Puts($"Stats {player.stats.combat.ToString()}");
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
        }
    }
}
