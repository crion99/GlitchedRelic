using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MysteriousMapMod.Rooms;

namespace MysteriousMapMod.Patches;

[HarmonyPatch(typeof(RunManager), nameof(RunManager.ToSave))]
internal static class SaveReusableShopsPatch
{
    [HarmonyPostfix]
    private static void Postfix(RunManager __instance, ref SerializableRun __result)
    {
        var state = __instance.DebugOnlyGetState();
        if (state == null)
            return;

        _ = ShopInventoryPersistence.ReadFrom(__result, remove: true);
        var payload = RoomFactory.CaptureShopSnapshots(state);
        if (!string.IsNullOrWhiteSpace(payload))
            __result.Modifiers.Add(ShopInventoryPersistence.ToSerializableModifier(payload));
    }
}

[HarmonyPatch(typeof(RunState), nameof(RunState.FromSerializable))]
internal static class RestoreReusableShopsPatch
{
    [HarmonyPrefix]
    private static void Prefix(SerializableRun save, out string? __state)
    {
        // Do not consume the marker: the game can materialize multiple RunStates
        // from one SerializableRun while saving, replaying, or returning to menu.
        __state = ShopInventoryPersistence.ReadFrom(save, remove: false);
    }

    [HarmonyPostfix]
    private static void Postfix(RunState __result, string? __state)
    {
        if (!string.IsNullOrWhiteSpace(__state))
        {
            try
            {
                RoomFactory.RestoreShopSnapshots(__result, __state);
            }
            catch (Exception exception)
            {
                Log.Warn($"[MysteriousMap] Ignoring an invalid saved shop snapshot: {exception.Message}");
            }
        }

        // This modifier is only a serialization carrier and must not appear in
        // ordinary modifier hooks or the top-bar modifier display.
        var modifiers = __result.Modifiers
            .Where(modifier => modifier is not MysteriousMapShopSnapshotModifier)
            .ToArray();
        if (modifiers.Length != __result.Modifiers.Count)
            AccessTools.Property(typeof(RunState), nameof(RunState.Modifiers)).SetValue(__result, modifiers);
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.CanonicalizeSave))]
internal static class PreserveReusableShopsDuringCanonicalizationPatch
{
    [HarmonyPrefix]
    private static void Prefix(SerializableRun save, out string? __state)
    {
        __state = ShopInventoryPersistence.ReadFrom(save, remove: false);
    }

    [HarmonyPostfix]
    private static void Postfix(ref SerializableRun __result, string? __state)
    {
        if (string.IsNullOrWhiteSpace(__state))
            return;

        _ = ShopInventoryPersistence.ReadFrom(__result, remove: true);
        __result.Modifiers.Add(ShopInventoryPersistence.ToSerializableModifier(__state));
    }
}
