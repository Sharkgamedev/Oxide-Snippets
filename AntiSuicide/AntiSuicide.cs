using Network;
using Newtonsoft.Json;
using Rust;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("AntiSuicide", "Sharkgamedev", "0.8.1")]
    [Description("Prevents players without permission from suiciding")]
    public class AntiSuicide : CovalencePlugin
    {
        // Config based off of Vanish oxide plugin
        #region Configuration
        private static readonly DamageTypeList _EmptyDmgList = new DamageTypeList();
        private Configuration config;

        public class Configuration
        {
            [JsonProperty("Announces when anyone commits suicide in chat.")]
            public bool AnnounceSuicide = true;

            [JsonProperty("Whether or not suicide should be allowed by default.")]
            public bool AllowByDefault = false;

            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }

        protected override void LoadDefaultConfig() => config = new Configuration();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                    throw new JsonException();

                if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys))
                {
                    LogWarning("Config appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch
            {
                LogWarning($"Config {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            LogWarning($"Config saved to {Name}.json");
            Config.WriteObject(config, true);
        }

        #endregion Config

        private const string suicideAllow = "suicide.allow";

        private void Init()
        {
            // Register permissions for commands
            permission.RegisterPermission(suicideAllow, this);

            //Unsubscribe from hooks
            Unsubscribe("OnEntityTakeDamage");
            Subscribe("OnEntityTakeDamage");
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            var attacker = info?.InitiatorPlayer;
            var victim = entity?.ToPlayer();

            // If you are killing yourself
            if (victim != attacker) return null;
            if (HasPerm(victim.UserIDString, suicideAllow))
            {
                if (config.AnnounceSuicide)
                    covalence.Server.Command($"say {victim.UserIDString} has made the horrible choice of killing themselves. Everyone is disapointed and they have only suceeded in passing on the pain.");
                return null;
            }

            if (info != null)
            {
                info.damageTypes = _EmptyDmgList;
                info.HitMaterial = 0;
                info.PointStart = Vector3.zero;
                info.HitEntity = null;
            }
            return true;
        }

        private bool HasPerm(string id, string perm) => permission.UserHasPermission(id, perm);
    }
}
