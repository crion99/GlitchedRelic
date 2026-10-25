using MegaCrit.Sts2.Core.Map;
using MysteriousMapMod.Generation;

namespace MysteriousMapMod.Map;

/// <summary>
/// Encodes each outer room in a non-overlapping 5x5 block of the game's map
/// coordinate space. This lets vanilla save data retain the nested structure.
/// </summary>
internal static class NestedMapCoordinates
{
    public const int BlockSize = 6;
    public const int GridSize = NestedFloorGenerator.OuterSize * BlockSize;

    public static MapCoord ToCoord(GridCell outer, InnerNodeRole role)
    {
        var (col, row) = role switch
        {
            InnerNodeRole.Entry => (2, 0),
            InnerNodeRole.ChoiceLeft => (1, 1),
            InnerNodeRole.ChoiceRight => (3, 1),
            InnerNodeRole.BranchLeft => (0, 2),
            InnerNodeRole.BranchMiddle => (2, 2),
            InnerNodeRole.BranchRight => (4, 2),
            InnerNodeRole.Reward => (2, 3),
            InnerNodeRole.Exit => (2, 4),
            InnerNodeRole.Boss => (2, 1),
            InnerNodeRole.SecondBoss => (2, 2),
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        };
        return new MapCoord(outer.Col * BlockSize + col, outer.Row * BlockSize + row);
    }

    public static bool TryDecode(MapCoord coord, out GridCell outer, out InnerNodeRole role)
    {
        outer = new GridCell(coord.col / BlockSize, coord.row / BlockSize);
        var local = (Col: coord.col % BlockSize, Row: coord.row % BlockSize);
        role = local switch
        {
            (2, 0) => InnerNodeRole.Entry,
            (1, 1) => InnerNodeRole.ChoiceLeft,
            (3, 1) => InnerNodeRole.ChoiceRight,
            (0, 2) => InnerNodeRole.BranchLeft,
            (2, 2) => InnerNodeRole.BranchMiddle,
            (4, 2) => InnerNodeRole.BranchRight,
            (2, 3) => InnerNodeRole.Reward,
            (2, 4) => InnerNodeRole.Exit,
            _ => default
        };
        return local is (2, 0) or (1, 1) or (3, 1) or (0, 2) or (2, 2) or (4, 2) or (2, 3) or (2, 4);
    }
}
