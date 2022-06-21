using Microsoft.Xna.Framework;

namespace CustomTreeLib.DataStructures
{
    /// <summary>
    /// Tree tile with position and its info
    /// </summary>
    public record struct PositionedTreeTile(Point Pos, TreeTileInfo Info);
}
