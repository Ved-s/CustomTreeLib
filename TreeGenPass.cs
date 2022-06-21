using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace CustomTreeLib
{
    /// <summary>
    /// CustomTrees worldgen pass
    /// </summary>
    public class TreeGenPass : GenPass
    {
        /// <summary>
        /// Use this to determine if CustomTree worldgen is active
        /// </summary>
        public static bool Active { get; private set; }

        /// <summary/>
        public TreeGenPass() : base("GenCustomTrees", 300)
        {
        }

        /// <summary/>
        protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
        {
            Active = true;
            progress.Message = "Planting custom trees";
            TreeWorldGen.Generate(CustomTree.LoadedTrees.ToArray(), progress.Set);
            Active = false;
        }
    }
}
