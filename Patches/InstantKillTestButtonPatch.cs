using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MysteriousMapMod.Map;

namespace MysteriousMapMod.Patches;

/// <summary>An opt-in, single-player-only combat shortcut for testing room routes.</summary>
[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
internal static class InstantKillTestButtonPatch
{
    private const string ButtonName = "MysteriousMapInstantKillTestButton";

    [HarmonyPostfix]
    private static void Postfix(NCombatRoom __instance)
    {
        var state = RunManager.Instance?.DebugOnlyGetState();
        if (state == null || !MysteriousMapSettings.ShowInstantKillButton ||
            !MysteriousMapRules.IsNestedMap(state.Map) || state.Players.Count != 1 ||
            state.CurrentRoom is not CombatRoom room ||
            LocalContext.GetMe(state.Players) == null)
            return;

        if (__instance.GetNodeOrNull<Button>(ButtonName) != null)
            return;

        var button = new Button
        {
            Name = ButtonName,
            Position = new Vector2(42f, 820f),
            Size = new Vector2(330f, 76f),
            Text = "测试 · 秒杀当前所有怪",
            ZIndex = 80
        };
        button.Pressed += async () =>
        {
            if (button.Disabled || state.CurrentRoom != room)
                return;
            var enemies = room.CombatState.Enemies.Where(enemy => !enemy.IsDead).ToArray();
            if (enemies.Length == 0)
                return;
            button.Disabled = true;
            try
            {
                await CreatureCmd.Kill(enemies, false);
            }
            catch (Exception exception)
            {
                Log.Error($"[MysteriousMap] Instant-kill test button failed: {exception}");
            }
            finally
            {
                if (GodotObject.IsInstanceValid(button) && button.IsInsideTree())
                    button.Disabled = false;
            }
        };
        __instance.AddChild(button);
    }
}
