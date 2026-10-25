using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace MysteriousMapMod.Patches;

internal static class MysteriousMapSettingsToggle
{
    internal enum Kind { None, Enabled, Mind, Compass, TreasureMap, BlueMap, StartWithDataminer, ShowInstantKillButton, RevealGlitchEffects }

    internal const string GroupLineName = "MysteriousMapSettingsGroup";
    internal const string GroupButtonName = "MysteriousMapSettingsGroupButton";
    internal const string EnabledLineName = "MysteriousMapEnabled";
    internal const string MindLineName = "MysteriousMapMind";
    internal const string CompassLineName = "MysteriousMapCompass";
    internal const string TreasureMapLineName = "MysteriousMapTreasureMap";
    internal const string BlueMapLineName = "MysteriousMapBlueMap";
    internal const string StartWithDataminerLineName = "MysteriousMapStartWithDataminer";
    internal const string ShowInstantKillButtonLineName = "MysteriousMapShowInstantKillButton";
    internal const string RevealGlitchEffectsLineName = "MysteriousMapRevealGlitchEffects";
    internal static NFastModeTickbox? EnabledInstance { get; set; }
    internal static NFastModeTickbox? MindInstance { get; set; }
    internal static NFastModeTickbox? CompassInstance { get; set; }
    internal static NFastModeTickbox? TreasureMapInstance { get; set; }
    internal static NFastModeTickbox? BlueMapInstance { get; set; }
    internal static NFastModeTickbox? StartWithDataminerInstance { get; set; }
    internal static NFastModeTickbox? ShowInstantKillButtonInstance { get; set; }
    internal static NFastModeTickbox? RevealGlitchEffectsInstance { get; set; }
    internal static readonly string[] OptionLineNames = [EnabledLineName, MindLineName, CompassLineName, TreasureMapLineName, BlueMapLineName, StartWithDataminerLineName, ShowInstantKillButtonLineName, RevealGlitchEffectsLineName];

    internal static Kind GetKind(NFastModeTickbox instance)
    {
        var lineName = instance.GetParent()?.Name.ToString();
        if (instance == EnabledInstance || instance.Name.ToString() == EnabledLineName || lineName == EnabledLineName)
            return Kind.Enabled;
        if (instance == MindInstance || instance.Name.ToString() == MindLineName || lineName == MindLineName)
            return Kind.Mind;
        if (instance == CompassInstance || instance.Name.ToString() == CompassLineName || lineName == CompassLineName)
            return Kind.Compass;
        if (instance == TreasureMapInstance || instance.Name.ToString() == TreasureMapLineName || lineName == TreasureMapLineName)
            return Kind.TreasureMap;
        if (instance == BlueMapInstance || instance.Name.ToString() == BlueMapLineName || lineName == BlueMapLineName)
            return Kind.BlueMap;
        if (instance == StartWithDataminerInstance || instance.Name.ToString() == StartWithDataminerLineName || lineName == StartWithDataminerLineName)
            return Kind.StartWithDataminer;
        if (instance == ShowInstantKillButtonInstance || instance.Name.ToString() == ShowInstantKillButtonLineName || lineName == ShowInstantKillButtonLineName)
            return Kind.ShowInstantKillButton;
        if (instance == RevealGlitchEffectsInstance || instance.Name.ToString() == RevealGlitchEffectsLineName || lineName == RevealGlitchEffectsLineName)
            return Kind.RevealGlitchEffects;
        return Kind.None;
    }

    internal static bool IsRevealOption(Kind kind) =>
        kind is Kind.Mind or Kind.Compass or Kind.TreasureMap or Kind.BlueMap;

    internal static bool GetValue(Kind kind) => kind switch
    {
        Kind.Enabled => MysteriousMapSettings.Enabled,
        Kind.Mind => MysteriousMapSettings.Mind,
        Kind.Compass => MysteriousMapSettings.Compass,
        Kind.TreasureMap => MysteriousMapSettings.TreasureMap,
        Kind.BlueMap => MysteriousMapSettings.BlueMap,
        Kind.StartWithDataminer => MysteriousMapSettings.StartWithDataminer,
        Kind.ShowInstantKillButton => MysteriousMapSettings.ShowInstantKillButton,
        Kind.RevealGlitchEffects => MysteriousMapSettings.RevealGlitchEffects,
        _ => false
    };

    internal static NSettingsScreen? FindSettingsScreen(Node node)
    {
        for (var current = node.GetParent(); current != null; current = current.GetParent())
            if (current is NSettingsScreen screen)
                return screen;
        return null;
    }
}

