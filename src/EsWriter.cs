using System.Collections;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Oxide.Game.Rust.Libraries;
using Server = Rust.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rust;

namespace Oxide.Plugins
{
    [Info("ES writer", "Darjus Kazakevic", "0.1.0")]
    [Description("Writes data to ElasticSearch")]
    class EsWriter : CovalencePlugin
    {
        //private Dictionary<ulong, PluginPlayer> PluginPlayers = new Dictionary<ulong, PluginPlayer>();

        private void Init()
        {
            Puts("Initialized ES writer");
        }

        void OnPlayerInit(BasePlayer player)
        {
            var playerFromDb = GetPlayerFromDb(player.userID);
            if (playerFromDb == null)
            {
                CreateOrUpdatePlayer(InitPluginPlayer(player));
            }
            else
            {
                playerFromDb.IsOnline = true;
                CreateOrUpdatePlayer(playerFromDb);
            }
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            var playerFromDb = GetPlayerFromDb(player.userID);
            playerFromDb.IsOnline = false;
            CreateOrUpdatePlayer(playerFromDb);
        }

        void OnPlayerKicked(BasePlayer player, string reason)
        {
            var playerFromDb = GetPlayerFromDb(player.userID);
            playerFromDb.IsOnline = false;
            CreateOrUpdatePlayer(playerFromDb);;
        }

        private void OnPlayerDie(BasePlayer player, HitInfo info)
        {
            if (info == null || player == null || player.IsNpc)
                return;

            var playerFromDb = GetPlayerFromDb(player.userID);

            var stats = playerFromDb.Stats;

            if (info.damageTypes.GetMajorityDamageType() == DamageType.Suicide)
                stats.suicides++;
            else
            {
                stats.deaths++;
                var attacker = info.InitiatorPlayer;
                if (attacker == null || attacker.IsNpc)
                    return;

                var attackerFromDb = GetPlayerFromDb(attacker.userID);
                attackerFromDb.Stats.kills++;
                CreateOrUpdatePlayer(attackerFromDb);
            }
            CreateOrUpdatePlayer(playerFromDb);
        }

        private void OnServerSave()
        {

        }

        void CreateOrUpdatePlayer(PluginPlayer player)
        {
            var data = JsonConvert.SerializeObject(player);
            Dictionary<string, string> headers = new Dictionary<string, string> {{"Content-Type", "application/json"}};
            webrequest.Enqueue("http://localhost:9200/players/_doc/" + player.Id, data, (code, response) =>
            {
                if (code != 200 || response == null)
                {
                    Puts($"not good response!");
                    return;
                }
            }, this, RequestMethod.PUT, headers);
        }

        PluginPlayer GetPlayerFromDb(ulong id)
        {
            PluginPlayer res = null;
            webrequest.Enqueue("http://localhost:9200/players/_doc/" + id, null, (code, response) =>
            {
                JObject parsedResponse = JObject.Parse(response);
                PluginPlayer pluginPlayer = JsonConvert.DeserializeObject<PluginPlayer>(parsedResponse["_source"].ToString(Formatting.None));
                res = pluginPlayer;
            }, this);
            return res;
        }

        PluginPlayer InitPluginPlayer(BasePlayer player)
        {
            PluginPlayer pluginPlayer = new PluginPlayer();
            PluginPlayerStats stats = new PluginPlayerStats();
            stats.deaths = 0;
            stats.kills = 0;
            stats.suicides = 0;
            stats.headShots = 0;
            pluginPlayer.Name = player.displayName;
            pluginPlayer.Id = player.userID;
            pluginPlayer.IsOnline = true;
            pluginPlayer.Stats = stats;
            return pluginPlayer;
        }

        class PluginPlayer
        {
            public ulong Id;
            public string Name;
            public bool IsOnline;
            public PluginPlayerStats Stats;
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
