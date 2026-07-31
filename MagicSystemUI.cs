
using BepInEx.Logging;
using HarmonyLib;
using Polytopia.Data;
using PolytopiaBackendBase.Game;
using UnityEngine;

namespace Toporzol;

public static class MagicSystemUI
{
    public static bool inMagicView = false;
    internal static TechView techView = null;



    /*

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
    private static void ToggleTest()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            inMagicView = !inMagicView;
            NotificationManager.Notify("inMagicView now " + inMagicView.ToString());
            if (techView != null)
            {
                techView.lastPlayer = 0;
                techView.MarkDirty();
                techView.SetDirty();
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(TechView), nameof(TechView.Show))]
    private static bool MagicStarter(TechView __instance, bool instant)
    {
        if (techView == null) techView = __instance;
        __instance.lastPlayer = 255;
        if (inMagicView)
        {
            Main.modLogger.LogMessage("in Magic View");
            UIManager.Instance.BlockHints();
            UpdateMagicView(__instance);
            __instance.gameObject?.SetActive(true);
            __instance.ActiveSelf = true;
            CameraController.Instance.SetTechBoundsState();
            return false;
        }
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TechItem), nameof(TechItem.RefreshState))]
    private static void MagicIcon(TechItem __instance, bool forceUnavaliable)
    {
        if (inMagicView)
        {
            if (__instance.headImage != null) __instance.headImage.sprite = PolyMod.Registry.GetSprite("swarmcall");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(TechView), nameof(TechView.CreateNode))]
    private static bool MagicBasic(TechView __instance, TechData data, TechItem parentItem, float angle)
    {
        if (inMagicView && data.type == TechData.Type.Basic)
        {

            float num = 120f; // was 72f
            float num2 = 0f;

            if (parentItem != null)
            {
                num2 = angle + num * (float)(data.techUnlocks.Count - 1) / 2f;
            }

            GameLogicData gameLogicData = GameManager.GameState.GameLogicData;
            TribeData tribeData = gameLogicData.GetTribeData(GameManager.LocalPlayer.tribe);
            List<TechData> magicRoutes = new List<TechData>()
            {
              GameManager.GameState.GameLogicData.GetTechData(EnumCache<TechData.Type>.GetType("toporzolmagicone")),
              GameManager.GameState.GameLogicData.GetTechData(EnumCache<TechData.Type>.GetType("toporzolmagictwo")),
              GameManager.GameState.GameLogicData.GetTechData(EnumCache<TechData.Type>.GetType("toporzolmagicthree"))
            };

            foreach (TechData magicRoute in magicRoutes)
            {
                if (gameLogicData.TryGetData(magicRoute.type, out var _)) // if tech exists
                {
                    TechItem parentItem2 = __instance.CreateTechItem(magicRoute, parentItem, num2);
                    __instance.currTechIdx++;
                    if (magicRoute.techUnlocks != null && magicRoute.techUnlocks.Count > 0)
                    {
                        __instance.CreateNode(magicRoute, parentItem2, num2);
                        Main.modLogger.LogMessage("Found child for: " + magicRoute.displayName);
                    }
                    else
                    {
                        Main.modLogger!.LogMessage("Didnt find child for: " + magicRoute.displayName);
                    }
                    num2 -= num;
                }
            }

            __instance.OnItemsRefreshed?.Invoke(__instance);
            return false;

        }
        else if (inMagicView)
        {
            if ((int)data.type < 1000)
            {
                Main.modLogger!.LogMessage("Skipped a node: " + (int)data.type);
                return false;
            }
        }
        return true;
    }



    #region UTILITIES
    private static void UpdateMagicView(TechView view)
    {
        view.lastPlayer = 0;
        view.currTechIdx = 0;
        GameLogicData gameLogicData = GameManager.GameState.GameLogicData;
        PlayerState localPlayer = GameManager.LocalPlayer;
        TechData techData = gameLogicData.GetTechData(TechData.Type.Basic);
        TribeData tribeData = gameLogicData.GetTribeData(localPlayer.tribe);
        techData = GameManager.GameState.GameLogicData.GetOverride(techData, tribeData);
        TechItem techItem = view.CreateTechItem(techData);
        //techItem.button.button.navigation = default(Navigation);
        view.currTechIdx++;
        view.CreateNode(techData, techItem);
        UIUtils.SetExplicitNavigation(view.nodeContainer, useCenter: true, 2);
        view.UpdateTreeSize();

        view.UpdateInfoText();
    }

    #endregion*/
}