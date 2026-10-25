using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MysteriousMapMod.Relics;

namespace MysteriousMapMod.Patches;

// The exported game has no ResourceLoader importer for loose PNGs inside a
// mod PCK. Decode the packed bytes explicitly and cache the resulting texture.
internal static class OwnedRelicTextures
{
    private static readonly Dictionary<string, Texture2D> Cache = new();

    internal static Texture2D Get(RelicModel relic)
    {
        var path = relic.IconPath;
        if (Cache.TryGetValue(path, out var texture))
            return texture;
        var bytes = Godot.FileAccess.GetFileAsBytes(path);
        if (bytes.Length == 0)
            throw new InvalidOperationException($"Relic artwork is missing from the mod package: {path}");
        var image = new Image();
        var error = image.LoadPngFromBuffer(bytes);
        if (error != Error.Ok)
            throw new InvalidOperationException($"Cannot decode relic artwork {path}: {error}");
        texture = ImageTexture.CreateFromImage(image);
        Cache[path] = texture;
        Log.Info($"[MysteriousMap] Loaded original relic artwork: {path}");
        return texture;
    }
}

[HarmonyPatch(typeof(RelicModel), "get_Icon")]
internal static class OwnedRelicIconPatch
{
    [HarmonyPrefix]
    private static bool Prefix(RelicModel __instance, ref Texture2D __result)
    {
        if (__instance is not IsaacMapRelic)
            return true;
        __result = OwnedRelicTextures.Get(__instance);
        return false;
    }
}

[HarmonyPatch(typeof(RelicModel), "get_IconOutline")]
internal static class OwnedRelicIconOutlinePatch
{
    [HarmonyPrefix]
    private static bool Prefix(RelicModel __instance, ref Texture2D __result)
    {
        if (__instance is not IsaacMapRelic)
            return true;
        __result = OwnedRelicTextures.Get(__instance);
        return false;
    }
}

[HarmonyPatch(typeof(RelicModel), "get_BigIcon")]
internal static class OwnedRelicBigIconPatch
{
    [HarmonyPrefix]
    private static bool Prefix(RelicModel __instance, ref Texture2D __result)
    {
        if (__instance is not IsaacMapRelic)
            return true;
        __result = OwnedRelicTextures.Get(__instance);
        return false;
    }
}