[HarmonyPatch(typeof(NSettingsScreen), nameof(NSettingsScreen._Ready))]
internal static class MysteriousMapSettingsScreenPatch
{
    [HarmonyPostfix]
    private static void Postfix(NSettingsScreen __instance)
    {
        try
        {
            var panel = __instance.GetNode<NSettingsPanel>("%GeneralSettings");
            var content = panel.Content;
            var source = content.GetNodeOrNull<Control>("FastMode");
            var groupSource = content.GetNodeOrNull<Control>("Modding");
            if (source == null || groupSource == null)
                return;

            var anchor = content.GetNodeOrNull<Node>("ModdingDivider");
            var insertionIndex = anchor?.GetIndex() ?? content.GetChildCount();
            var addedRows = 0;
            var group = content.GetNodeOrNull<Control>(MysteriousMapSettingsToggle.GroupLineName);
            if (group == null)
            {
                group = AddGroupRow(panel, groupSource, insertionIndex + addedRows);
                addedRows++;
            }
            if (content.GetNodeOrNull<Node>(MysteriousMapSettingsToggle.EnabledLineName) == null)
            {
                MysteriousMapSettingsToggle.EnabledInstance = AddRow(content, source,
                    MysteriousMapSettingsToggle.EnabledLineName, "MYSTERIOUS_MAP_ENABLED", insertionIndex + addedRows++);
            }
            if (content.GetNodeOrNull<Node>(MysteriousMapSettingsToggle.MindLineName) == null)
            {
                MysteriousMapSettingsToggle.MindInstance = AddRow(content, source,
                    MysteriousMapSettingsToggle.MindLineName, "MYSTERIOUS_MAP_MIND", insertionIndex + addedRows++);
            }
            if (content.GetNodeOrNull<Node>(MysteriousMapSettingsToggle.CompassLineName) == null)
            {
                MysteriousMapSettingsToggle.CompassInstance = AddRow(content, source,
                    MysteriousMapSettingsToggle.CompassLineName, "MYSTERIOUS_MAP_COMPASS", insertionIndex + addedRows++);
            }
            if (content.GetNodeOrNull<Node>(MysteriousMapSettingsToggle.TreasureMapLineName) == null)
            {
                MysteriousMapSettingsToggle.TreasureMapInstance = AddRow(content, source,
                    MysteriousMapSettingsToggle.TreasureMapLineName, "MYSTERIOUS_MAP_TREASURE_MAP", insertionIndex + addedRows++);
            }
            if (content.GetNodeOrNull<Node>(MysteriousMapSettingsToggle.BlueMapLineName) == null)
            {
                MysteriousMapSettingsToggle.BlueMapInstance = AddRow(content, source,
                    MysteriousMapSettingsToggle.BlueMapLineName, "MYSTERIOUS_MAP_BLUE_MAP", insertionIndex + addedRows++);
            }
            if (content.GetNodeOrNull<Node>(MysteriousMapSettingsToggle.StartWithDataminerLineName) == null)
            {
                MysteriousMapSettingsToggle.StartWithDataminerInstance = AddRow(content, source,
                    MysteriousMapSettingsToggle.StartWithDataminerLineName, "MYSTERIOUS_MAP_START_WITH_DATAMINER", insertionIndex + addedRows++);
            }
            if (content.GetNodeOrNull<Node>(MysteriousMapSettingsToggle.ShowInstantKillButtonLineName) == null)
            {
                MysteriousMapSettingsToggle.ShowInstantKillButtonInstance = AddRow(content, source,
                    MysteriousMapSettingsToggle.ShowInstantKillButtonLineName, "MYSTERIOUS_MAP_SHOW_INSTANT_KILL_BUTTON", insertionIndex + addedRows++);
            }
            if (content.GetNodeOrNull<Node>(MysteriousMapSettingsToggle.RevealGlitchEffectsLineName) == null)
            {
                MysteriousMapSettingsToggle.RevealGlitchEffectsInstance = AddRow(content, source,
                    MysteriousMapSettingsToggle.RevealGlitchEffectsLineName, "MYSTERIOUS_MAP_REVEAL_GLITCH_EFFECTS", insertionIndex + addedRows++);
            }

            if (MysteriousMapSettings.RevealOptionsLocked)
            {
                foreach (var name in new[]
                         {
                             MysteriousMapSettingsToggle.MindLineName,
                             MysteriousMapSettingsToggle.CompassLineName,
                             MysteriousMapSettingsToggle.TreasureMapLineName,
                             MysteriousMapSettingsToggle.BlueMapLineName
                         })
                {
                    if (content.GetNodeOrNull<Node>(name) is { } row && FindToggle(row) is { } toggle)
                    {
                        toggle.IsTicked = MysteriousMapSettingsToggle.GetValue(
                            MysteriousMapSettingsToggle.GetKind(toggle));
                        toggle.Disable();
                    }
                }
            }

            SetGroupExpanded(panel, group, expanded: true);
            var dividerSource = content.GetNodeOrNull<Node>("FastModeDivider");
            if (addedRows > 0 && dividerSource != null &&
                content.GetNodeOrNull<Node>("MysteriousMapSettingsDivider") == null)
            {
                var divider = dividerSource.Duplicate(15);
                divider.Name = "MysteriousMapSettingsDivider";
                content.AddChild(divider);
                content.MoveChild(divider, insertionIndex + addedRows);
            }
            panel.Call(NSettingsPanel.MethodName.RefreshSize);
            panel.Call(NSettingsPanel.MethodName.UpdateNavigation);
        }
        catch (Exception exception)
        {
            Log.Error($"[MysteriousMap] Failed to add settings group: {exception}");
        }
    }

