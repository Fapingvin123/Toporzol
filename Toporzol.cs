
using BepInEx.Logging;
using HarmonyLib;
using Polytopia.Data;
using PolytopiaBackendBase.Game;
using UnityEngine;

namespace Toporzol;



public static class Main
{
    private static ManualLogSource? modLogger;
    public static void Load(ManualLogSource logger)
    {
        PolyMod.Loader.AddPatchDataType("unitEffect", typeof(UnitEffect));
        Harmony.CreateAndPatchAll(typeof(Main));
        modLogger = logger;
        logger.LogMessage("Toporzol.dll loaded.");
    }

    #region Utils

    /// <summary>
    /// Trains a land unit on a random valid neighboring tile, returns success.
    /// </summary>
    public static bool TrainAroundTile(byte id, UnitData.Type type, WorldCoordinates coords, int cost = 0)
    {
        GameState state = GameManager.GameState;
        List<WorldCoordinates> validtiles = new();
        foreach (var tile in state.Map.GetTileNeighbors(coords))
        {
            if (tile != null && !tile.IsWater && tile.unit == null) validtiles.Add(tile.coordinates);
        }

        if (validtiles.Count != 0)
        {
            state.ActionStack.Add(new TrainAction(id, type, validtiles[UnityEngine.Random.RandomRangeInt(0, validtiles.Count)], cost));
            return true;
        }
        return false;
    }

    #endregion
    #region ExtraSpearman
    /* Start with 2 spearmen instead of 1 */
    [HarmonyPostfix]
    [HarmonyPatch(typeof(StartMatchAction), nameof(StartMatchAction.ExecuteDefault))]
    public static void ExtraSpearmanAbility(GameState gameState, StartMatchAction __instance)
    {
        if (gameState.TryGetPlayer(__instance.PlayerId, out PlayerState playerState))
        {
            if (gameState.GameLogicData.GetTribeData(playerState.tribe).HasAbility(EnumCache<TribeAbility.Type>.GetType("swarmability")))
            {
                TrainAroundTile(playerState.Id, EnumCache<UnitData.Type>.GetType("spearman"), playerState.GetCurrentCapitalCoordinates(gameState));
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(StartMatchReaction), nameof(StartMatchReaction.GetDescription))]
    public static void CustomIntro(GameMode gameMode, StartMatchReaction __instance, ref string __result)
    {
        if (GameManager.GameState.TryGetPlayer(__instance.action.PlayerId, out PlayerState player))
        {
            if (GameManager.GameState.GameLogicData.GetTribeData(player.tribe).HasAbility(EnumCache<TribeAbility.Type>.GetType("swarmability")))
            {
                __result += Localization.Get("toporzol.intro");
            }
        }
    }
    #endregion

    #region Swarmcalling


    // count max hp increase of swarmcallers by stacking effects
    [HarmonyPrefix]
    [HarmonyPatch(typeof(UnitState), nameof(UnitState.AddEffect))]
    public static bool StackEffects(UnitState __instance, UnitEffect effect)
    {
        if (effect == EnumCache<UnitEffect>.GetType("toporzolcalled"))
        {
            __instance.effects.Add(effect);
        }
        return true;
    }

    // The stacked effects though have to stay hidden in InteractionBar, and because its method is very very long,
    // use prefix-postfix. every time interactionbar is opened, temporarily remove, then readd the stacked effects. 
    static int numOfCleanedEffects;
    [HarmonyPrefix]
    [HarmonyPatch(typeof(InteractionBar), nameof(InteractionBar.RefreshUnitOptions))]
    public static void TempEffectCleanup(InteractionBar __instance)
    {
        if (__instance.unit == null || __instance.mode != InteractionBar.Mode.Unit || __instance.unit.UnitState.effects.Count == 0) return;
        numOfCleanedEffects = 0;
        bool effectoccuredonce = false;
        for (int i = __instance.unit.UnitState.effects.Count - 1; i >= 0; i--)
        {
            if (__instance.unit.UnitState.effects[i] == EnumCache<UnitEffect>.GetType("toporzolcalled"))
            {
                if (!effectoccuredonce)
                {
                    effectoccuredonce = true;
                }
                else
                {
                    __instance.unit.UnitState.effects.RemoveAt(i);
                    numOfCleanedEffects++;
                }
            }
        }
        modLogger.LogMessage("Number of cleaned effects: " + numOfCleanedEffects);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(InteractionBar), nameof(InteractionBar.RefreshUnitOptions))]
    public static void ReaddTempRemovedEffect(InteractionBar __instance)
    {
        if (__instance.unit == null || __instance.mode != InteractionBar.Mode.Unit || __instance.unit.UnitState.effects.Count == 0) return;
        for (int i = 0; i < numOfCleanedEffects; i++)
        {
            __instance.unit.UnitState.effects.Add(EnumCache<UnitEffect>.GetType("toporzolcalled"));
        }
        modLogger.LogMessage("Reinstated " + numOfCleanedEffects + " effects");
        numOfCleanedEffects = 0;
    }


    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameLogicData), nameof(GameLogicData.CanBuild))]
    public static void CanSwarmcall(GameState gameState, TileData tile, PlayerState playerState, ImprovementData improvement, ref bool __result)
    {
        if(improvement.type == EnumCache<ImprovementData.Type>.GetType("swarmcall"))
        {
            if(tile.unit == null || !tile.unit.HasAbility(EnumCache<UnitAbility.Type>.GetType("swarmcalling")) || tile.unit.HasEffect(EnumCache<UnitEffect>.GetType("toporzolcalled")))
            {
                __result = false;
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BuildAction), nameof(BuildAction.ExecuteDefault))]
    public static void Swarmcalling(BuildAction __instance, GameState gameState)
    {
        TileData tile = gameState.Map.GetTile(__instance.Coordinates);
        if (__instance.Type == EnumCache<ImprovementData.Type>.GetType("swarmcall"))
        {
            gameState.TryGetPlayer(__instance.PlayerId, out PlayerState playerState);
            foreach(TileData city in playerState.GetCityTiles(gameState))
            {
                if (!city.IsBeingCaptured(gameState))
                {
                    gameState.ActionStack.Add(new TrainAction(playerState.Id, EnumCache<UnitData.Type>.GetType("spearman"), city.coordinates, 0));
                    tile.unit.AddEffect(EnumCache<UnitEffect>.GetType("toporzolcalled"));
                }
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UnitDataExtensions), nameof(UnitDataExtensions.GetMaxHealth))]
    public static void IncreaseMaxHP(UnitState unitState, GameState gameState, ref int __result)
    {
        foreach(var effect in unitState.effects)
        {
            if(effect == EnumCache<UnitEffect>.GetType("toporzolcalled")) __result += 10;
        }
        __result += numOfCleanedEffects;
        modLogger.LogMessage("End result: "+__result+" of which numOfCleanedEffects: "+numOfCleanedEffects);
    }

    #endregion
}
