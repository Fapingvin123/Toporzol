
using BepInEx.Logging;
using HarmonyLib;
using Polytopia.Data;
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

    // Trains a unit on a random valid neighboring tile
    // This does assume that the unit is land-only
    // true if could do so
    public static bool TrainAroundTile(byte id, UnitData.Type type, WorldCoordinates coords, int cost = 0)
    {
        GameState state = GameManager.GameState;
        List<WorldCoordinates> validtiles = new();
        foreach(var tile in state.Map.GetTileNeighbors(coords))
        {
            if(tile != null && !tile.IsWater && tile.unit == null) validtiles.Add(tile.coordinates);
        }

        if(validtiles.Count != 0)
        {
            state.ActionStack.Add(new TrainAction(id, type, validtiles[UnityEngine.Random.RandomRangeInt(0, validtiles.Count)], cost));
            return true;
        }
        return false;
    }


    /* Start with 2 spearmen instead of 1 */
    [HarmonyPrefix]
    [HarmonyPatch(typeof(StartMatchAction), nameof(StartMatchAction.ExecuteDefault))]
    public static void ExtraSpearmanAbility(GameState gameState, StartMatchAction __instance)
    {
        if(gameState.TryGetPlayer(__instance.PlayerId, out PlayerState playerState))
        {
            if (gameState.GameLogicData.GetTribeData(playerState.tribe).HasAbility(EnumCache<TribeAbility.Type>.GetType("swarmability")))
            {
                TrainAroundTile(playerState.Id, EnumCache<UnitData.Type>.GetType("spearman"), playerState.GetCurrentCapitalCoordinates(gameState));
            }
        }
    }
}
