using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Rust;
using UnityEngine;
using Time = Oxide.Core.Libraries.Time;

namespace Oxide.Plugins
{
    [Info("ES writer", "Darjus Kazakevic", "0.1.0")]
    [Description("Writes data to ElasticSearch")]
    class EsWriter : CovalencePlugin
    {
        private static readonly Time Time = GetLibrary<Time>();

        private PluginPlayer currentPlayer;

        private void Init()
        {
            Puts("Initialized ES writer");
        }

        void OnServerSave()
        {
            GetPlayerFromDb(76561198216484769);
            Puts($" laaa { this.currentPlayer.Name }");
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

        void GetAndUpdatePlayer(ulong id, Action<int, string> callback)
        {
            Action<int, string> GetPlayerCallBack = (code, response) =>
            {
                if (code != 200 || response == null)
                {
                    return;
                }

                var deserializedPlayer = DeserializePlayer(response);
                if (deserializedPlayer != null)
                {
                    CreateOrUpdatePlayer(deserializedPlayer);
                }
            };
            webrequest.Enqueue("http://localhost:9200/players/_doc/" + id, null, GetPlayerCallBack, this);
        }

        PluginPlayer DeserializePlayer(string response)
        {
            PluginPlayer pluginPlayer = null;
            JObject parsedResponse = JObject.Parse(response);
            pluginPlayer = JsonConvert.DeserializeObject<PluginPlayer>(parsedResponse["_source"].ToString(Formatting.None));
            return pluginPlayer;
        }

        bool GetPlayerFromDb(ulong id)
        {
            var done = false;
            Action<int, string> callback = (code, response) =>
            {
                if (code != 200 || response == null)
                {
                    done = true;
                }
                JObject parsedResponse = JObject.Parse(response);
                PluginPlayer pluginPlayer = JsonConvert.DeserializeObject<PluginPlayer>(parsedResponse["_source"].ToString(Formatting.None));
                done = true;
                this.currentPlayer = pluginPlayer;
            };

            webrequest.Enqueue("http://localhost:9200/players/_doc/" + id, null, callback, this);
            return done;
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
            public bool IsOnline;
            public string Name;
            public PluginPlayerStats Stats;
        }

        class PluginPlayerStats
        {
            public int deaths;
            public int headShots;
            public int kills;
            public int suicides;
        }

        class Message
        {
            public uint CreatedAt;
            public ulong From;
            public string Msg;
        }
    }
}