    private static string Text(string key) => new LocString("main_menu_ui", key).GetFormattedText();

    private static NFastModeTickbox AddRow(Node content, Control source, string lineName, string labelKey, int index)
    {
        var line = (Control)source.Duplicate(6);
        line.Name = lineName;
        FixOwnerRecursive(line, line);
        var toggle = FindToggle(line)
            ?? throw new InvalidOperationException($"Settings row '{lineName}' has no tickbox.");
        SetLabel(line.GetNodeOrNull<Node>("Label"), Text(labelKey));
        content.AddChild(line);
        content.MoveChild(line, index);
        toggle.IsTicked = MysteriousMapSettingsToggle.GetValue(MysteriousMapSettingsToggle.GetKind(toggle));
        return toggle;
    }

    private static Control AddGroupRow(NSettingsPanel panel, Control source, int index)
    {
        var line = (Control)source.Duplicate(6);
        line.Name = MysteriousMapSettingsToggle.GroupLineName;
        FixOwnerRecursive(line, line);
        panel.Content.AddChild(line);
        panel.Content.MoveChild(line, index);
        line.Visible = true;
        SetLabel(line.GetNodeOrNull<Node>("Label"), Text("MYSTERIOUS_MAP_SETTINGS_GROUP"));
        var button = line.GetNodeOrNull<NOpenModdingScreenButton>("ModdingButton")
            ?? throw new InvalidOperationException("Duplicated settings group has no button.");
        button.Name = MysteriousMapSettingsToggle.GroupButtonName;
        button.Enable();
        button.Connect(NClickableControl.SignalName.Released,
            Callable.From<NButton>(_ => SetGroupExpanded(panel, line,
                !panel.Content.GetNode<Control>(MysteriousMapSettingsToggle.EnabledLineName).Visible)));
        SetGroupButtonLabel(line, expanded: false);
        return line;
    }

    private static void SetGroupExpanded(NSettingsPanel panel, Control group, bool expanded)
    {
        foreach (var name in MysteriousMapSettingsToggle.OptionLineNames)
            if (panel.Content.GetNodeOrNull<Control>(name) is { } option)
                option.Visible = expanded;
        SetGroupButtonLabel(group, expanded);
        panel.Call(NSettingsPanel.MethodName.RefreshSize);
        panel.Call(NSettingsPanel.MethodName.UpdateNavigation);
    }

    private static void SetGroupButtonLabel(Control group, bool expanded)
    {
        var button = group.GetNodeOrNull<Node>(MysteriousMapSettingsToggle.GroupButtonName)
            ?? group.GetNodeOrNull<Node>("ModdingButton");
        SetLabel(button?.GetNodeOrNull<Node>("Label"), Text(expanded
            ? "MYSTERIOUS_MAP_SETTINGS_COLLAPSE"
            : "MYSTERIOUS_MAP_SETTINGS_EXPAND"));
    }

    private static void SetLabel(Node? node, string value)
    {
        switch (node)
        {
            case MegaRichTextLabel rich: rich.Text = value; break;
            case MegaLabel mega: mega.SetTextAutoSize(value); break;
            case RichTextLabel rich: rich.Text = value; break;
            case Label label: label.Text = value; break;
        }
    }

