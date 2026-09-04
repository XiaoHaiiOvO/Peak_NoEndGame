using HarmonyLib;
using Photon.Pun;
using TMPro;
using UnityEngine;

namespace Peak_NoEndGame
{
    [HarmonyPatch]
    internal static class Patches
    {
        [HarmonyPatch(typeof(RunManager), "Awake")]
        [HarmonyPostfix]
        private static void RunManagerAwakePostfix(RunManager __instance)
        {
            if (__instance != null && __instance.GetComponent<ReviewHandler>() == null)
            {
                __instance.gameObject.AddComponent<ReviewHandler>();
                PhotonView view = __instance.GetComponent<PhotonView>();
                if (view != null)
                {
                    view.RefreshRpcMonoBehaviourCache();
                }

                Plugin.Log.LogInfo("Attached the campfire respawn controller to RunManager.");
            }
        }

        [HarmonyPatch(typeof(RunManager), nameof(RunManager.StartRun))]
        [HarmonyPostfix]
        private static void RunManagerStartRunPostfix()
        {
            if (ReviewHandler.Instance != null)
            {
                ReviewHandler.Instance.BeginRun();
            }
        }

        [HarmonyPatch(typeof(Character), "RPCEndGame")]
        [HarmonyPostfix]
        private static void CharacterEndGamePostfix()
        {
            if (ReviewHandler.Instance != null)
            {
                ReviewHandler.Instance.EndRun();
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.CheckEndGame))]
        [HarmonyPrefix]
        private static bool CharacterCheckEndGamePrefix()
        {
            return ReviewHandler.Instance == null || !ReviewHandler.Instance.ShouldSuppressEndGame();
        }

        [HarmonyPatch(typeof(Campfire), "Light_Rpc")]
        [HarmonyPostfix]
        private static void CampfireLightPostfix(bool updateSegment)
        {
            if (updateSegment && ReviewHandler.Instance != null)
            {
                ReviewHandler.Instance.CaptureCampfireCheckpoint();
            }
        }

        [HarmonyPatch(typeof(OrbFogHandler), nameof(OrbFogHandler.InitNewSphere))]
        [HarmonyPostfix]
        private static void OrbFogInitPostfix(FogSphereOrigin newOrigin)
        {
            if (ReviewHandler.Instance != null)
            {
                ReviewHandler.Instance.RememberFogOrigin(newOrigin);
            }
        }

        [HarmonyPatch(typeof(GUIManager), "Awake")]
        [HarmonyPostfix]
        private static void GuiManagerAwakePostfix(GUIManager __instance)
        {
            if (__instance == null || __instance.GetComponentInChildren<ReviewUI>(true) != null)
            {
                return;
            }

            AscentUI source = __instance.GetComponentInChildren<AscentUI>(true);
            if (source == null)
            {
                Plugin.Log.LogWarning("AscentUI was not found; the remaining-respawn counter is unavailable in this scene.");
                return;
            }

            GameObject reviewObject = UnityEngine.Object.Instantiate(source.gameObject, source.transform.parent, false);
            reviewObject.name = "CampfireRespawnUI";

            AscentUI clonedAscent = reviewObject.GetComponent<AscentUI>();
            if (clonedAscent != null)
            {
                UnityEngine.Object.Destroy(clonedAscent);
            }

            RectTransform rect = reviewObject.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 10f);
                rect.sizeDelta = new Vector2(400f, 120f);
            }

            TMP_Text text = reviewObject.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.text = string.Empty;
            }

            reviewObject.AddComponent<ReviewUI>();
        }
    }
}
