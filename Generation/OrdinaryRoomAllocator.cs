namespace MysteriousMapMod.Generation;

internal static class OrdinaryRoomAllocator
{
    internal const double BaseUnknownRoomChance = 0.33;
    internal const int AdditionalUnknownRooms = 1;

    internal static int GetUnknownRoomCount(int ordinaryRoomCount)
    {
        if (ordinaryRoomCount < 0)
            throw new ArgumentOutOfRangeException(nameof(ordinaryRoomCount));

        var vanillaRatioCount = (int)Math.Round(
            ordinaryRoomCount * BaseUnknownRoomChance,
            MidpointRounding.AwayFromZero);
        return Math.Min(ordinaryRoomCount, vanillaRatioCount + AdditionalUnknownRooms);
    }
}
