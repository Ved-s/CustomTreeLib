using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CustomTreeLib
{
    internal class TreeCommand : ModCommand
    {
        public override string Command => "ctl";
        public override CommandType Type => CommandType.World;

        public override void Action(CommandCaller caller, string input, string[] argArray)
        {
            if (argArray.Length == 2 && argArray[0] == "debug")
            {
                CustomTreeLib.ForceDebug = argArray[1] == "on";
                if (CustomTreeLib.ForceDebug)
                    caller.Reply("Forced debug mode");
                return;
            }

            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                caller.Reply("Custom Tree Lib command cannot be used in multiplayer");
                return;
            }

            if (!CustomTreeLib.DebugMode)
            {
                caller.Reply("Custom Tree Lib command is disabled outside of debug mode");
                return;
            }

            List<string> args = new(argArray);

            if (args.Count == 0)
            {
                caller.Reply("Custom Tree Lib debug command\n" +
                    "Subcommands:\n" +
                    "    list - List loaded trees\n" +
                    "    clear <trees> - Clear trees from the world\n" +
                    "    gen <trees> - Generate trees in the world\n" +
                    "    regen <trees> - Regenerate trees in the world (same as clear then gen)\n" +
                    "    count <tree> - Count individual trees in the world\n" +
                    "");
                return;
            }
            string sub = args[0].ToLower();
            args.RemoveAt(0);

            if ("list".StartsWith(sub))
                caller.Reply($"Loaded trees: {string.Join(", ", CustomTree.LoadedTrees.Select(t => t.Name))}");

            else if ("clear".StartsWith(sub))
                SubcommandClear(caller, args);

            else if ("gen".StartsWith(sub))
                SubcommandGenerate(caller, args);

            else if ("regen".StartsWith(sub))
                SubcommandRegenerate(caller, args);

            else if ("count".StartsWith(sub))
                SubcommandCount(caller, args);

            else
                caller.Reply($"Unknown subcommand: {sub}");
        }

        static void SubcommandClear(CommandCaller caller, List<string> args)
        {
            if (args.Count == 0)
            {
                caller.Reply($"Provide tree types to clear (ctl list)");
                return;
            }
            CustomTree[] trees = args
                .Select(arg => arg.ToLower())
                .Select(arg => CustomTree.LoadedTrees.FirstOrDefault(t => t.Name.ToLower().StartsWith(arg)))
                .Where(tree => tree is not null)
                .ToArray();

            if (trees.Length == 0)
            {
                caller.Reply($"No matching trees found");
                return;
            }
            caller.Reply($"Clearing {trees.Length} tree(s) from this world");
            TreeWorldGen.Clear(trees);
        }

        static void SubcommandGenerate(CommandCaller caller, List<string> args)
        {
            if (args.Count == 0)
            {
                caller.Reply($"Provide tree types to generate (ctl list)");
                return;
            }
            CustomTree[] trees = args
                .Select(arg => arg.ToLower())
                .Select(arg => CustomTree.LoadedTrees.FirstOrDefault(t => t.Name.ToLower().StartsWith(arg)))
                .Where(tree => tree is not null)
                .ToArray();

            if (trees.Length == 0)
            {
                caller.Reply($"No matching trees found");
                return;
            }
            caller.Reply($"Generating {trees.Length} tree(s)");
            TreeWorldGen.Generate(trees);
        }

        static void SubcommandRegenerate(CommandCaller caller, List<string> args)
        {
            if (args.Count == 0)
            {
                caller.Reply($"Provide tree types to regenerate (ctl list)");
                return;
            }
            CustomTree[] trees = args
                .Select(arg => arg.ToLower())
                .Select(arg => CustomTree.LoadedTrees.FirstOrDefault(t => t.Name.ToLower().StartsWith(arg)))
                .Where(tree => tree is not null)
                .ToArray();

            if (trees.Length == 0)
            {
                caller.Reply($"No matching trees found");
                return;
            }
            caller.Reply($"Regenerating {trees.Length} tree(s)");
            TreeWorldGen.Clear(trees);
            TreeWorldGen.Generate(trees);
        }

        static void SubcommandCount(CommandCaller caller, List<string> args)
        {
            if (args.Count == 0)
            {
                caller.Reply($"Provide tree type to count (ctl list)");
                return;
            }
            string arg = args[0].ToLower();
            CustomTree tree = CustomTree.LoadedTrees.FirstOrDefault(t => t.Name.ToLower().StartsWith(arg));
        
            if (tree is null)
            {
                caller.Reply($"No matching tree found");
                return;
            }

            HashSet<Point> treePositions = new();
            int found = 0;

            for (int x = 0; x < Main.maxTilesX; x++)
                for (int y = 0; y < Main.maxTilesY; y++)
                {
                    if (treePositions.Contains(new(x, y)))
                        continue;

                    Tile tile = Main.tile[x, y];
                    if (!tile.HasTile || tile.TileType != tree.Tile.Type)
                        continue;
                    found++;
                    foreach (var treeTile in TreeGrowing.EnumerateTreeTiles(x, y))
                        treePositions.Add(treeTile.Pos);
                }
            caller.Reply($"Found {found} {tree.Name} trees in the world");
        }
    }
}
