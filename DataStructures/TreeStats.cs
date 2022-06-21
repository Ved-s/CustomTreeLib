using Microsoft.Xna.Framework;

namespace CustomTreeLib.DataStructures
{
    /// <summary>
    /// Tree statistics
    /// </summary>
    public struct TreeStats
    {
        /// <summary>
        /// Total tree blocks
        /// </summary>
        public int TotalBlocks;

        /// <summary>
        /// Total tree branches
        /// </summary>
        public int TotalBranches;

        /// <summary>
        /// Total leafy branches
        /// </summary>
        public int LeafyBranches;

        /// <summary>
        /// True if tree top exisis and not broken
        /// </summary>
        public bool HasTop;

        /// <summary>
        /// True if tree top exisis and broken
        /// </summary>
        public bool BrokenTop;

        /// <summary>
        /// True if thee have left root
        /// </summary>
        public bool LeftRoot;

        /// <summary>
        /// True if thee have right root
        /// </summary>
        public bool RightRoot;

        /// <summary>
        /// Tree top position
        /// </summary>
        public Point Top = new(0, int.MaxValue);

        /// <summary>
        /// Tree bottom position
        /// </summary>
        public Point Bottom;

        /// <summary>
        /// Tree ground tile type
        /// </summary>
        public ushort GroundType;

        /// <summary/>
        public override string ToString()
        {
            return $"T:{TotalBlocks} " +
                $"B:{TotalBranches} " +
                $"BL:{LeafyBranches} " +
                $"{(HasTop ? BrokenTop ? "TB " : "TL " : "")}" +
                $"{(LeftRoot ? "Rl " : "")}" +
                $"{(RightRoot ? "Rr " : "")}" +
                $"X:{Top.X} Y:{Top.Y}-{Bottom.Y} G:{GroundType}";
        }
    }
}
