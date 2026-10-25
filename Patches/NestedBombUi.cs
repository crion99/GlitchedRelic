using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using MysteriousMapMod.Map;
using MysteriousMapMod.Rooms;
using MegaCrit.Sts2.Core.Context;

namespace MysteriousMapMod.Patches;

/// <summary>
/// Shows the four wall actions throughout a nested room. A failed probe still
/// consumes the bomb.
/// </summary>
[HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.SetMap))]
internal static class NestedBombUi
{
    private const string RootName = "MysteriousMapBombWallActions";
    private static readonly FieldInfo RunStateField = AccessTools.Field(typeof(NMapScreen), "_runState");
    private static readonly MethodInfo RecalculateTravelabilityMethod =
        AccessTools.Method(typeof(NMapScreen), "RecalculateTravelability");

    [HarmonyPostfix]
    private static void Postfix(NMapScreen __instance)
    {
        var runState = (RunState?)RunStateField.GetValue(__instance);
        if (runState == null || !MysteriousMapRules.IsNestedMap(runState.Map))
            return;
        Attach(__instance, runState);
    }

    internal static void RefreshAfterRemoteBomb(NMapScreen screen, RunState state)
    {
        RecalculateTravelabilityMethod.Invoke(screen, null);
        MapVisibilityController.Apply(screen);
        Attach(screen, state);
    }

    private static void Attach(NMapScreen screen, RunState runState, string? result = null)
    {
        var oldRoot = screen.GetNodeOrNull<Control>(RootName);
        if (oldRoot != null)
        {
            screen.RemoveChild(oldRoot);
            oldRoot.QueueFree();
        }

        var panel = new PanelContainer
        {
            Name = RootName,
            Position = new Vector2(360f, 185f),
            Size = new Vector2(460f, 130f),
            ZIndex = 100,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.09f, 0.08f, 0.14f, 0.90f),
            BorderColor = new Color(0.75f, 0.62f, 0.35f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            ContentMarginLeft = 12,
            ContentMarginTop = 8,
            ContentMarginRight = 12,
            ContentMarginBottom = 8
        };
        panel.AddThemeStyleboxOverride("panel", style);
        var content = new VBoxContainer();
        panel.AddChild(content);
        var count = HiddenRoomState.GetBombCount(runState);
        content.AddChild(new Label
        {
            Text = $"💣 炸墙 · 炸弹 {count}"
        });
        content.AddChild(new Label
        {
            Text = result ?? (runState.VisitedMapCoords.Count == 0
                ? "进入房间后可选择墙面"
                : count == 0 ? "暂无炸弹；可探索或在商店购买" : "选择当前房间的一面墙")
        });
        var buttons = new HBoxContainer();
        content.AddChild(buttons);
        foreach (var (text, col, row) in new[]
                 {
                     ("← 左墙", -1, 0), ("↑ 上墙", 0, 1),
                     ("↓ 下墙", 0, -1), ("→ 右墙", 1, 0)
                 })
        {
            var button = new Button
            {
                Text = text,
                CustomMinimumSize = new Vector2(92f, 45f),
                Disabled = !HiddenRoomState.CanBombWall(runState, col, row)
            };
            button.Pressed += () =>
            {
                if (!HiddenRoomState.TryBombWall(runState, col, row, out var discovered))
                    return;
                var localPlayer = LocalContext.GetMe(runState.Players);
                if (localPlayer != null && runState.CurrentMapCoord is { } source)
                    ShopInventoryMultiplayerSynchronizer.PublishBombAction(
                        runState, localPlayer, 2, source, col, row);
                Log.Info(discovered
                    ? $"[MysteriousMap] Bombed {text} and discovered a hidden room."
                    : $"[MysteriousMap] Bombed {text}; the wall was empty.");
                RecalculateTravelabilityMethod.Invoke(screen, null);
                MapVisibilityController.Apply(screen);
                Attach(screen, runState, discovered ? "炸开了隐藏房！" : "这面墙后面没有房间");
            };
            buttons.AddChild(button);
        }
        screen.AddChild(panel);
    }
}
