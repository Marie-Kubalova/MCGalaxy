﻿/*
    Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl/MCForge)
    
    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    http://www.opensource.org/licenses/ecl2.php
    http://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using System;
using MCGalaxy.Generator.Realistic;

namespace MCGalaxy.Generator 
{
    public static class SimpleGen 
    {
        delegate byte NextBlock();
        
        public static void RegisterGenerators() {
            const GenType type = GenType.Simple;
            MapGen.Register("Flat",    type, GenFlat,  "&HSeed specifies grass height (default half of level height)");
            MapGen.Register("Pixel",   type, GenPixel, "&HSeed does nothing");
            MapGen.Register("Empty",   type, GenEmpty, "&HSeed does nothing");
            MapGen.Register("Space",   type, GenSpace,   MapGen.DEFAULT_HELP);
            MapGen.Register("Rainbow", type, GenRainbow, MapGen.DEFAULT_HELP);
        }
        
        unsafe static bool GenFlat(Player p, Level lvl, string seed) {
            int grassHeight = lvl.Height / 2, v;
            if (int.TryParse(seed, out v) && v >= 0 && v <= lvl.Height) grassHeight = v;
            lvl.Config.EdgeLevel = grassHeight;
            int grassY = grassHeight - 1;

            fixed (byte* ptr = lvl.blocks) 
            {
                if (grassY > 0)
                    MapSet(lvl.Width, lvl.Length, ptr, 0, grassY - 1,  Block.Dirt);
                if (grassY >= 0 && grassY < lvl.Height)
                    MapSet(lvl.Width, lvl.Length, ptr, grassY, grassY, Block.Grass);
            }
            return true;
        }
        
        unsafe static void MapSet(int width, int length, byte* ptr,
                                  int yBeg, int yEnd, byte block) {
            int beg = (yBeg * length) * width;
            int end = (yEnd * length + (length - 1)) * width + (width - 1);
            Utils.memset((IntPtr)ptr, block, beg, end - beg + 1);
        }
        

        static bool GenEmpty(Player p, Level lvl, string seed) {
            int maxX = lvl.Width - 1, maxZ = lvl.Length - 1;
            Cuboid(lvl, 0, 0, 0, maxX, 0, maxZ, () => Block.Bedrock);
            lvl.Config.EdgeLevel = 1;
            return true;
        }
        
        static bool GenPixel(Player p, Level lvl, string seed) {
            int maxX = lvl.Width - 1, maxY = lvl.Height - 1, maxZ = lvl.Length - 1;
            NextBlock nextBlock = () => Block.White;
            
            // Cuboid the four walls
            Cuboid(lvl, 0, 1, 0,    maxX, maxY, 0,    nextBlock);
            Cuboid(lvl, 0, 1, maxZ, maxX, maxY, maxZ, nextBlock);
            Cuboid(lvl, 0, 1, 0,    0, maxY, maxZ,    nextBlock);
            Cuboid(lvl, maxX, 1, 0, maxX, maxY, maxZ, nextBlock);
            
            // Cuboid base
            Cuboid(lvl, 0, 0, 0, maxX, 0, maxZ, () => Block.Bedrock);
            return true;
        }
        
        static bool GenSpace(Player p, Level lvl, string seed) {
            int maxX = lvl.Width - 1, maxY = lvl.Height - 1, maxZ = lvl.Length - 1;
            Random rng = MapGen.MakeRng(seed);
            NextBlock nextBlock = () => rng.Next(100) == 0 ? Block.Iron : Block.Obsidian;

            // Cuboid the four walls
            Cuboid(lvl, 0, 2, 0,    maxX, maxY, 0,    nextBlock);
            Cuboid(lvl, 0, 2, maxZ, maxX, maxY, maxZ, nextBlock);
            Cuboid(lvl, 0, 2, 0,    0, maxY, maxZ,    nextBlock);
            Cuboid(lvl, maxX, 2, 0, maxX, maxY, maxZ, nextBlock);
            
            // Cuboid base and top
            Cuboid(lvl, 0, 0, 0,    maxX, 0, maxZ, () => Block.Bedrock);
            Cuboid(lvl, 0, 1, 0,    maxX, 1, maxZ,    nextBlock);
            Cuboid(lvl, 0, maxY, 0, maxX, maxY, maxZ, nextBlock);
            
            lvl.Config.EdgeLevel    = 1;
            lvl.Config.HorizonBlock = Block.Obsidian;
            lvl.Config.SkyColor     = "#000000";
            lvl.Config.FogColor     = "#000000";
            return true;
        }
        
        static bool GenRainbow(Player p, Level lvl, string seed) {
            int maxX = lvl.Width - 1, maxY = lvl.Height - 1, maxZ = lvl.Length - 1;
            Random rng = MapGen.MakeRng(seed);
            NextBlock nextBlock = () => (byte)rng.Next(Block.Red, Block.White);

            // Cuboid the four walls
            Cuboid(lvl, 0, 1, 0,    maxX, maxY, 0,    nextBlock);
            Cuboid(lvl, 0, 1, maxZ, maxX, maxY, maxZ, nextBlock);
            Cuboid(lvl, 0, 1, 0,    0, maxY, maxZ,    nextBlock);
            Cuboid(lvl, maxX, 1, 0, maxX, maxY, maxZ, nextBlock);
            
            // Cuboid base and top
            Cuboid(lvl, 0, 0, 0,    maxX, 0, maxZ,    nextBlock);
            Cuboid(lvl, 0, maxY, 0, maxX, maxY, maxZ, nextBlock);
            return true;
        }
        
        static void Cuboid(Level lvl, int minX, int minY, int minZ,
                           int maxX, int maxY, int maxZ, NextBlock nextBlock) {
            int width = lvl.Width, length = lvl.Length;
            byte[] blocks = lvl.blocks;
            
            // space theme uses maxY = 2, but map might only be 1 block high
            maxY = Math.Min(maxY, lvl.MaxY);
            
            for (int y = minY; y <= maxY; y++)
                for (int z = minZ; z <= maxZ; z++)
                    for (int x = minX; x <= maxX; x++)
            {
                blocks[x + width * (z + y * length)] = nextBlock();
            }
        }
    }
}
