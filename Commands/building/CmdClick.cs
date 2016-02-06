/*
    Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl/MCGalaxy)
    
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
namespace MCGalaxy.Commands {
    
    public sealed class CmdClick : Command {
        
        public override string name { get { return "click"; } }
        public override string shortcut { get { return "x"; } }
        public override string type { get { return CommandTypes.Building; } }
        public override bool museumUsable { get { return false; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Guest; } }
        public CmdClick() { }

        public override void Use(Player p, string message) {
            if (p == null) { MessageInGameOnly(p); return; }
            ushort[] click = p.lastClick;

            if (message.IndexOf(' ') != -1) {
                string[] args = message.ToLower().Split(' ');
                if (args.Length != 3) { Help(p); return; }
                
                for (int i = 0; i < 3; i++) {
                    if (args[i] == "x" || args[i] == "y" || args[i] == "z") {
                        click[i] = p.lastClick[i];
                    } else if (IsValid(p, i, args[i])) {
                        click[i] = ushort.Parse(args[i]);
                    } else {
                        Player.SendMessage(p, "\"" + args[i] + "\" was not valid");  return;
                    }
                }
            }

            p.lastCMD = "click";
            p.ManualChange(click[0], click[1], click[2], 0, Block.rock);
            Player.SendMessage(p, "Clicked &b(" + click[0] + ", " + click[1] + ", " + click[2] + ")");
        }

        bool IsValid(Player p, int axis, string message) {
            ushort value;
            if (!ushort.TryParse(message, out value)) return false;

            if (value >= p.level.Width && axis == 0) return false;
            else if (value >= p.level.Height && axis == 1) return false;
            else if (value >= p.level.Length && axis == 2) return false;
            return true;
        }
        
        public override void Help(Player p) {
            Player.SendMessage(p, "/click [x y z] - Fakes a click");
            Player.SendMessage(p, "If no xyz is given, it uses the last place clicked");
            Player.SendMessage(p, "/click 200 y 200 will cause it to click at 200x, last y and 200z");
        }
    }
}
