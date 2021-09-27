using Facepunch;
using Network;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using Rust;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Oxide.Plugins
{
    [Info("OutPost Revenge", "Sharkgamedev", "0.9.1")]
    [Description("Sends sceientists to the bases of any player that attacks a safe zone.")]
    public class Revenge : CovalencePlugin
    {
        static Revenge revenge;
        private const string _tcPrefab = "cupboard.tool.deployed";
        private const string _scientistPrefab = "assets/prefabs/npc/scientist/scientist.prefab";

        private float _lastRaidingParty;

        private List<BaseEntity> _tcList = new List<BaseEntity>();

        // Config based off of Vanish oxide plugin
        #region Configuration
        private static readonly DamageTypeList _EmptyDmgList = new DamageTypeList();
        private Configuration config;

        public class Configuration
        {
            [JsonProperty("Send scientists to every base the user has privelage on or the first one.")]
            public bool RevengeMultiple = true;

            [JsonProperty("Number of scientists to send to each base.")]
            public int RevengePartySize = 5;

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
            revenge = this;

            //Unsubscribe from hooks
            Unsubscribe("OnEntityTakeDamage");
            Subscribe("OnEntityTakeDamage");
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return null;

            var attacker = info?.InitiatorPlayer;
            var victim = entity?.ToPlayer();

            if (attacker == null || victim == null) return null;

            // If you are attacked by npc
            if (!(attacker is NPCPlayer)) return null;
            if (!attacker.InSafeZone()) return null;
            if (Time.time < _lastRaidingParty) return null;

            UpdateTCList();

            BaseEntity _victimTcb = GetTCOfPlayer(victim);
            if (_victimTcb == null) return null;

            _lastRaidingParty = Time.time + 300.0f;

            for (int i = 0; i < config.RevengePartySize; i++)
            {
                NPCPlayer t_ent = (NPCPlayer)InstantiateSci(TryGetSpawn(_victimTcb.ServerPosition, 50), _victimTcb.ServerRotation);
                var _npc = t_ent.GetComponent<NPCPlayerApex>();
                _npc.Spawn();

                _npc.AttackTarget = _victimTcb;
            }
            
            return null;
        }

        private void UpdateTCList()
        {
            foreach (var _tcb in GameObject.FindObjectsOfType<BaseEntity>())
                if (_tcb != null && IsCupboardEntity(_tcb))
                    _tcList.Add(_tcb);
        }

        private BaseEntity GetTCOfPlayer(BasePlayer player)
        {
            foreach (var _tc in _tcList)
                if (_tc.OwnerID == player.userID)
                    return _tc;

            return null;
        }

        private List<BaseEntity> GetAllTCOfPlayer(BasePlayer player)
        {
            List<BaseEntity> _playerTc = new List<BaseEntity>();

            foreach (var _tc in _tcList)
                if (_tc.OwnerID == player.userID)
                    _playerTc.Add(_tc);

            return (_playerTc.Count > 0 ? _playerTc : null);
        }

        private BaseEntity InstantiateSci(Vector3 position, Quaternion rotation)
        {
            GameObject gameObject = Instantiate.GameObject(GameManager.server.FindPrefab(_scientistPrefab), position, rotation);
            gameObject.name = _scientistPrefab;
            SceneManager.MoveGameObjectToScene(gameObject, Rust.Server.EntityScene);
            if (gameObject.GetComponent<Spawnable>())
                UnityEngine.Object.Destroy(gameObject.GetComponent<Spawnable>());
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            BaseEntity component = gameObject.GetComponent<BaseEntity>();
            return component;
        }

        public static Vector3 CalculateGroundPos(Vector3 pos)
        {
            pos.y = TerrainMeta.HeightMap.GetHeight(pos);
            NavMeshHit navMeshHit;

            if (!NavMesh.SamplePosition(pos, out navMeshHit, 2, 1))
                pos = Vector3.zero;
            else if (WaterLevel.GetWaterDepth(pos, true) > 0)
                pos = Vector3.zero;
            else if (Physics.RaycastAll(navMeshHit.position + new Vector3(0, 100, 0), Vector3.down, 99f, 1235288065).Any())
                pos = Vector3.zero;
            else
                pos = navMeshHit.position;
            return pos;
        }

        private Vector3 TryGetSpawn(Vector3 pos, int radius)
        {
            int attempts = 0;
            var spawnPoint = Vector3.zero;
            Vector2 rand;

            while (attempts < 200 && spawnPoint == Vector3.zero)
            {
                attempts++;
                rand = UnityEngine.Random.insideUnitCircle * radius;
                spawnPoint = CalculateGroundPos(pos + new Vector3(rand.x, 0, rand.y));
                if (spawnPoint != Vector3.zero)
                    return spawnPoint;
            }
            return spawnPoint;
        }

        private bool IsCupboardEntity(BaseEntity entity) => entity != null && entity.ShortPrefabName == _tcPrefab;

        private bool HasPerm(string id, string perm) => permission.UserHasPermission(id, perm);
    }
}
