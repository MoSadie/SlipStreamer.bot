using BepInEx;
using BepInEx.Configuration;
using System.Collections.Generic;
using HarmonyLib;
using System.Net.Http;
using BepInEx.Logging;
using UnityEngine;
using Newtonsoft.Json;
using Subpixel.Events;
using System;

namespace SlipStreamer.bot
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Slipstream_Win.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private static ConfigEntry<string> streamerBotIp;
        private static ConfigEntry<int> streamerBotPort;

        private static ConfigEntry<string> streamerBotActionId;
        private static ConfigEntry<string> streamerBotActionName;

        private static ConfigEntry<bool> captaincyRequired;

        private static HttpClient httpClient = new HttpClient();

        internal static ManualLogSource Log;

        private void Awake()
        {
            Plugin.Log = base.Logger;

            streamerBotIp = Config.Bind("StreamerBot", "Ip", "127.0.0.1");
            streamerBotPort = Config.Bind("StreamerBot", "Port", 7474);
            streamerBotActionId = Config.Bind("StreamerBot", "ActionId", "", "Action ID to execute on game events.");
            streamerBotActionName = Config.Bind("StreamerBot", "ActionName", "", "Action name to execute on game events.");

            captaincyRequired = Config.Bind("Captaincy", "CaptaincyRequired", true, "Configure if you must be the captain of the ship to trigger Streamer.bot actions.");

            //Harmony.CreateAndPatchAll(typeof(Plugin));

            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            Svc.Get<Events>().AddListener<ShipLoadedEvent>(ShipLoadedEvent);
            Svc.Get<Events>().AddListener<OrderGivenEvent>(OrderGivenEvent);
            Svc.Get<Events>().AddListener<BattleStartEvent>(BattleStartEvent);
            Svc.Get<Events>().AddListener<BattleEndEvent>(BattleEndEvent);
            Svc.Get<Events>().AddListener<CampaignStartEvent>(CampaignStartEvent);
            Svc.Get<Events>().AddListener<CampaignEndEvent>(CampaignEndEvent);
            Svc.Get<Events>().AddListener<CampaignSectorChangeEvent>(CampaignSectorChangeEvent);
            Svc.Get<Events>().AddListener<SectorNodeChangedEvent>(SectorNodeChangedEvent);


            Application.quitting += ApplicationQuitting;

            sendEvent(EventType.GameLaunch, []);

        }

        private enum EventType
        {
            GameLaunch, // Wrong time, emits at ship join finish
            GameExit,
            JoinShip,
            StartFight,
            EndFight,
            NodeChange,
            OrderSent,
            Accolade,
            KnockedOut,
            RunStarted,
            RunFailed,
            RunSucceeded,
            NextSector
        }

        private bool blockEvent()
        {
            try {
            return captaincyRequired.Value && (CaptainManager.Main == null || !CaptainManager.Main.IsLocalPlayerCaptain);
            } catch (Exception e)
            {
                Log.LogError($"Error checking captaincy: {e.Message}");
                return false;
            }
        }

        private static void sendEvent(EventType eventType, Dictionary<string, string> data)
        {
            // Make an POST HTTP request to http://<streamerbotIp>:<streamerbotPort>
            // with the following data:
            // {
            //    "action": {
            //        "id": <streamerbotActionId>
            //        "name": <streamerbotActionName>
            //    },
            //    "args": {
            //        <key value pairs from data>
            //    }
            // }

            try
            {
                data.Add("eventType", eventType.ToString());

                var dataJSON = JsonConvert.SerializeObject(new
                {
                    action = new
                    {
                        id = streamerBotActionId.Value,
                        name = streamerBotActionName.Value
                    },
                    args = data
                });

                Log.LogInfo($"Sending event {eventType} to StreamerBot: {streamerBotIp.Value}:{streamerBotPort.Value} with data: {dataJSON}");
                httpClient.PostAsync($"http://{streamerBotIp.Value}:{streamerBotPort.Value}/DoAction", new StringContent(dataJSON));
            }
            catch (HttpRequestException e)
            {
                Log.LogError($"Error sending event to StreamerBot: {e.Message}");
            }
        }

        private void ShipLoadedEvent(ShipLoadedEvent e)
        {
            if (blockEvent())
                return;

            sendEvent(EventType.JoinShip, []);
        }

        private void OrderGivenEvent(OrderGivenEvent e)
        {
            if (blockEvent())
                return;

            switch (e.Order.Type)
            {
                case OrderType.ACCOLADE:
                    sendEvent(EventType.Accolade, new Dictionary<string, string>
                    {
                        { "message", e.Order.Message }
                    });
                    break;

                case OrderType.KNOCKED_OUT:
                    sendEvent(EventType.KnockedOut, new Dictionary<string, string>
                    {
                        { "message", e.Order.Message }
                    });
                    break;

                case OrderType.GENERAL:
                case OrderType.SYSTEM_CRITICAL:
                case OrderType.CREW_TO_MEDBAYS:
                case OrderType.CREW_TO_WEAPONS:
                case OrderType.INVADER_ALERT:
                    sendEvent(EventType.OrderSent, new Dictionary<string, string>
                    {
                        { "orderType", e.Order.Type.ToString() },
                        { "message", e.Order.Message }
                    });
                    break;
            }
        }

        private void ApplicationQuitting()
        {
            sendEvent(EventType.GameExit, []);
        }

        private void BattleStartEvent(BattleStartEvent e)
        {
            if (blockEvent())
                return;

            if (e.Scenario == null || e.Scenario.Encounter == null)
                return;

            sendEvent(EventType.StartFight, new Dictionary<string, string>
            {
                { "enemy", e.Scenario.Encounter.Name },
                { "details", e.Scenario.Encounter.Details.Full.Description }
            });
        }

        private void BattleEndEvent(BattleEndEvent e)
        {
            if (blockEvent())
                return;

            sendEvent(EventType.EndFight, new Dictionary<string, string>
            {
                { "outcome", e.Outcome.ToString() }
            });
        }

        private void CampaignStartEvent(CampaignStartEvent e)
        {
            if (blockEvent())
                return;

            sendEvent(EventType.RunStarted, new Dictionary<string, string>
            {
                { "campaign", e.Campaign.CampaignId.ToString() }
            });
        }

        private void CampaignEndEvent(CampaignEndEvent e)
        {
            if (blockEvent())
                return;

            if (e.Victory)
            {
                sendEvent(EventType.RunFailed, []);
            }
            else
            {
                sendEvent(EventType.RunSucceeded, []);
            }
        }

        private void CampaignSectorChangeEvent(CampaignSectorChangeEvent e)
        {
            if (blockEvent())
                return;

            sendEvent(EventType.NextSector, new Dictionary<string, string>
            {
                { "sectorIndex", e.Campaign.CurrentSectorIndex.ToString() },
                { "sectorName", e.Campaign.CurrentSectorVo.Definition.Name }
            });
        }

        private void SectorNodeChangedEvent(SectorNodeChangedEvent e)
        {
            if (blockEvent())
                return;

            if (!e.CampaignVo.CurrentNodeVo.HasValue) 
                return;

            sendEvent(EventType.NodeChange, new Dictionary<string, string>
            {
                { "isBacktrack", e.IsBacktrack.ToString() },
                { "scenarioKey",  e.CampaignVo.CurrentNodeVo.Value.ScenarioKey},
                { "visited", e.CampaignVo.CurrentNodeVo.Value.Visited.ToString() },
                { "completed", e.CampaignVo.CurrentNodeVo.Value.Completed.ToString() },
                { "captainVictory", e.CampaignVo.CurrentNodeVo.Value.CaptainVictory.ToString() }
            });
        }



        //[HarmonyPatch(typeof(SettingsButtonContainer), "ShowSettingsDialog")]
        //[HarmonyPostfix]
        //static void Press(PressButton __instance)
        //{
        //    Log.LogMessage($"Settings Menu {__instance.name} shown");
        //    sendEvent(EventType.ChoiceMade, new Dictionary<string, string>
        //    {
        //        { "button", __instance.name }
        //    });
        //}

        //[HarmonyPatch(typeof(BaseAlertItem), "OnSendClicked")]
        //[HarmonyPrefix]
        //static bool OnSendClicked(ref int __result)
        //{
         //   sendEvent(EventType.AlertSent, null); //FIXME remove debug code
          //  return true;
        //}
    }
}
