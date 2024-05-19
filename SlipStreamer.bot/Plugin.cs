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

        //private static ConfigEntry<int> eventCooldown;

        //private static ConfigEntry<bool> captaincyRequired;

        private static HttpClient httpClient = new HttpClient();

        internal static ManualLogSource Log;

        private static Dictionary<EventType, ConfigEntry<int>> eventCooldownConfigs = new Dictionary<EventType, ConfigEntry<int>>();
        private static Dictionary<EventType, long> lastEventTime = new Dictionary<EventType, long>();

        private void Awake()
        {
            Plugin.Log = base.Logger;

            streamerBotIp = Config.Bind("StreamerBot", "Ip", "127.0.0.1");
            streamerBotPort = Config.Bind("StreamerBot", "Port", 7474);
            streamerBotActionId = Config.Bind("StreamerBot", "ActionId", "da524811-ff47-4493-afe6-67f27eff234d", "Action ID to execute on game events.");
            streamerBotActionName = Config.Bind("StreamerBot", "ActionName", "(Internal) Receive Event", "Action name to execute on game events.");

            //eventCooldown = Config.Bind("StreamerBot", "EventCooldown", 5000, "Cooldown in ms before sending a duplicate event. (Cooldown is per event type.) Set to 0 to disable cooldown.");

            foreach (EventType eventType in Enum.GetValues(typeof(EventType)))
            {
                eventCooldownConfigs[eventType] = Config.Bind("StreamerBot", $"EventCooldown_{eventType}", 0, $"Cooldown in ms before sending a duplicate {eventType} event. (ex. 5000 = 5 seconds) Set to 0 to disable cooldown.");
            }

            //captaincyRequired = Config.Bind("Captaincy", "CaptaincyRequired", true, "Configure if you must be the captain of the ship to trigger Streamer.bot actions.");

            Harmony.CreateAndPatchAll(typeof(Plugin));

            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            Svc.Get<Events>().AddListener<ShipLoadedEvent>(ShipLoadedEvent, 1);
            Svc.Get<Events>().AddListener<OrderGivenEvent>(OrderGivenEvent, 1);
            Svc.Get<Events>().AddListener<BattleStartEvent>(BattleStartEvent, 1);
            Svc.Get<Events>().AddListener<BattleEndEvent>(BattleEndEvent, 1);
            Svc.Get<Events>().AddListener<CampaignStartEvent>(CampaignStartEvent, 1);
            Svc.Get<Events>().AddListener<CampaignEndEvent>(CampaignEndEvent, 1);
            Svc.Get<Events>().AddListener<CampaignSectorChangeEvent>(CampaignSectorChangeEvent, 1);
            Svc.Get<Events>().AddListener<SectorNodeChangedEvent>(SectorNodeChangedEvent, 1);
            Svc.Get<Events>().AddListener<CrewmateCreatedEvent>(CrewmateCreatedEvent, 1);
            Svc.Get<Events>().AddListener<CrewmateRemovedEvent>(CrewmateRemovedEvent, 1);
                      

            Application.quitting += ApplicationQuitting;

            sendEvent(EventType.GameLaunch, []);

        }

        private enum EventType
        {
            GameLaunch,
            GameExit,
            JoinShip,
            StartFight,
            EndFight,
            NodeChange,
            ChoiceAvailable,
            //OrderSent, // not working
            //Accolade, // not working
            KnockedOut,
            RunStarted,
            RunFailed,
            RunSucceeded,
            NextSector,
            ShopEntered,
            CrewmateCreated,
            CrewmateRemoved,
            CrewmateSwapped,
        }

        private bool blockEvent()
        {
            return false;

            //This doesn't work right now.
            //try {
            //    return captaincyRequired.Value && (CaptainManager.Main == null || !CaptainManager.Main.IsLocalPlayerCaptain);
            //} catch (Exception e)
            //{
            //    Log.LogError($"Error checking captaincy: {e.Message}");
            //    return false;
            //}
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

            // Check if the event is on cooldown
            if (eventCooldownConfigs[eventType].Value > 0)
            {
                long currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                if (lastEventTime.ContainsKey(eventType) && currentTime - lastEventTime[eventType] < eventCooldownConfigs[eventType].Value)
                {
                    Log.LogInfo($"Event {eventType} is on cooldown. Skipping.");
                    return;
                }
                lastEventTime[eventType] = currentTime;
            }

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
            try
            {
                if (blockEvent())
                    return;

                sendEvent(EventType.JoinShip, []);
            }
            catch (Exception ex)
            {
                Log.LogError($"Error sending ShipLoadedEvent: {ex.Message}");
            }
        }

        private void OrderGivenEvent(OrderGivenEvent e)
        {
            try
            {
                if (blockEvent())
                    return;

                switch (e.Order.Type)
                {
                    // This event is only for reciving orders, not sending them.

                    //case OrderType.ACCOLADE:
                    //    sendEvent(EventType.Accolade, new Dictionary<string, string>
                    //    {
                    //        { "message", e.Order.Message }
                    //    });
                    //    break;

                    case OrderType.KNOCKED_OUT:
                        sendEvent(EventType.KnockedOut, new Dictionary<string, string>
                    {
                        { "message", e.Order.Message }
                    });
                        break;

                        //case OrderType.GENERAL:
                        //case OrderType.SYSTEM_CRITICAL:
                        //case OrderType.CREW_TO_MEDBAYS:
                        //case OrderType.CREW_TO_WEAPONS:
                        //case OrderType.INVADER_ALERT:
                        //    sendEvent(EventType.OrderSent, new Dictionary<string, string>
                        //    {
                        //        { "orderType", e.Order.Type.ToString() },
                        //        { "message", e.Order.Message }
                        //    });
                        //    break;

                        //default:
                        //    Log.LogError($"Unknown order type: {e.Order.Type}");
                        //    break;
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"Error sending OrderGivenEvent: {ex.Message}");
            }
        }

        private void ApplicationQuitting()
        {
            sendEvent(EventType.GameExit, []);
        }

        private void BattleStartEvent(BattleStartEvent e)
        {
            try
            {
                if (blockEvent())
                    return;

                if (e.Scenario == null || e.Scenario.Battle == null)
                {
                    Log.LogError("BattleStartEvent: Scenario or Battle is null");
                    return;
                }


                sendEvent(EventType.StartFight, new Dictionary<string, string>
                {
                    { "enemy", e.Scenario.Battle.Metadata.EnemyName },
                    { "invaders", e.Scenario.Battle.Metadata.InvaderDescription },
                    { "intel", e.Scenario.Battle.Metadata.IntelDescription },
                    { "threatLevel", e.Scenario.Battle.Metadata.ThreatLevel.ToString() },
                    { "speedLevel", e.Scenario.Battle.Metadata.SpeedLevel.ToString() },
                    { "cargoLevel", e.Scenario.Battle.Metadata.CargoLevel.ToString() }
                });
            }
            catch (Exception ex)
            {
                Log.LogError($"Error sending BattleStartEvent: {ex.Message}");
            }
        }

        private void BattleEndEvent(BattleEndEvent e)
        {
            try
            {
                if (blockEvent())
                    return;

                sendEvent(EventType.EndFight, new Dictionary<string, string>
                {
                    { "outcome", e.Outcome.ToString() }
                });
            }
            catch (Exception ex)
            {
                Log.LogError($"Error sending BattleEndEvent: {ex.Message}");
            }
        }

        private void CampaignStartEvent(CampaignStartEvent e)
        {
            try
            {
                if (blockEvent())
                    return;

                sendEvent(EventType.RunStarted, new Dictionary<string, string>
                {
                    { "campaign", e.Campaign.CampaignId.ToString() },
                    { "region", e.Campaign.CaptainCampaign.RegionVo.Metadata.Name }
                });
            }
            catch (Exception ex)
            {
                Log.LogError($"Error sending CampaignStartEvent: {ex.Message}");
            }
        }

        private void CampaignEndEvent(CampaignEndEvent e)
        {
            try
            {
                if (blockEvent())
                    return;

                if (e.Victory)
                {
                    sendEvent(EventType.RunSucceeded, []);
                }
                else
                {
                    sendEvent(EventType.RunFailed, []);
                }
            } catch (Exception ex)
            {
                Log.LogError($"Error sending CampaignEndEvent: {ex.Message}");
            }
        }

        private void CampaignSectorChangeEvent(CampaignSectorChangeEvent e)
        {
            try
            {
                if (blockEvent())
                    return;

                sendEvent(EventType.NextSector, new Dictionary<string, string>
                {
                    { "sectorIndex", e.Campaign.CurrentSectorIndex.ToString() },
                    { "sectorName", e.Campaign.CurrentSectorVo.Definition.Name }
                });
            } catch (Exception ex)
            {
                Log.LogError($"Error sending CampaignSectorChangeEvent: {ex.Message}");
            }
        }

        private void SectorNodeChangedEvent(SectorNodeChangedEvent e)
        {
            try
            {
                if (blockEvent())
                    return;

                if (!e.CampaignVo.CurrentNodeVo.HasValue)
                    return;

                ScenarioWrapperVo scenario = Svc.Get<ScenarioCatalog>().GetScenarioVo(e.CampaignVo.CurrentNodeVo.Value.ScenarioKey);

                if (scenario == null)
                {
                    Log.LogError($"Scenario not found: {e.CampaignVo.CurrentNodeVo.Value.ScenarioKey}");
                    return;
                }
                else if (scenario.Encounter != null)
                {
                    sendEvent(EventType.ChoiceAvailable, new Dictionary<string, string>
                    {
                        { "isBacktrack", e.IsBacktrack.ToString() },
                        { "scenarioKey",  e.CampaignVo.CurrentNodeVo.Value.ScenarioKey},
                        { "visited", e.CampaignVo.CurrentNodeVo.Value.Visited.ToString() },
                        { "completed", e.CampaignVo.CurrentNodeVo.Value.Completed.ToString() },
                        { "captainVictory", e.CampaignVo.CurrentNodeVo.Value.CaptainVictory.ToString() },
                        { "scenarioName", scenario.Encounter.Name },
                        { "scenarioDescription", scenario.Encounter.Details.Full.Description },
                        { "proposition", scenario.Encounter.Proposition },
                        { "choice1", scenario.Encounter.Option1.Action },
                        { "choice2", scenario.Encounter.Option2.Action }
                    });
                }
                else if (scenario.Outpost != null)
                {
                    Dictionary<string, string> data = new Dictionary<string, string>()
                    {
                        { "isBacktrack", e.IsBacktrack.ToString() },
                        { "scenarioKey",  e.CampaignVo.CurrentNodeVo.Value.ScenarioKey},
                        { "visited", e.CampaignVo.CurrentNodeVo.Value.Visited.ToString() },
                        { "name", scenario.Outpost.Name },
                        { "description", scenario.Outpost.Details.Full.Description },
                        { "inventorySize", scenario.Outpost.Inventory.Length.ToString() }
                    };

                    for (int i = 0; i < scenario.Outpost.Inventory.Length; i++)
                    {
                        data.Add($"inventory{i}_type", scenario.Outpost.Inventory[i].Type.ToString());
                        data.Add($"inventory{i}_price", scenario.Outpost.Inventory[i].PricePerUnit.ToString());
                        data.Add($"inventory{i}_subtype", scenario.Outpost.Inventory[i].SubType.ToString());
                    }
                    sendEvent(EventType.ShopEntered, data);
                }

                sendEvent(EventType.NodeChange, new Dictionary<string, string>
                {
                    { "isBacktrack", e.IsBacktrack.ToString() },
                    { "scenarioKey",  e.CampaignVo.CurrentNodeVo.Value.ScenarioKey},
                    { "visited", e.CampaignVo.CurrentNodeVo.Value.Visited.ToString() },
                    { "completed", e.CampaignVo.CurrentNodeVo.Value.Completed.ToString() },
                    { "captainVictory", e.CampaignVo.CurrentNodeVo.Value.CaptainVictory.ToString() }
                });
            } catch (Exception ex)
            {
                Log.LogError($"Error sending SectorNodeChangedEvent: {ex.Message}");
            }
        }

        private void CrewmateCreatedEvent(CrewmateCreatedEvent e)
        {
            try
            {
                if (blockEvent())
                    return;

                string name = e.Crewmate.Client != null ? e.Crewmate.Client.Player.DisplayName : "Crew";


                sendEvent(EventType.CrewmateCreated, new Dictionary<string, string>
                {
                    { "name", name },
                    { "id", e.Crewmate.CrewmateId.ToString() },
                    { "level", e.CrewmateVo.Progression.Level.ToString() },
                    { "xp", e.CrewmateVo.Progression.TotalXp.ToString() },
                    { "archetype", e.CrewmateVo.ArchetypeId },
                    { "statHealth", e.Crewmate.Stats.MaxHealth.ToString() },
                    { "statShields", e.Crewmate.Stats.MaxShields.ToString() }
                    //{ "statDefense", e.Crewmate.Stats.StationShieldChargeMultiplier.ToString() },
                    //{ "statGunnery", e.Crewmate.Stats.StationAttackMultiplier.ToString() },
                    //{ "statCombat", e.Crewmate.Stats.MeleeDamage.ToString() },
                    //{ "statRepairs", e.Crewmate.Stats.StationRepairMultiplier.ToString() },
                    //{ "statSpeed", e.Crewmate.Stats.Speed.ToString() }
                });
            } catch (Exception ex)
            {
                Log.LogError($"Error sending CrewmateCreatedEvent: {ex.Message}");
            }
        }

        private void CrewmateRemovedEvent(CrewmateRemovedEvent e)
        {
            try
            {
                if (blockEvent())
                    return;

                string name = e.Crewmate.Client != null ? e.Crewmate.Client.Player.DisplayName : "Crew";

                sendEvent(EventType.CrewmateRemoved, new Dictionary<string, string>
                {
                    { "name", name },
                    { "id", e.Crewmate.CrewmateId.ToString() }
                });
            } catch (Exception ex)
            {
                Log.LogError($"Error sending CrewmateRemovedEvent: {ex.Message}");
            }
        }


        // One day it would be nice to have a dedicated CrewmateSwappedEvent that I can listen to.
        // In the meantime I'll patch into the MpCrewController.OnCrewmateSwapped method to listen.
        [HarmonyPatch(typeof(MpCrewController), "OnCrewmateSwapped")]
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        static void OnCrewmateSwapped(ref MpCrewController __instance, Notification<CrewmateSwapped.Payload> notif)
        {
            Log.LogMessage($"Crewmate Swapped Test. ID: {notif.Payload.SessionId}");
            Crewmate crewmateById = __instance.GetCrewmateById(notif.Payload.SessionId);
            if (crewmateById == null)
            {
                Log.LogError($"Crewmate not found by ID: {notif.Payload.SessionId}");
                return;
            }
            string name = crewmateById.Client != null ? crewmateById.Client.Player.DisplayName : "Crew";
            try
            {
                sendEvent(EventType.CrewmateSwapped, new Dictionary<string, string>
            {
                { "name", name },
                { "id", crewmateById.CrewmateId.ToString() },
                { "level", notif.Payload.NewCrewmateVo.Progression.Level.ToString() },
                { "xp", notif.Payload.NewCrewmateVo.Progression.TotalXp.ToString() },
                { "archetype", notif.Payload.NewCrewmateVo.ArchetypeId },
                { "statHealth", notif.Payload.NewCrewmateVo.Stats.MaxHealth.ToString() },
                { "statShields", notif.Payload.NewCrewmateVo.Stats.MaxShields.ToString() }
            });
            } catch (Exception ex)
            {
                Log.LogError($"Error sending CrewmateSwappedEvent: {ex.Message}");
            }
        }
    }
}
