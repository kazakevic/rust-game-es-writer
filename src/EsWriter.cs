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
            UpdatePlayerBaseData(player);
        }

        private void OnServerSave()
        {
           var pl = GetPlayerFromDb(76561198115425683);
           Puts($"VOOOO: {pl.Name}");

            foreach (BasePlayer activePlayer in BasePlayer.activePlayerList)
            {
               UpdatePlayerBaseData(activePlayer);
            }
        }

        void UpdatePlayerBaseData(BasePlayer player)
        {
            CreateOrUpdatePlayer(GetPlayer(player));
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
            IList<PluginPlayer> pluginPlayers = new List<PluginPlayer>();
            var pluginPlayer = new PluginPlayer();

            webrequest.Enqueue("http://localhost:9200/players/_doc/" + id, null, (code, response) =>
            {
                JObject parsedResponse = JObject.Parse(response);
                Puts($"STOROO: {parsedResponse["_source"].ToString(Formatting.None)}");
                pluginPlayer = JsonConvert.DeserializeObject<PluginPlayer>(parsedResponse["_source"].ToString(Formatting.None));
                Puts($"STOROO: { pluginPlayer.Name}");
            }, this);

            return pluginPlayer;
        }

        PluginPlayer GetPlayer(BasePlayer player)
        {
            ServerStatistics.Storage storage = ServerStatistics.Get(player.userID);
            var stats = new PluginPlayerStats();
            stats.kills = storage.Get("kill_player");
            stats.deaths = (storage.Get("deaths") - storage.Get("death_suicide"));
            stats.headShots = storage.Get("headshot");
            stats.suicides = storage.Get("death_suicide");
            var serializablePlayer = new PluginPlayer();
           // serializablePlayer.stats = stats;
            //serializablePlayer.id = player.userID;
           // serializablePlayer.name = player.displayName;
           // serializablePlayer.set = player.IsConnected;
            return serializablePlayer;
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
