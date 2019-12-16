using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core.Libraries;
using Time = Oxide.Core.Libraries.Time;

namespace Oxide.Plugins
{
    [Info("ES writer", "Darjus Kazakevic", "0.1.0")]
    [Description("Writes data to ElasticSearch")]
    class EsWriter : CovalencePlugin
    {
        private static readonly Time Time = GetLibrary<Time>();

        private void Init()
        {
            Puts("Initialized ES writer");
        }

        void OnPlayerInit(BasePlayer player)
        {
            GetPlayerFromDb(player.userID, (code, response) =>
            {
                Puts($"Code response: {code} {response}");
                if (code == 404)
                {
                    Puts("ok ok ok ok ");
                    //new player
                    CreateOrUpdatePlayer(InitPluginPlayer(player));
                }
                else
                {
                    var playerFromDb = DeserializeResponse(response);
                    playerFromDb.IsOnline = true;
                    CreateOrUpdatePlayer(playerFromDb);
                }

            });
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            GetPlayerFromDb(player.userID, (code, response) =>
            {
                if (code != 200 || response == null) { return; }
                var playerFromDb = DeserializeResponse(response);
                playerFromDb.IsOnline = false;
                CreateOrUpdatePlayer(playerFromDb);
                Puts($"Player goes offline: {playerFromDb.Id}");
            });
        }

        object OnPlayerDie(BasePlayer victim, HitInfo info)
        {
            var killer = info.InitiatorPlayer;

            if (
                killer == null ||
                victim == null ||
                killer == victim ||
                victim.IsNpc
                ) { return null; }

            GetPlayerFromDb(victim.userID, (code, response) =>
            {
                if (code != 200 || response == null) { return; }
                var playerFromDb = DeserializeResponse(response);
                playerFromDb.Stats.deaths++;
                CreateOrUpdatePlayer(playerFromDb);
                Puts($"Player dies: {playerFromDb.Id}");
            });

            GetPlayerFromDb(killer.userID, (code, response) =>
            {
                if (code != 200 || response == null) { return; }
                var playerFromDb = DeserializeResponse(response);
                playerFromDb.Stats.kills++;
                CreateOrUpdatePlayer(playerFromDb);
                Puts($"Player dies: {playerFromDb.Id}");
            });

            return null;
        }

        void OnServerSave()
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

        PluginPlayer DeserializePlayer(string response)
        {
            PluginPlayer pluginPlayer = null;
            JObject parsedResponse = JObject.Parse(response);
            pluginPlayer = JsonConvert.DeserializeObject<PluginPlayer>(parsedResponse["_source"].ToString(Formatting.None));
            return pluginPlayer;
        }

        void GetPlayerFromDb(ulong id,  Action<int, string> callback)
        {
            webrequest.Enqueue("http://localhost:9200/players/_doc/" + id, null, callback, this);
        }

        PluginPlayer DeserializeResponse(string response)
        {
            JObject parsedResponse = JObject.Parse(response);
            return JsonConvert.DeserializeObject<PluginPlayer>(parsedResponse["_source"].ToString(Formatting.None));
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
