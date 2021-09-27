using Network;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using Rust;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Tanker", "Sharkgamedev", "0.8.1")]
    [Description("Causes tankers containing low grade to explode violently when taking damage.")]
    public class Tanker : CovalencePlugin
    {
        static Tanker tanker;

        // Config based off of Vanish oxide plugin
        #region Configuration
        private static readonly DamageTypeList _EmptyDmgList = new DamageTypeList();
        private Configuration config;

        public class Configuration
        {
            [JsonProperty("Whether or not to enable the plugin.")]
            public bool Enable = true;

            [JsonProperty("How strong the explosion should be.")]
            public float ExplosionStrength = 50.0f;

            public ExplosionSpec ExplosionSettings = new ExplosionSpec
            {
                BlastRadiusMult = 1,
                DamageMult = 6.0f,
                DensityCoefficient = 1,
                DensityExponent = Math.Round(1.6f * 100) / 100,
                Radius = 15,
                Speed = 10,
            };

        public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }

        public class ExplosionSpec
        {
            private double _speed = 10;
            private double _densityCoefficient = 1;
            private double _densityExponent = 2;

            [JsonProperty("Radius")]
            public double Radius = 10;

            [JsonProperty("DensityCoefficient")]
            public double DensityCoefficient
            {
                get { return _densityCoefficient; }
                set { _densityCoefficient = Math.Max(value, 0.01); }
            }

            [JsonProperty("DensityExponent")]
            public double DensityExponent
            {
                get { return _densityExponent; }
                set { _densityExponent = Math.Min(Math.Max(value, 1), 3); }
            }

            [JsonProperty("Speed")]
            public double Speed
            {
                get { return _speed; }
                set { _speed = Math.Max(value, 0.1); }
            }

            [JsonProperty("BlastRadiusMult")]
            public float BlastRadiusMult = 1;

            [JsonProperty("DamageMult")]
            public float DamageMult = 1;
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
            tanker = this;

            //Unsubscribe from hooks
           // Unsubscribe("OnEntityTakeDamage");
         //   Subscribe("OnEntityTakeDamage");
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return null;

            var attacker = info?.InitiatorPlayer;
            if (entity.health < entity._maxHealth / 2 || info.damageTypes.Has(DamageType.Explosion) || info.damageTypes.Has(DamageType.Heat))
            {
                ServerMgr.Instance.StartCoroutine(ExplosionCoroutine(config.ExplosionSettings, entity.CenterPoint()));
                return true;
            }
            else
                return true;
        }

        private bool HasPerm(string id, string perm) => permission.UserHasPermission(id, perm);

        

        private IEnumerator ExplosionCoroutine(ExplosionSpec spec, Vector3 origin)
        {
            float rocketTravelTime = 0.3f;
            double totalTime = spec.Radius / spec.Speed;
            int numExplosions = (int)Math.Ceiling(spec.DensityCoefficient * Math.Pow(spec.Radius, spec.DensityExponent));

            float timeElapsed = 0;
            double prevDistance = 0;

          //  FireRocket(PrefabExplosiveRocket, origin, Vector3.forward, 0, spec.BlastRadiusMult, spec.DamageMult);

            for (var i = 1; i <= numExplosions; i++)
            {
             //   if (_pluginUnloaded)
              //      yield break;

                double timeFraction = timeElapsed / totalTime;
                double stepDistance = spec.Radius * timeFraction;

                double stepStartDistance = prevDistance;
                double stepEndDistance = stepDistance;

                double rocketDistance = Core.Random.Range(stepStartDistance, stepEndDistance);
                double rocketSpeed = rocketDistance / rocketTravelTime;

             //   Vector3 rocketVector = MakeRandomDomeVector();

                // Skip over some space to reduce the frequency of rockets colliding with each other.
               // Vector3 skipDistance = rocketVector;

             //   rocketVector *= Convert.ToSingle(rocketSpeed);
              //  FireRocket(PrefabExplosiveRocket, origin + skipDistance, rocketVector + skipDistance, rocketTravelTime, spec.BlastRadiusMult, spec.DamageMult);

                float timeToNext = Convert.ToSingle(Math.Pow(i / spec.DensityCoefficient, 1.0 / spec.DensityExponent) / spec.Speed - timeElapsed);

                yield return new WaitForSeconds(timeToNext);
                prevDistance = stepDistance;
              //  timeElapsed += timeToNext;
            }
        }
    }
}
