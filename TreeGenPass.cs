using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace CustomTreeLib
{
    internal class TreeGenPass : GenPass
    {
        public TreeGenPass() : base("GenCustomTrees", 300)
        {
        }

        protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
        {
            progress.Message = "Planting custom trees";
            TreeWorldGen.Generate(CustomTree.LoadedTrees.ToArray(), progress.Set);
        }
    }
}
