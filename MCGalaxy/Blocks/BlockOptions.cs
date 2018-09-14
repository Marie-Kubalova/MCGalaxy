﻿/*
    Copyright 2015 MCGalaxy team
    
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
using System.Collections.Generic;
using MCGalaxy.Commands;
using MCGalaxy.Generator.Foliage;
using BlockID = System.UInt16;

namespace MCGalaxy.Blocks {
    
    public sealed class BlockOption {
        public string Name, Help;
        public BlockOptions.OptionSetter SetFunc;
        
        public BlockOption(string name, BlockOptions.OptionSetter func, string help) {
            Name = name; SetFunc = func; Help = help;
        }
    }
    
    public static class BlockOptions {        
        public delegate void OptionSetter(Player p, BlockProps[] scope, BlockID b, string value);
        public const string Portal = "Portal", MB = "MessageBlock", Rails = "Rails", WaterKills = "WaterKills"; 
        public const string LavaKills = "LavaKills", Door = "Door", tDoor = "tDoor", Killer = "Killer";
        public const string DeathMsg = "DeathMessage", AI = "AnimalAI", StackId = "StackBlock", OPBlock = "OPBlock";
        public const string oDoor = "oDoor", Drownable = "Drownable", Grass = "Grass", Dirt = "Dirt";
        
        public static List<BlockOption> Options = new List<BlockOption>() {
            new BlockOption(Portal,     SetPortal, "%HToggles whether the block is a %T/Portal"),
            new BlockOption(MB,         SetMB,     "%HToggles whether the block is a %T/MessageBlock"),
            new BlockOption(Rails,      SetRails,  "%HToggles whether %Strain %Hblocks can run over this block"),
            new BlockOption(WaterKills, SetWater,  "%HToggles whether flooding water kills this block"),
            new BlockOption(LavaKills,  SetLava,   "%HToggles whether flooding lava kills this block"),
            new BlockOption(Door,       SetDoor,   "%HToggles whether this block is a Door block"),
            new BlockOption(tDoor,      SetTDoor,  "%HToggles whether this block is a tDoor block"),
            new BlockOption(Killer,     SetKiller, "%HToggles whether colliding with this block kills player"),
            new BlockOption(DeathMsg, SetDeathMsg, "%HSets or removes the death message for this block " +
                            "(Note: %S@p %His a placeholder for name of player who dies"),
            new BlockOption(AI,         SetAI,     "%HSets the flying or swimming animal AI for this block. " +
                            "Types: &f" + Enum.GetNames(typeof(AnimalAI)).Join()),
            new BlockOption(StackId,    SetStackId,"%HSets the block this block is converted into, when placed on top " +
                            "of another of the same block. (e.g. placing two slabs on each other becomes a double slab)"),
            new BlockOption(OPBlock,    SetOPBlock,"%HMarks the block as being on OP block. OP blocks can't be " +
                            "blown up by explosions, and can't be replaced in games when build type is ModifyOnly."),
            new BlockOption(Drownable,  SetDrown,  "%HSets whether this block can drown players " +
                            "(Note %T/Map death %Hmust be enabled for players to drown)"),         
            new BlockOption(Grass,      SetGrass,  "%HSets the block that this block is changed into, " +
                            "when exposed to sunlight (leave block blank to remove)"),
            new BlockOption(Dirt,       SetDirt,   "%HSets the block that this block is changed into, " +
                            "when no longer exposed to sunlight (leave block blank to remove)"),
        };       
        
        
        public static BlockOption Find(string opt) {
            if (opt.CaselessEq("mb"))       opt = MB;
            if (opt.CaselessEq("death"))    opt = Killer;
            if (opt.CaselessEq("deathmsg")) opt = DeathMsg;
            if (opt.CaselessEq("animal"))   opt = AI;
            if (opt.CaselessEq("stackid"))  opt = StackId;
            if (opt.CaselessEq("drown"))    opt = Drownable;
            
            foreach (BlockOption option in Options) {
                if (option.Name.CaselessEq(opt)) return option;
            }
            return null;
        }
        
        internal static void ApplyChanges(BlockProps[] scope, Level lvl, BlockID block) {
            if (scope == Block.Props) {
                scope[block].ChangedScope |= 1;
                BlockProps.Save("default",      scope, Block.PropsLock, 1);
                Block.ChangeGlobalProps(block, scope[block]);
            } else {
                scope[block].ChangedScope |= 2;
                BlockProps.Save("_" + lvl.name, scope, lvl.PropsLock,   2);
                lvl.UpdateBlockHandler(block);
            }
        }
        
        internal static string BlockName(BlockProps[] scope, Player p, BlockID block) {
            return scope == Block.Props ? Block.GetName(Player.Console, block) : Block.GetName(p, block);
        }
        
        
        static void SetPortal(Player p, BlockProps[] s, BlockID b, string v) { Toggle(p,s,b, "a portal",        ref s[b].IsPortal); }
        static void SetMB(Player p,     BlockProps[] s, BlockID b, string v) { Toggle(p,s,b, "a message block", ref s[b].IsMessageBlock); }
        static void SetRails(Player p,  BlockProps[] s, BlockID b, string v) { Toggle(p,s,b, "train rails",     ref s[b].IsRails); }
        static void SetWater(Player p,  BlockProps[] s, BlockID b, string v) { Toggle(p,s,b, "killed by water", ref s[b].WaterKills); }
        static void SetLava(Player p,   BlockProps[] s, BlockID b, string v) { Toggle(p,s,b, "killed by lava",  ref s[b].LavaKills); }
        static void SetDoor(Player p,   BlockProps[] s, BlockID b, string v) { Toggle(p,s,b, "a door",          ref s[b].IsDoor); }
        static void SetTDoor(Player p,  BlockProps[] s, BlockID b, string v) { Toggle(p,s,b, "a tDoor",         ref s[b].IsTDoor); }
        static void SetKiller(Player p, BlockProps[] s, BlockID b, string v) { Toggle(p,s,b, "a killer block",  ref s[b].KillerBlock); }       
        static void SetOPBlock(Player p,BlockProps[] s, BlockID b, string v) { Toggle(p,s,b, "an OP block",     ref s[b].OPBlock); }
        static void SetDrown(Player p,  BlockProps[] s, BlockID b, string v) { Toggle(p,s,b, "drowns players",  ref s[b].Drownable); }      
        static void SetGrass(Player p,  BlockProps[] s, BlockID b, string v) { SetBlock(p,s,b,v, "Grass form",  ref s[b].GrassBlock); }
        static void SetDirt(Player p,   BlockProps[] s, BlockID b, string v) { SetBlock(p,s,b,v, "Dirt form",   ref s[b].DirtBlock); }
        
        static void Toggle(Player p, BlockProps[] scope, BlockID block, string type, ref bool on) {
            on = !on;           
            string blockName = BlockName(scope, p, block);            
            p.Message("Block {0} is {1}: {2}", blockName, type, on ? "&aYes" : "&cNo");
        }
        
        static void SetAI(Player p, BlockProps[] scope, BlockID block, string msg) {
            AnimalAI ai = AnimalAI.None;
            if (!CommandParser.GetEnum(p, msg, "Animal AI", ref ai)) return;
            scope[block].AnimalAI = ai;
            
            string blockName = BlockName(scope, p, block);
            p.Message("Animal AI for {0} set to: {1}", blockName, ai);
        }
        
        static void SetDeathMsg(Player p, BlockProps[] scope, BlockID block, string msg) {
            scope[block].DeathMessage = msg;
            
            string blockName = BlockName(scope, p, block);
            if (msg == null) {
                p.Message("Death message for {0} removed.", blockName);
            } else {
                p.Message("Death message for {0} set to: {1}", blockName, msg);
            }
        }
        
        static void SetStackId(Player p, BlockProps[] scope, BlockID block, string msg) {
            BlockID stackBlock;
            if (msg == null) {
                stackBlock = Block.Air;
            } else {
                if (!CommandParser.GetBlock(p, msg, out stackBlock)) return;
            }
            scope[block].StackBlock = stackBlock;
            
            string blockName = BlockName(scope, p, block);
            if (stackBlock == Block.Air) {
                p.Message("Removed stack block for {0}", blockName);
            } else {
                p.Message("Stack block for {0} set to: {1}",
                          blockName, BlockName(scope, p, stackBlock));
            }
        }
        
        static void SetBlock(Player p, BlockProps[] scope, BlockID block,
                             string msg, string type, ref BlockID target) {
            string blockName = BlockName(scope, p, block);
            if (msg == null) {
                target = Block.Invalid;
                p.Message("{1} for {0} removed.", blockName, type);
            } else {
                BlockID other;
                if (!CommandParser.GetBlock(p, msg, out other)) return;
                if (other == block) { p.Message("ID of {0} must be different.", type); return; }
                
                target = other;
                p.Message("{2} for {0} set to: {1}", blockName, BlockName(scope, p, other), type);
            }
        }
    }
}
