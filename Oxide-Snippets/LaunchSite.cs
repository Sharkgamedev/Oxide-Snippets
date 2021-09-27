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
    [Info("Launch", "Sharkgamedev", "0.8.1")]
    [Description("Allows players to get into launch site without keycard.")]
    public class LaunchSite : CovalencePlugin
    {
        static LaunchSite ls;

        // Config based off of Vanish oxide plugin
        #region Configuration
        private static readonly DamageTypeList _EmptyDmgList = new DamageTypeList();
        private Configuration config;

        public class Configuration
        {

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

        private void Init()
        {
            ls = this;

            //Unsubscribe from hooks
            Unsubscribe("CanUnlock");
            Subscribe("CanUnlock");
        }

        private object CanUnlock(BasePlayer player, BaseLock baseLock)
        {
            MonumentInfo[] _monuments = UnityEngine.Object.FindObjectsOfType<MonumentInfo>();
            foreach (var monumentInfo in _monuments)
                if (monumentInfo.displayPhrase.translated == "Launch Site")
                    if (monumentInfo.IsInBounds(baseLock.ServerPosition)) return true;

            return null;
        }
    }
}
