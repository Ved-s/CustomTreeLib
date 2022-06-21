namespace CustomTreeLib.DataStructures
{
    /// <summary/>
    public enum TreeTileType
    {
        /// <summary>
        /// Unspecified tree tile
        /// </summary>
        None,

        /// <summary>
        /// Straight tree tile
        /// </summary>
        Normal,

        /// <summary>
        /// Tile with less bark on <see cref="TreeTileInfo.Side"/> (Left/Right)
        /// </summary>
        LessBark,

        /// <summary>
        /// Tile with more bark on <see cref="TreeTileInfo.Side"/> (Left/Right)
        /// </summary>
        MoreBark,

        /// <summary>
        /// Tile with branches (All <see cref="TreeTileSide"/>s)
        /// </summary>
        WithBranches,

        /// <summary>
        /// Left or Right branch
        /// </summary>
        Branch,

        /// <summary>
        /// Left or Right branch with leaves
        /// </summary>
        LeafyBranch,

        /// <summary>
        /// Bottom tile with roots (All <see cref="TreeTileSide"/>s)
        /// </summary>
        WithRoots,

        /// <summary>
        /// Left or Right root
        /// </summary>
        Root,

        /// <summary>
        /// Cutted off top tile
        /// </summary>
        Top,

        /// <summary>
        /// Cutted off top with branches
        /// </summary>
        TopWithBranches,

        /// <summary>
        /// Cutted off top with roots
        /// </summary>
        TopWithRoots,

        /// <summary>
        /// Broken tree top
        /// </summary>
        BrokenTop,

        /// <summary>
        /// Tree top with leaves
        /// </summary>
        LeafyTop
    }

}
