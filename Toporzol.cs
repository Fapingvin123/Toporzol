
using BepInEx.Logging;
using HarmonyLib;
using Polytopia.Data;
using PolytopiaBackendBase.Game;
using UnityEngine;

namespace Toporzol;



public static class Main
{
    public static ManualLogSource modLogger;
    public static void Load(ManualLogSource logger)
    {
        PolyMod.Loader.AddPatchDataType("unitEffect", typeof(UnitEffect));
        Harmony.CreateAndPatchAll(typeof(Main));
        Harmony.CreateAndPatchAll(typeof(MagicSystemUI));
        modLogger = logger;
        logger.LogMessage("Toporzol.dll loaded.");
    }

    #region Utils

    /// <summary>
    /// Trains a land unit on a random valid neighboring tile, returns success.
    /// </summary>
    public static UnitState TrainAroundTile(PlayerState player, UnitData data, WorldCoordinates coords, bool swtch = false)
    {
        GameState state = GameManager.GameState;
        List<WorldCoordinates> validtiles = new();
        foreach (var tile in state.Map.GetTileNeighbors(coords))
        {
            if (tile != null && !tile.IsWater && tile.unit == null) validtiles.Add(tile.coordinates);
        }

        if (validtiles.Count != 0)
        {
            if(swtch){state.ActionStack.Add(new TrainAction(player.Id, data.type, validtiles[UnityEngine.Random.RandomRangeInt(0, validtiles.Count)], 0)); return null;}
            return ActionUtils.TrainUnitScored(state, player, state.Map.GetTile(validtiles[UnityEngine.Random.RandomRangeInt(0, validtiles.Count)]), data);
        }
        return null;
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
                UnitState secondspearman = TrainAroundTile(playerState, gameState.GameLogicData.GetUnitData(EnumCache<UnitData.Type>.GetType("toporzolspearman")), playerState.GetCurrentCapitalCoordinates(gameState));
                if (secondspearman == null)
                {
                    NotificationManager.Notify(Localization.Get("toporzol.nospearmanmsg"), Localization.Get("toporzol.nospearmantitle"));
                    gameState.ActionStack.Add(new IncreaseCurrencyAction(playerState.Id, playerState.GetCurrentCapitalCoordinates(gameState), 2, 0));
                }
                else
                {
                    secondspearman.moved = false;
                    secondspearman.attacked = false;
                }

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

    /*
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
    */

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameLogicData), nameof(GameLogicData.CanBuild))]
    public static void CanSwarmcall(GameState gameState, TileData tile, PlayerState playerState, ImprovementData improvement, ref bool __result)
    {
        if (improvement.type == EnumCache<ImprovementData.Type>.GetType("swarmcall"))
        {
            if (tile.unit == null || !tile.unit.HasAbility(EnumCache<UnitAbility.Type>.GetType("swarmcalling")) || tile.unit.HasEffect(EnumCache<UnitEffect>.GetType("toporzolcalled")))
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
            tile.unit.AddEffect(EnumCache<UnitEffect>.GetType("toporzolcalled"));
            foreach (TileData city in playerState.GetCityTiles(gameState))
            {
                //gameState.ActionStack.Add(new TrainAction(playerState.Id, EnumCache<UnitData.Type>.GetType("spearman"), city.coordinates, 0));
                TrainAroundTile(playerState, gameState.GameLogicData.GetUnitData(EnumCache<UnitData.Type>.GetType("toporzolspearman")), city.coordinates, true);
                
            }
        }
    }

    /*[HarmonyPostfix]
    [HarmonyPatch(typeof(UnitDataExtensions), nameof(UnitDataExtensions.GetMaxHealth))]
    public static void IncreaseMaxHP(UnitState unitState, GameState gameState, ref int __result)
    {
        foreach (var effect in unitState.effects)
        {
            if (effect == EnumCache<UnitEffect>.GetType("toporzolcalled")) __result += 10;
        }
        __result += numOfCleanedEffects;
        modLogger.LogMessage("End result: " + __result + " of which numOfCleanedEffects: " + numOfCleanedEffects);
    }*/

    #endregion

    #region Bulky
    private static bool IsTrainingBulkyUnit = false;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(TrainCommand), nameof(TrainCommand.IsValid))]
    private static void MaybeWantsToTrainABulkyUnit(TrainCommand __instance, GameState state, string validationError)
    {
        if (state.GameLogicData.GetUnitData(__instance.Type).HasAbility(EnumCache<UnitAbility.Type>.GetType("toporzolbulky")))
            IsTrainingBulkyUnit = true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TrainCommand), nameof(TrainCommand.IsValid))]
    private static void PrePostDisable(TrainCommand __instance, GameState state, string validationError)
    {
        IsTrainingBulkyUnit = false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CommandValidation), nameof(CommandValidation.CanCitySupportUnit))]
    private static void BulkyNeedsMorePopulation(ref bool __result, GameState state, WorldCoordinates coordinates)
    {
        if (IsTrainingBulkyUnit)
        {
            TileData tile = state.Map.GetTile(coordinates);
            TileData tile2 = state.Map.GetTile(tile.rulingCityCoordinates);
            if (tile2 == null || tile2.improvement == null || tile2.improvement.type != ImprovementData.Type.City)
            {
                return;
            }
            __result = state.Map.GetCityUnitCount(tile2.coordinates) < tile2.improvement.level;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MapDataExtensions), nameof(MapDataExtensions.GetCityUnitCount))]
    private static void CountBulkiesOnceAgain(this MapData mapData, WorldCoordinates cityCoordinates, ref int __result)
    {
        
		int num = 0;
		TileData[] tiles = mapData.Tiles;
		foreach (TileData tileData in tiles)
		{
			if (tileData.unit != null && tileData.unit.home == cityCoordinates && tileData.unit.HasAbility(EnumCache<UnitAbility.Type>.GetType("toporzolbulky")))
			{
				num++;
			}
		}
        __result += num;
    }
    #endregion
}
