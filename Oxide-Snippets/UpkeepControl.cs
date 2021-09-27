using Network;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using Rust;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Upkeep Control", "Sharkgamedev", "1.0.1")]
    [Description("Allows you to control or disable upkeep")]
    public class UpkeepControl : CovalencePlugin
    {
        static UpkeepControl ukCont;

        // Config based off of Vanish oxide plugin
        #region Configuration
        private Configuration config;
        private static readonly DamageTypeList _EmptyDmgList = new DamageTypeList();

        public class Configuration
        {
            [JsonProperty("Upkeep Toggle.")]
            public bool UpkeepEnabled = true;

            [JsonProperty("Upkeep Speed.")]
            public float UpkeepSpeed = 0.0f;

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

        private const string change = "upkeep.change";

        private void Init()
        {
            ukCont = this;

            // Register permissions for commands
            permission.RegisterPermission(change, this);

            //Unsubscribe from hooks
            Unsubscribe("OnEntityTakeDamage");
            Subscribe("OnEntityTakeDamage");
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (info == null || info.damageTypes == null || entity == null || !info.damageTypes.Has(DamageType.Decay)) return null;

            info.damageTypes = _EmptyDmgList;
            info.HitMaterial = 0;
            info.PointStart = Vector3.zero;
            info.HitEntity = null;

            return true;
        }
    }
}
