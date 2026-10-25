using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MysteriousMapMod.Map;

namespace MysteriousMapMod.Patches;

[HarmonyPatch(typeof(RunManager), nameof(RunManager.ToSave))]
internal static class SaveHiddenRoomRevealsPatch
{
    [HarmonyPostfix]
    private static void Postfix(RunManager __instance, ref SerializableRun __result)
    {
        var state = __instance.DebugOnlyGetState();
        if (state == null)
            return;
        _ = HiddenRoomState.ReadFrom(__result, remove: true);
        var payload = HiddenRoomState.Capture(state);
        if (!string.IsNullOrWhiteSpace(payload))
            __result.Modifiers.Add(HiddenRoomState.ToSerializableModifier(payload));
    }
}

[HarmonyPatch(typeof(RunState), nameof(RunState.FromSerializable))]
internal static class RestoreHiddenRoomRevealsPatch
{
    [HarmonyPrefix]
    private static void Prefix(SerializableRun save, out string? __state) =>
        __state = HiddenRoomState.ReadFrom(save, remove: false);

    [HarmonyPostfix]
    private static void Postfix(RunState __result, string? __state)
    {
        try
        {
            HiddenRoomState.Restore(__result, __state);
        }
        catch (Exception exception)
        {
            Log.Warn($"[MysteriousMap] Ignoring invalid hidden-room reveal state: {exception.Message}");
        }

        var modifiers = __result.Modifiers
            .Where(modifier => modifier is not MysteriousMapHiddenRoomStateModifier)
            .ToArray();
        if (modifiers.Length != __result.Modifiers.Count)
            AccessTools.Property(typeof(RunState), nameof(RunState.Modifiers)).SetValue(__result, modifiers);
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.CanonicalizeSave))]
internal static class PreserveHiddenRoomRevealsDuringCanonicalizationPatch
{
    [HarmonyPrefix]
    private static void Prefix(SerializableRun save, out string? __state) =>
        __state = HiddenRoomState.ReadFrom(save, remove: false);

    [HarmonyPostfix]
    private static void Postfix(ref SerializableRun __result, string? __state)
    {
        if (string.IsNullOrWhiteSpace(__state))
            return;
        _ = HiddenRoomState.ReadFrom(__result, remove: true);
        __result.Modifiers.Add(HiddenRoomState.ToSerializableModifier(__state));
    }
}
