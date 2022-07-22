using Celeste.Mod.CelesteNet;
using Celeste.Mod.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Shared {
    public struct GlobalAreaKey {

        private readonly string _sid;
        private readonly AreaKey? _localKey;
        private readonly AreaData _areaData;
        private string _modVersion; // TODO - enforce version match

		public AreaKey? Local { get { return _localKey; } }
        public AreaKey Local_Safe { get { return _localKey ?? VanillaPrologue.Local.Value; } }
        public AreaData Data { get { return _areaData; } }
        public string SID { get { return _sid; } }
        public AreaMode Mode { get { return ExistsLocal ? Local.Value.Mode : AreaMode.Normal; } }
        public MapMeta ModeMeta { get { return !ExistsLocal ? null : _areaData.GetModeMeta(Mode); } }
        public MapMetaModeProperties ModeMetaProperties { get { return !ExistsLocal ? null : _areaData.GetModeMeta(Mode).Modes[(int)Mode]; } }

        public bool ExistsLocal { get { return _localKey != null; } }
        public bool IsOverworld { get { return _localKey == null && _sid == "Overworld"; } }

        public string DisplayName {
            get {
                if (IsOverworld) {
                    return Dialog.Get("HEAD2HEAD_OVERWORLD");
                }
                else if (ExistsLocal) {
                    return Dialog.Get(Data.Name) + GetTranslatedSide(_localKey?.Mode);
                }
                else return "<Map Not Installed>";
            }
        }

		public static GlobalAreaKey Overworld {
            get {
                return new GlobalAreaKey("Overworld");
            }
        }

        public static GlobalAreaKey VanillaPrologue {
            get {
                return new GlobalAreaKey(AreaData.Get(0).ToKey());
            }
        }

		public bool IsVanilla { get { return ExistsLocal && Local?.LevelSet == "Celeste"; } }

		private static string GetTranslatedSide(AreaMode? mode) {
			switch (mode) {
                case null:
                case AreaMode.Normal:
                    return "";
                case AreaMode.BSide:
                    return " (" + Dialog.Get("OVERWORLD_REMIX") + ")";
                case AreaMode.CSide:
                    return " (" + Dialog.Get("OVERWORLD_REMIX2") + ")";
                default:
                    return " (" + mode.ToString() + ")";  // TODO AltSidesHelper support
            }
		}

        public GlobalAreaKey(string SID, AreaMode mode = AreaMode.Normal) {
            _sid = SID;
            _localKey = null;
            _areaData = null;
            _modVersion = "";
            if (SID != "Overworld") {
                foreach (AreaData d in AreaData.Areas) {
                    if (d.GetSID() == SID) {
                        _areaData = d;
                        _localKey = new AreaKey(d.ID, mode);
                    }
                }
            }
        }
        public GlobalAreaKey(AreaKey localKey) {
            _localKey = localKey;
            _sid = localKey.SID;
            _areaData = AreaData.Areas[localKey.ID];
            _modVersion = "";
        }

		public GlobalAreaKey(int localID, AreaMode mode = AreaMode.Normal) : this() {
            _areaData = AreaData.Areas[localID];
            _localKey = _areaData.ToKey(mode);
            _sid = _areaData.SID;
            _modVersion = "";
        }

		public override bool Equals(object obj) {
            if (obj is GlobalAreaKey k) {
                return k.SID == SID && k.Mode == Mode;
            }
            return false;
        }

        public override int GetHashCode() {
            return (SID + Mode.ToString()).GetHashCode();
        }
	}

    public static class GlobalAreaKeyRelatedExtensions {

        public static GlobalAreaKey ReadAreaKey(this CelesteNetBinaryReader reader) {
            string sid = reader.ReadString();
            AreaMode mode = (AreaMode)Enum.Parse(typeof(AreaMode), reader.ReadString());
            return new GlobalAreaKey(sid, mode);
        }

        public static void Write(this CelesteNetBinaryWriter writer, GlobalAreaKey area) {
            writer.Write(area.SID);
            writer.Write(area.Mode.ToString());
        }
    }

}
