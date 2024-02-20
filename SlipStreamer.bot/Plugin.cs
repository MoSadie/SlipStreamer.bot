using BepInEx;
using BepInEx.Configuration;
using System.Collections.Generic;
using HarmonyLib;

namespace SlipStreamer.bot
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Slipstream_Win.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private ConfigEntry<string> streamerBotIp;
        private ConfigEntry<int> streamerBotPort;

        private ConfigEntry<string> streamerBotActionId;
        private ConfigEntry<string> streamerBotActionName;

        private void Awake()
        {
            streamerBotIp = Config.Bind("StreamerBot", "Ip", "");
            streamerBotPort = Config.Bind("StreamerBot", "Port", 0);
            streamerBotActionId = Config.Bind("StreamerBot", "ActionId", "", "Action ID to execute on game events.");
            streamerBotActionName = Config.Bind("StreamerBot", "ActionName", "", "Action name to execute on game events.");

            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private enum EventType
        {
            GameLaunch,
            GameExit,
            JoinShip,
            StartFight,
            ChoiceAvailable,
            ChoiceMade,
            AlertSent
        }

        private static void sendEvent(EventType eventType , Dictionary<string, string> data) {
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



        }

        [HarmonyPatch(typeof(BaseAlertItem), "OnSendClicked")]
        [HarmonyPrefix]
        static bool OnSendClicked(ref int __result)
        {
            sendEvent(EventType.AlertSent, null); //FIXME remove debug code
            return true;
        }
    }
}
