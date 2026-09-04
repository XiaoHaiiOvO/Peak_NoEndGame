using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Peak_NoEndGame
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string GUID = "com.Xiaohai.CampfireRespawn";
        public const string NAME = "Campfire Respawn";
        public const string VERSION = "2.0.0";

        internal static Plugin Instance { get; private set; }
        internal static ManualLogSource Log { get; private set; }

        internal ConfigEntry<bool> CampfireClearStatus { get; private set; }
        internal ConfigEntry<bool> ReviveAddCurse { get; private set; }
        internal ConfigEntry<int> RespawnItemChance { get; private set; }
        internal ConfigEntry<KeyCode> RespawnHotkey { get; private set; }
        internal ConfigEntry<int> RespawnMaxTimes { get; private set; }
        internal ConfigEntry<bool> RecordItemsAtCampfire { get; private set; }

        private Harmony _harmony;

        internal int MaximumRespawns
        {
            get { return Mathf.Max(0, RespawnMaxTimes.Value); }
        }

        internal int ItemRestoreChance
        {
            get { return Mathf.Clamp(RespawnItemChance.Value, 0, 100); }
        }

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            CampfireClearStatus = Config.Bind(
                "Settings",
                "CampfireClearStatus",
                false,
                "Clear curable negative status effects while resting at a campfire. 在营火旁休息时清除可治愈的负面状态。");

            // Keep the legacy key so existing configuration files continue to work.
            ReviveAddCurse = Config.Bind(
                "Settings",
                "ReviveClearStatus",
                false,
                "Apply the normal post-revive curse and hunger penalties. 复活时施加游戏原生的诅咒与饥饿惩罚。");

            RespawnHotkey = Config.Bind(
                "Settings",
                "RespawnHotkey",
                KeyCode.F11,
                "Force a campfire respawn (host only). 强制触发篝火复活（仅房主）。");

            RespawnMaxTimes = Config.Bind(
                "Settings",
                "RespawnMaxTimes",
                99,
                new ConfigDescription(
                    "Maximum respawns per run. 每局允许的最大复活次数。",
                    new AcceptableValueRange<int>(0, 999)));

            RecordItemsAtCampfire = Config.Bind(
                "Settings",
                "recordItemsAtCampfire",
                true,
                "TRUE restores the last campfire inventory; FALSE recovers items dropped during the wipe. TRUE：恢复最后一次篝火记录；FALSE：找回团灭时掉落的物品。");

            RespawnItemChance = Config.Bind(
                "Settings",
                "RespawnItemChance",
                88,
                new ConfigDescription(
                    "Chance (0-100) to restore each item. 每件物品被恢复的概率（0-100）。",
                    new AcceptableValueRange<int>(0, 100)));

            _harmony = new Harmony(GUID);
            _harmony.PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo(NAME + " v" + VERSION + " loaded / 篝火复活 MOD 已加载");
        }

        private void OnDestroy()
        {
            if (_harmony != null)
            {
                _harmony.UnpatchSelf();
                _harmony = null;
            }

            Instance = null;
            Log = null;
        }
    }
}
