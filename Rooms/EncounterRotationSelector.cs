namespace MysteriousMapMod.Rooms;

internal static class EncounterRotationSelector
{
    public static int SelectIndex(
        int count,
        uint start,
        Func<int, bool> isPreferred,
        Func<int, bool> isFallback)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        var startIndex = (int)(start % (uint)count);
        for (var offset = 0; offset < count; offset++)
        {
            var index = (startIndex + offset) % count;
            if (isPreferred(index))
                return index;
        }

        // Match the vanilla encounter builder's two-stage behavior: if tag
        // separation is impossible, relax that constraint before allowing an
        // exact immediate repeat.
        for (var offset = 0; offset < count; offset++)
        {
            var index = (startIndex + offset) % count;
            if (isFallback(index))
                return index;
        }

        return startIndex;
    }
}
