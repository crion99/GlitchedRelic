using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MysteriousMapMod.Map;
using MysteriousMapMod.Patches;

namespace MysteriousMapMod;

internal static class MysteriousMapSettings
{
    private sealed class SettingsData
    {
        public int Schema { get; set; } = 5;
        public bool Enabled { get; set; } = true;
        public bool Mind { get; set; }
        public bool Compass { get; set; }
        public bool TreasureMap { get; set; }
        public bool BlueMap { get; set; }
        public bool StartWithDataminer { get; set; }
        public bool ShowInstantKillButton { get; set; }
        public bool RevealGlitchEffects { get; set; } = true;
    }

    private static readonly string SettingsPath = Path.Combine(
        OS.GetUserDataDir(), "MysteriousMap", "settings.json");
    private static bool _loaded;
    private static bool _enabled = true;
    private static bool _mind;
    private static bool _compass;
    private static bool _treasureMap;
    private static bool _blueMap;
    private static bool _startWithDataminer;
    private static bool _showInstantKillButton;
    private static bool _revealGlitchEffects = true;
    private static MysteriousMapRevealSettings? _multiplayerRevealSettings;

    internal static bool RevealOptionsLocked => _multiplayerRevealSettings.HasValue;

    internal static bool Enabled
    {
        get { EnsureLoaded(); return _enabled; }
        set
        {
            EnsureLoaded();
            if (_enabled == value)
                return;
            _enabled = value;
            Save();
            Log.Info($"[MysteriousMap] Map mode {(value ? "enabled" : "disabled")}; the change applies to newly generated acts. Existing MysteriousMap acts remain playable.");
        }
    }

    internal static bool Mind
    {
        get { EnsureLoaded(); return _multiplayerRevealSettings?.Mind ?? _mind; }
        set => Set(ref _mind, value, "The Mind");
    }

    internal static bool Compass
    {
        get { EnsureLoaded(); return _multiplayerRevealSettings?.Compass ?? _compass; }
        set => Set(ref _compass, value, "The Compass");
    }

    internal static bool TreasureMap
    {
        get { EnsureLoaded(); return _multiplayerRevealSettings?.TreasureMap ?? _treasureMap; }
        set => Set(ref _treasureMap, value, "Treasure Map");
    }

    internal static bool BlueMap
    {
        get { EnsureLoaded(); return _multiplayerRevealSettings?.BlueMap ?? _blueMap; }
        set => Set(ref _blueMap, value, "Blue Map");
    }

    internal static bool StartWithDataminer
    {
        get { EnsureLoaded(); return _startWithDataminer; }
        set
        {
            EnsureLoaded();
            if (_startWithDataminer == value)
                return;
            _startWithDataminer = value;
            Save();
            Log.Info($"[MysteriousMap] Start with Dataminer {(value ? "enabled" : "disabled")}; " +
                     "applies to new solo runs or the host's next multiplayer run.");
        }
    }

    internal static bool ShowInstantKillButton
    {
        get { EnsureLoaded(); return _showInstantKillButton; }
        set
        {
            EnsureLoaded();
            if (_showInstantKillButton == value)
                return;
            _showInstantKillButton = value;
            Save();
            Log.Info($"[MysteriousMap] Instant-kill test button {(value ? "enabled" : "disabled")}; applies to newly entered single-player combats.");
        }
    }

    internal static bool RevealGlitchEffects
    {
        get { EnsureLoaded(); return _revealGlitchEffects; }
        set
        {
            EnsureLoaded();
            if (_revealGlitchEffects == value)
                return;
            _revealGlitchEffects = value;
            Save();
        }
    }

    internal static void EnsureLoaded()
    {
        if (_loaded)
            return;
        _loaded = true;
        try
        {
            if (!File.Exists(SettingsPath))
                return;
            var data = JsonSerializer.Deserialize<SettingsData>(File.ReadAllText(SettingsPath));
            if (data == null)
                return;
            _enabled = data.Enabled;
            _mind = data.Mind;
            _compass = data.Compass;
            _treasureMap = data.TreasureMap;
            _blueMap = data.BlueMap;
            _startWithDataminer = data.StartWithDataminer;
            _showInstantKillButton = data.ShowInstantKillButton;
            _revealGlitchEffects = data.RevealGlitchEffects;
        }
        catch (Exception exception)
        {
            _enabled = true;
            _mind = false;
            _compass = false;
            _treasureMap = false;
            _blueMap = false;
            _startWithDataminer = false;
            _showInstantKillButton = false;
            _revealGlitchEffects = true;
            Log.Error($"[MysteriousMap] Failed to load settings; using defaults: {exception}");
        }
    }

    internal static MysteriousMapRevealSettings CaptureLocalRevealSettings()
    {
        EnsureLoaded();
        return new MysteriousMapRevealSettings(_mind, _compass, _treasureMap, _blueMap);
    }

    internal static void ApplyMultiplayerRevealSettings(MysteriousMapRevealSettings settings)
    {
        EnsureLoaded();
        _multiplayerRevealSettings = settings;
        Log.Info($"[MysteriousMap] Applied host reveal settings: mask={settings.ToMask()}.");
        MapVisibilityController.RefreshCurrentMap();
    }

    internal static void ClearMultiplayerRevealSettings()
    {
        if (!_multiplayerRevealSettings.HasValue)
            return;
        _multiplayerRevealSettings = null;
        MapVisibilityController.RefreshCurrentMap();
    }

    private static void Set(ref bool field, bool value, string name)
    {
        EnsureLoaded();
        if (_multiplayerRevealSettings.HasValue)
        {
            Log.Info($"[MysteriousMap] Ignored local {name} change during a host-authoritative multiplayer run.");
            return;
        }
        if (field == value)
            return;
        field = value;
        Save();
        Log.Info($"[MysteriousMap] {name} {(value ? "enabled" : "disabled")}.");
        MapVisibilityController.RefreshCurrentMap();
    }

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(
                new SettingsData
                {
                    Enabled = _enabled,
                    Mind = _mind,
                    Compass = _compass,
                    TreasureMap = _treasureMap,
                    BlueMap = _blueMap,
                    StartWithDataminer = _startWithDataminer,
                    ShowInstantKillButton = _showInstantKillButton,
                    RevealGlitchEffects = _revealGlitchEffects
                },
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception exception)
        {
            Log.Error($"[MysteriousMap] Failed to save settings: {exception}");
        }
    }
}
