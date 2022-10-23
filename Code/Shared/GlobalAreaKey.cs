using Celeste.Mod.CelesteNet;
using Celeste.Mod.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Shared {
    public struct GlobalAreaKey {
        // TODO include display name when sending over network

        private readonly string _sid;
        private readonly AreaKey? _localKey;
        private readonly AreaData _areaData;
        private string _versionString;  // lazily populated
        private ModContent _modContent;  // lazily populated

        public AreaKey? Local { get { return _localKey; } }
        public AreaKey Local_Safe { get { return _localKey ?? VanillaPrologue.Local.Value; } }
        public AreaData Data { get { return _areaData; } }
        public string SID { get { return _sid; } }
        public Version LocalVersion {
			get {
                if (IsOverworld || !ExistsLocal || IsVanilla) return Celeste.Instance.Version;
                else return ModContent.Mod.Version;
            }
		}
        public string VersionString { 
            get {
                if (string.IsNullOrEmpty(_versionString)) _versionString = LocalVersion.ToString();
                return _versionString;
            }
        }
        public Version Version { get { return new Version(VersionString); } }
        public AreaMode Mode { get { return ExistsLocal ? Local.Value.Mode : AreaMode.Normal; } }
        public MapMeta ModeMeta { get { return !ExistsLocal ? null : _areaData.GetModeMeta(Mode); } }
        public MapMetaModeProperties ModeMetaProperties { get { return !ExistsLocal ? null : _areaData.GetModeMeta(Mode)?.Modes[(int)Mode]; } }
        public ModContent ModContent { 
            get {
                if (_localKey == null) return null;
                if (_modContent == null) _modContent = Util.GetModContent(this);
                return _modContent;
			}
        }
        public bool VersionMatchesLocal { get { return Version == LocalVersion; } }
        public bool ExistsLocal { get { return _localKey != null; } }
        public bool IsOverworld { get { return _localKey == null && _sid == "Overworld"; } }
        public bool IsVanilla { get { return ExistsLocal && Local?.LevelSet == "Celeste"; } }
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
        public static GlobalAreaKey Head2HeadLobby {
            get {
                return new GlobalAreaKey("Head2Head/00_Head2HeadLobby");
            }
        }

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
                    return " (" + mode.ToString() + ")";
            }
		}

        public GlobalAreaKey(string SID, AreaMode mode = AreaMode.Normal, string version = null) {
            _sid = SID;
            _localKey = null;
			_areaData = null;
            _modContent = null;
            _versionString = version;
            if (SID != "Overworld" && AreaData.Areas != null) {
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
            _modContent = null;
            _versionString = null;
        }
		public GlobalAreaKey(int localID, AreaMode mode = AreaMode.Normal) : this() {
            _areaData = AreaData.Areas[localID];
            _localKey = _areaData.ToKey(mode);
            _sid = _areaData.SID;
            _modContent = null;
            _versionString = null;
        }

		public override bool Equals(object obj) {
            if (obj is GlobalAreaKey k) {
                return k.SID == SID && k.Mode == Mode && k.VersionString == VersionString;
            }
            return false;
        }

        public override int GetHashCode() {
            return (SID + Mode.ToString() + VersionString).GetHashCode();
        }
	}

    public static class GlobalAreaKeyRelatedExtensions {

        public static GlobalAreaKey ReadAreaKey(this CelesteNetBinaryReader reader) {
            string sid = reader.ReadString();
            AreaMode mode = (AreaMode)Enum.Parse(typeof(AreaMode), reader.ReadString());
            string version = reader.ReadString();
            return new GlobalAreaKey(sid, mode, version);
        }

        public static void Write(this CelesteNetBinaryWriter writer, GlobalAreaKey area) {
            writer.Write(area.SID);
            writer.Write(area.Mode.ToString());
            writer.Write(area.VersionString);
        }
    }

}
