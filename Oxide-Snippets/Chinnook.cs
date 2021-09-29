using Network;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using Rust;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Oxide.Plugins
{
    [Info("Chinnook", "Sharkgamedev", "0.8.1")]
    [Description("Makes Chinnook purchasable")]
    public class Chinnook : CovalencePlugin
    {
        static Chinnook chin;

        const int c_scrapId = -932201673;
        const string c_chinnookPrefabPath = "assets/prefabs/npc/ch47/ch47.entity.prefab";
        const string c_chinnyBuyText = "Give me a damn chinnook ({} scrap).";
        NPCPlayer lastTalkedToNPC = null;

        CuiElementContainer cachedChinnyUI = null;

        public List<BaseVehicle> Chinnooks = new List<BaseVehicle>();

        // Config based off of Vanish oxide plugin
        #region Configuration
        private Configuration config;

        public class Configuration
        {
            [JsonProperty("The price to pay for Chinny in scrap.")]
            public float Price = 2000;

            [JsonProperty("Save/Load locations for Chinnook.")]
            public List<Vector3> ChinnookLocations = new List<Vector3>();

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
            chin = this;
            cachedChinnyUI = CreateChinnyUI();

            AddCovalenceCommand("BuyChinnook9874537598", "BuyChinnook");

            //Unsubscribe from hooks
            Unsubscribe("OnNpcConversationStart");
            Unsubscribe("OnNpcConversationRespond");
            Unsubscribe("OnPluginUnloaded");
            Unsubscribe("OnSaveLoad");
            Unsubscribe("OnNpcConversationEnded");
            Subscribe("OnNpcConversationStart");
            Subscribe("OnNpcConversationRespond");
            Subscribe("OnNpcConversationEnded");
            Subscribe("OnPluginUnloaded");
            Subscribe("OnSaveLoad");
        }

        private void OnPluginUnloaded(Chinnook name)
        {
            SaveChinnookLocations();
        }

        private object OnSaveLoad(Dictionary<BaseEntity, ProtoBuf.Entity> entities)
        {
            Chinnooks = new List<BaseVehicle>();

            // On load spawn in chinnys at their respective locations
            foreach (Vector3 pos in config.ChinnookLocations)
               SpawnChinnook(pos);

            // if save
            SaveChinnookLocations();

            return null;
        }

        private void BuyChinnook (IPlayer player, string command, string[] args)
        {
            if (lastTalkedToNPC == null) return;

            // Remove scrap from players inventory if they have enough
            BasePlayer bPlayer = player.Object as BasePlayer;
            if (!HasEnough(bPlayer, c_scrapId, (int)config.Price)) return;
            bPlayer.inventory.Take(null, c_scrapId, (int)config.Price);

            Vector3 spawnPos = lastTalkedToNPC.transform.position + (lastTalkedToNPC.transform.right * 20);
            SpawnChinnook(spawnPos);
        }
        
        private bool HasEnough(BasePlayer player, int ItemID, int Amount)
        {
            int total = 0;
            foreach (Item itm in player.inventory.FindItemIDs(ItemID))
                total += itm.amount;

            return (total >= Amount);
        }

        private void SaveChinnookLocations()
        {
            //log the locations of all chinooks
            config.ChinnookLocations = new List<Vector3>();

            foreach (BaseVehicle ch47 in Chinnooks)
                config.ChinnookLocations.Add(ch47.transform.position);

            Config.WriteObject(config, true);
        }

        private void SpawnChinnook(Vector3 spawnPos)
        {
            if (spawnPos == null) return;

            BaseVehicle ch47 = (BaseVehicle)GameManager.server.CreateEntity(c_chinnookPrefabPath, spawnPos, new Quaternion());
            if (ch47 == null) return;

            ch47.Spawn();

            Chinnooks.Add(ch47);
        }

        private object OnNpcConversationStart(NPCTalking npcTalking, BasePlayer player, ConversationData conversationData)
        {
            if (conversationData.providerName != "Airwolf Vendor") return null;

            return null;
        }

        private object OnNpcConversationRespond(NPCTalking npcTalking, BasePlayer player, ConversationData conversationData, ConversationData.ResponseNode responseNode)
        {
            if (conversationData.providerName != "Airwolf Vendor" || responseNode.responseText != "I'd like to buy a helicopter") return null;

            lastTalkedToNPC = npcTalking;

            CuiHelper.AddUi(player, cachedChinnyUI);

            return null;
        }

        private void OnNpcConversationEnded(NPCTalking npcTalking, BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "ChinnyUI");
        }

        #region GUI
        private CuiElementContainer CreateChinnyUI()
        {
            CuiElementContainer _elements = new CuiElementContainer();
            string panel = _elements.Add(new CuiButton
            {
                Button = { Command = "BuyChinnook9874537598", Color = "0.15294117647 0.15686274509 0.12156862745 1.0" },
                RectTransform = { AnchorMin = "0.614 0.385", AnchorMax = "0.832 0.405" },
            }, "Hud.Menu", "ChinnyUI");

            // Number 3
            _elements.Add(new CuiElement
            {
                Parent = panel,
                Components =
                {
                    new CuiTextComponent { Text = "3", FontSize = 13, Font = "robotocondensed-bold.ttf", Color = "0.85490196078 0.81960784313 0.78431372549 1.0", Align = TextAnchor.MiddleCenter},
                    new CuiRectTransformComponent { AnchorMin = "0.1 0.5", AnchorMax = "0.2 0.6"}
                }
            });

            _elements.Add(new CuiElement
            {
                Parent = panel,
                Components =
                {
                    new CuiTextComponent { Text = c_chinnyBuyText.Replace("{}", config.Price.ToString()), FontSize = 11, Font = "robotocondensed-regular.ttf", Color = "0.85490196078 0.81960784313 0.78431372549 1.0", Align = TextAnchor.MiddleLeft},
                    new CuiRectTransformComponent {AnchorMin = "0.088 0", AnchorMax = "1 1"}
                }
            });

            return _elements;
        }

        #endregion GUI
    }
}
