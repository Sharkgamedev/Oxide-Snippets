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
        const string c_chinnyBuyText = "I'd like to buy a chinnook.";

        CuiElementContainer cachedChinnyUI = null;

        // Config based off of Vanish oxide plugin
        #region Configuration
        private Configuration config;

        public class Configuration
        {
            [JsonProperty("The price to pay for Chinny in scrap.")]
            public float Price = 2000;

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

            //Unsubscribe from hooks
            Unsubscribe("OnNpcConversationStart");
            Unsubscribe("OnNpcConversationRespond");
            Unsubscribe("OnNpcConversationEnded");
            Subscribe("OnNpcConversationStart");
            Subscribe("OnNpcConversationRespond");
            Subscribe("OnNpcConversationEnded");
        }

        private object OnNpcConversationStart(NPCTalking npcTalking, BasePlayer player, ConversationData conversationData)
        {
            if (conversationData.providerName != "Airwolf Vendor") return null;

            return null;
        }

        private object OnNpcConversationRespond(NPCTalking npcTalking, BasePlayer player, ConversationData conversationData, ConversationData.ResponseNode responseNode)
        {
            if (conversationData.providerName != "Airwolf Vendor" || responseNode.responseText != "I'd like to buy a helicopter") return null;

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
            string panel = _elements.Add(new CuiPanel
            {
                Image = { Color = "0.15294117647 0.15686274509 0.12156862745 1.0" },
                RectTransform = { AnchorMin = "0.614 0.385", AnchorMax = "0.832 0.405" },
            }, "Hud.Menu", "ChinnyUI");

            // Number 3
            _elements.Add(new CuiElement
            {
                Parent = panel,
                Components =
                {
                    new CuiTextComponent { Text = "3", FontSize = 13, Font = "robotocondensed-bold.ttf", Color = "0.85490196078 0.81960784313 0.78431372549 1.0", Align = TextAnchor.MiddleCenter},
                    new CuiRectTransformComponent { AnchorMin = "0.614 0.387", AnchorMax = "0.684 0.403"},
                    new CuiImageComponent { Color = "0.15294117647 0.15686274509 0.12156862745 1.0" },
                    new CuiRectTransformComponent { AnchorMin = "0.614 0.387", AnchorMax = "0.684 0.403"}
                }
            });

            _elements.Add(new CuiElement
            {
                Parent = panel,
                Components =
                {
                    new CuiTextComponent { Text = "I'd like to buy a chinnook.", FontSize = 11, Font = "robotocondensed-regular.ttf", Color = "0.85490196078 0.81960784313 0.78431372549 1.0", Align = TextAnchor.MiddleLeft},
                    new CuiRectTransformComponent {AnchorMin = "0.088 0", AnchorMax = "1 1"}
                }
            });
            return _elements;
        }

        #endregion GUI
    }
}