    private static NFastModeTickbox? FindToggle(Node root)
    {
        if (root is NFastModeTickbox toggle)
            return toggle;
        foreach (var child in root.GetChildren())
            if (FindToggle(child) is { } found)
                return found;
        return null;
    }

    private static void FixOwnerRecursive(Node root, Node owner)
    {
        foreach (var child in root.GetChildren())
        {
            child.Owner = owner;
            FixOwnerRecursive(child, owner);
        }
    }
}

[HarmonyPatch(typeof(NFastModeTickbox), nameof(NFastModeTickbox.SetFromSettings))]
internal static class MysteriousMapSettingsSetFromSettingsPatch
{
    [HarmonyPrefix]
    private static bool Prefix(NFastModeTickbox __instance)
    {
        var kind = MysteriousMapSettingsToggle.GetKind(__instance);
        if (kind == MysteriousMapSettingsToggle.Kind.None)
            return true;
        __instance.IsTicked = MysteriousMapSettingsToggle.GetValue(kind);
        if (MysteriousMapSettings.RevealOptionsLocked && MysteriousMapSettingsToggle.IsRevealOption(kind))
            __instance.Disable();
        return false;
    }
}

[HarmonyPatch(typeof(NFastModeTickbox), "OnTick")]
internal static class MysteriousMapSettingsOnTickPatch
{
    [HarmonyPrefix]
    private static bool Prefix(NFastModeTickbox __instance)
    {
        var kind = MysteriousMapSettingsToggle.GetKind(__instance);
        if (kind == MysteriousMapSettingsToggle.Kind.None)
            return true;
        if (MysteriousMapSettings.RevealOptionsLocked && MysteriousMapSettingsToggle.IsRevealOption(kind))
        {
            __instance.IsTicked = MysteriousMapSettingsToggle.GetValue(kind);
            MysteriousMapSettingsToggle.FindSettingsScreen(__instance)?.ShowToast(
                new LocString("main_menu_ui", "MYSTERIOUS_MAP_REVEAL_OPTIONS_LOCKED"));
            return false;
        }
        Set(kind, true);
        MysteriousMapSettingsToggle.FindSettingsScreen(__instance)?.ShowToast(
            new LocString("main_menu_ui", ToastKey(kind, true)));
        return false;
    }

    internal static void Set(MysteriousMapSettingsToggle.Kind kind, bool value)
    {
        switch (kind)
        {
            case MysteriousMapSettingsToggle.Kind.Enabled: MysteriousMapSettings.Enabled = value; break;
            case MysteriousMapSettingsToggle.Kind.Mind: MysteriousMapSettings.Mind = value; break;
            case MysteriousMapSettingsToggle.Kind.Compass: MysteriousMapSettings.Compass = value; break;
            case MysteriousMapSettingsToggle.Kind.TreasureMap: MysteriousMapSettings.TreasureMap = value; break;
            case MysteriousMapSettingsToggle.Kind.BlueMap: MysteriousMapSettings.BlueMap = value; break;
            case MysteriousMapSettingsToggle.Kind.StartWithDataminer: MysteriousMapSettings.StartWithDataminer = value; break;
            case MysteriousMapSettingsToggle.Kind.ShowInstantKillButton: MysteriousMapSettings.ShowInstantKillButton = value; break;
            case MysteriousMapSettingsToggle.Kind.RevealGlitchEffects: MysteriousMapSettings.RevealGlitchEffects = value; break;
        }
    }

    internal static string ToastKey(MysteriousMapSettingsToggle.Kind kind, bool enabled) =>
        kind switch
        {
            MysteriousMapSettingsToggle.Kind.Enabled => enabled ? "MYSTERIOUS_MAP_ENABLED_ON" : "MYSTERIOUS_MAP_ENABLED_OFF",
            MysteriousMapSettingsToggle.Kind.Mind => enabled ? "MYSTERIOUS_MAP_MIND_ON" : "MYSTERIOUS_MAP_MIND_OFF",
            MysteriousMapSettingsToggle.Kind.Compass => enabled ? "MYSTERIOUS_MAP_COMPASS_ON" : "MYSTERIOUS_MAP_COMPASS_OFF",
            MysteriousMapSettingsToggle.Kind.TreasureMap => enabled ? "MYSTERIOUS_MAP_TREASURE_MAP_ON" : "MYSTERIOUS_MAP_TREASURE_MAP_OFF",
            MysteriousMapSettingsToggle.Kind.StartWithDataminer => enabled ? "MYSTERIOUS_MAP_START_WITH_DATAMINER_ON" : "MYSTERIOUS_MAP_START_WITH_DATAMINER_OFF",
            MysteriousMapSettingsToggle.Kind.ShowInstantKillButton => enabled ? "MYSTERIOUS_MAP_SHOW_INSTANT_KILL_BUTTON_ON" : "MYSTERIOUS_MAP_SHOW_INSTANT_KILL_BUTTON_OFF",
            MysteriousMapSettingsToggle.Kind.RevealGlitchEffects => enabled ? "MYSTERIOUS_MAP_REVEAL_GLITCH_EFFECTS_ON" : "MYSTERIOUS_MAP_REVEAL_GLITCH_EFFECTS_OFF",
            _ => enabled ? "MYSTERIOUS_MAP_BLUE_MAP_ON" : "MYSTERIOUS_MAP_BLUE_MAP_OFF"
        };
}

