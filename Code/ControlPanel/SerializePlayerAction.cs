using Celeste.Mod.Head2Head.Shared;
using System.Collections.Generic;

namespace Celeste.Mod.Head2Head.ControlPanel
{
    public struct SerializePlayerAction
    {
        public PlayerID Id { get; set; }
        public List<string> Commands { get; set; }
    }
}
