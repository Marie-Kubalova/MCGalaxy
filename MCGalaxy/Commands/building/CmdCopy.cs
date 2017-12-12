/*
    Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl/MCGalaxy)
    
    Dual-licensed under the    Educational Community License, Version 2.0 and
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
using System.IO;
using System.IO.Compression;
using MCGalaxy.DB;
using MCGalaxy.Drawing;
using MCGalaxy.Drawing.Brushes;
using MCGalaxy.Drawing.Ops;
using MCGalaxy.Maths;

namespace MCGalaxy.Commands.Building {
    public sealed class CmdCopy : Command {
        public override string name { get { return "Copy"; } }
        public override string shortcut { get { return "c"; } }
        public override string type { get { return CommandTypes.Building; } }
        public override LevelPermission defaultRank { get { return LevelPermission.AdvBuilder; } }
        public override bool SuperUseable { get { return false; } }
        public override CommandAlias[] Aliases {
            get { return new CommandAlias[] { new CommandAlias("Cut", "cut") }; }
        }

        public override void Use(Player p, string message) {
            int offsetIndex = message.IndexOf('@');
            if (offsetIndex != -1)
                message = message.Replace("@ ", "").Replace("@", "");
            
            string[] parts = message.SplitSpaces();
            string opt = parts[0].ToLower();
            
            if (opt == "save") {
                if (parts.Length != 2) { Help(p); return; }
                if (!Formatter.ValidName(p, parts[1], "saved copy")) return;
                SaveCopy(p, parts[1]);
            } else if (opt == "load") {
                if (parts.Length != 2) { Help(p); return; }
                if (!Formatter.ValidName(p, parts[1], "saved copy")) return;
                LoadCopy(p, parts[1]);
            } else if (opt == "delete") {
                if (parts.Length != 2) { Help(p); return; }
                if (!Formatter.ValidName(p, parts[1], "saved copy")) return;
                
                string path = FindCopy(p.name, parts[1]);
                if (path == null) { Player.Message(p, "No such copy exists."); return; }
                File.Delete(path);
                Player.Message(p, "Deleted copy " + parts[1]);
            } else if (opt == "list") {
                string dir = "extra/savecopy/" + p.name;
                if (!Directory.Exists(dir)) {
                    Player.Message(p, "No such directory exists"); return;
                }
                
                string[] files = Directory.GetFiles(dir);
                for (int i = 0; i < files.Length; i++) {
                    Player.Message(p, Path.GetFileNameWithoutExtension(files[i]));
                }
            } else {
                HandleOther(p, parts, offsetIndex);
            }
        }
        
        void HandleOther(Player p, string[] parts, int offsetIndex) {
            CopyArgs cArgs = new CopyArgs();
            cArgs.offsetIndex = offsetIndex;
            
            for (int i = 0; i < parts.Length; i++) {
                string opt = parts[i];
                if (opt.CaselessEq("cut")) {
                    cArgs.cut = true;
                } else if (opt.CaselessEq("air")) {
                    cArgs.air = true;
                } else if (opt.Length > 0) {
                    Help(p);
                }
            }

            Player.Message(p, "Place or break two blocks to determine the edges.");
            int marks = cArgs.offsetIndex != -1 ? 3 : 2;
            p.MakeSelection(marks, "Selecting region for %SCopy", cArgs, DoCopy, DoCopyMark);
        }

        void DoCopyMark(Player p, Vec3S32[] m, int i, object state, ExtBlock block) {
            if (i == 2) {
                CopyState copy = p.CopySlots[p.CurrentCopySlot];
                copy.Offset.X = copy.OriginX - m[i].X;
                copy.Offset.Y = copy.OriginY - m[i].Y;
                copy.Offset.Z = copy.OriginZ - m[i].Z;
                Player.Message(p, "Set offset of where to paste from.");
                return;
            }
            if (i != 1) return;
            
            CopyArgs cArgs = (CopyArgs)state;
            Vec3S32 min = Vec3S32.Min(m[0], m[1]), max = Vec3S32.Max(m[0], m[1]);
            ushort minX = (ushort)min.X, minY = (ushort)min.Y, minZ = (ushort)min.Z;
            ushort maxX = (ushort)max.X, maxY = (ushort)max.Y, maxZ = (ushort)max.Z;
            
            CopyState cState = new CopyState(minX, minY, minZ, maxX - minX + 1,
                                             maxY - minY + 1, maxZ - minZ + 1);
            cState.OriginX = m[0].X; cState.OriginY = m[0].Y; cState.OriginZ = m[0].Z;
            
            int index = 0; cState.UsedBlocks = 0;
            cState.PasteAir = cArgs.air;
            
            for (ushort y = minY; y <= maxY; ++y)
                for (ushort z = minZ; z <= maxZ; ++z)
                    for (ushort x = minX; x <= maxX; ++x)
            {
                block = p.level.GetBlock(x, y, z);
                if (!p.group.Blocks[block.BlockID]) { index++; continue; } // TODO: will need to fix this when extblock permissions
                
                if (block.BlockID != Block.Air || cState.PasteAir)
                    cState.UsedBlocks++;
                cState.Set(block, index);
                index++;
            }
            
            if (cState.UsedBlocks > p.group.DrawLimit) {
                Player.Message(p, "You tried to copy {0} blocks. You cannot copy more than {1} blocks.",
                               cState.UsedBlocks, p.group.DrawLimit);
                cState.Clear(); cState = null;
                p.ClearSelection();
                return;
            }
            
            cState.CopySource = "level " + p.level.name;
            p.SetCurrentCopy(cState);
            if (cArgs.cut) {
                DrawOp op = new CuboidDrawOp();
                op.Flags = BlockDBFlags.Cut;
                Brush brush = new SolidBrush(ExtBlock.Air);
                DrawOpPerformer.Do(op, brush, p, new Vec3S32[] { min, max }, false);
            }

            Player.Message(p, "Copied &a{0} %Sblocks, origin at ({1}, {2}, {3}) corner", cState.UsedBlocks,
                           cState.OriginX == cState.X ? "Min" : "Max",
                           cState.OriginY == cState.Y ? "Min" : "Max",
                           cState.OriginY == cState.Y ? "Min" : "Max");
            if (!cState.PasteAir) {
                Player.Message(p, "To also copy air blocks, use %T/Copy Air");
            }
            if (cArgs.offsetIndex != -1) {
                Player.Message(p, "Place a block to determine where to paste from");
            }
        }
        
        bool DoCopy(Player p, Vec3S32[] m, object state, ExtBlock block) { return false; }
        class CopyArgs { public int offsetIndex; public bool cut, air; }
        
        void SaveCopy(Player p, string file) {
            if (!Directory.Exists("extra/savecopy"))
                Directory.CreateDirectory("extra/savecopy");
            if (!Directory.Exists("extra/savecopy/" + p.name))
                Directory.CreateDirectory("extra/savecopy/" + p.name);
            if (Directory.GetFiles("extra/savecopy/" + p.name).Length > 15) {
                Player.Message(p, "You can only save a maxmium of 15 copies. /copy delete some.");
                return;
            }
            
            string path = "extra/savecopy/" + p.name + "/" + file + ".cpb";
            using (FileStream fs = File.Create(path))
                using(GZipStream gs = new GZipStream(fs, CompressionMode.Compress))
            {
                p.CopySlots[p.CurrentCopySlot].SaveTo(gs);
            }
            Player.Message(p, "Saved copy as " + file);
        }

        void LoadCopy(Player p, string file) {
            string path = FindCopy(p.name, file);
            if (path == null) { Player.Message(p, "No such copy exists"); return; }
            file = Path.GetFileNameWithoutExtension(path);
            
            using (FileStream fs = File.OpenRead(path))
                using (GZipStream gs = new GZipStream(fs, CompressionMode.Decompress))
            {
                CopyState state = new CopyState(0, 0, 0, 0, 0, 0);
                if (path.CaselessEnds(".cpb")) {
                    state.LoadFrom(gs);
                } else {
                    state.LoadFromOld(gs, fs);
                }
                state.CopySource = "file " + file;
                p.SetCurrentCopy(state);
            }
            Player.Message(p, "Loaded copy from " + file);
        }
        
        static string FindCopy(string name, string file) {
            string path = "extra/savecopy/" + name + "/" + file;
            bool existsNew = File.Exists(path + ".cpb");
            bool existsOld = File.Exists(path + ".cpy");
            
            if (!existsNew && !existsOld) return null;
            string ext = existsNew ? ".cpb" : ".cpy";
            return path + ext;
        }
        
        public override void Help(Player p) {
            Player.Message(p, "%T/Copy %H- Copies the blocks in an area.");
            Player.Message(p, "%T/Copy save [name] %H- Saves what you have copied.");
            Player.Message(p, "%T/Copy load [name] %H- Loads what you have saved.");
            Player.Message(p, "%T/Copy delete [name] %H- Deletes the specified copy.");
            Player.Message(p, "%T/Copy list %H- Lists all saved copies you have");
            Player.Message(p, "%T/Copy cut %H- Copies the blocks in an area, then removes them.");
            Player.Message(p, "%T/Copy air %H- Copies the blocks in an area, including air.");
            Player.Message(p, "/Copy @ - @ toggle for all the above, gives you a third click after copying that determines where to paste from");
        }
    }
}