[HarmonyPatch(typeof(NFastModeTickbox), "OnUntick")]
internal static class MysteriousMapSettingsOnUntickPatch
{
    [HarmonyPrefix]
    private static bool Prefix(NFastModeTickbox __instance)
    {
        var kind = MysteriousMapSettingsToggle.GetKind(__instance);
        if (kind == MysteriousMapSettingsToggle.Kind.None)
            return true;
        if (MysteriousMapSettings.RevealOptionsLocked && MysteriousMapSettingsToggle.IsRevealOption(kind))
        {
            __instance.IsTicked = MysteriousMapSettingsToggle.GetValue(kind);
            MysteriousMapSettingsToggle.FindSettingsScreen(__instance)?.ShowToast(
                new LocString("main_menu_ui", "MYSTERIOUS_MAP_REVEAL_OPTIONS_LOCKED"));
            return false;
        }
        MysteriousMapSettingsOnTickPatch.Set(kind, false);
        MysteriousMapSettingsToggle.FindSettingsScreen(__instance)?.ShowToast(
            new LocString("main_menu_ui", MysteriousMapSettingsOnTickPatch.ToastKey(kind, false)));
        return false;
    }
}

[HarmonyPatch(typeof(NFastModeHoverTip), "OnHovered")]
internal static class MysteriousMapSettingsHoverTipPatch
{
    [HarmonyPrefix]
    private static bool Prefix(NFastModeHoverTip __instance)
    {
        var (title, description) = __instance.Name.ToString() switch
        {
            MysteriousMapSettingsToggle.EnabledLineName => ("MYSTERIOUS_MAP_ENABLED", "MYSTERIOUS_MAP_ENABLED_DESCRIPTION"),
            MysteriousMapSettingsToggle.MindLineName => ("MYSTERIOUS_MAP_MIND", "MYSTERIOUS_MAP_MIND_DESCRIPTION"),
            MysteriousMapSettingsToggle.CompassLineName => ("MYSTERIOUS_MAP_COMPASS", "MYSTERIOUS_MAP_COMPASS_DESCRIPTION"),
            MysteriousMapSettingsToggle.TreasureMapLineName => ("MYSTERIOUS_MAP_TREASURE_MAP", "MYSTERIOUS_MAP_TREASURE_MAP_DESCRIPTION"),
            MysteriousMapSettingsToggle.BlueMapLineName => ("MYSTERIOUS_MAP_BLUE_MAP", "MYSTERIOUS_MAP_BLUE_MAP_DESCRIPTION"),
            MysteriousMapSettingsToggle.StartWithDataminerLineName => ("MYSTERIOUS_MAP_START_WITH_DATAMINER", "MYSTERIOUS_MAP_START_WITH_DATAMINER_DESCRIPTION"),
            MysteriousMapSettingsToggle.ShowInstantKillButtonLineName => ("MYSTERIOUS_MAP_SHOW_INSTANT_KILL_BUTTON", "MYSTERIOUS_MAP_SHOW_INSTANT_KILL_BUTTON_DESCRIPTION"),
            MysteriousMapSettingsToggle.RevealGlitchEffectsLineName => ("MYSTERIOUS_MAP_REVEAL_GLITCH_EFFECTS", "MYSTERIOUS_MAP_REVEAL_GLITCH_EFFECTS_DESCRIPTION"),
            _ => (null, null)
        };
        if (title == null || description == null)
            return true;
        var tip = new HoverTip(new LocString("main_menu_ui", title), new LocString("main_menu_ui", description));
        NHoverTipSet.CreateAndShow(__instance, tip)?
            .SetGlobalPosition(__instance.GlobalPosition + NSettingsScreen.settingTipsOffset);
        return false;
    }
}
