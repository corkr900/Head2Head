using Celeste.Mod.Head2Head.IO;
using Celeste.Mod.CelesteNet;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Reflection.Metadata;

namespace Celeste.Mod.Head2Head.Shared {
	[Serializable]
	public struct PlayerID {
		public static PlayerID? MyID {
			get {
				PlayerID? ret;
				try {
					CNetComm comm = CNetComm.Instance;
					ret = !comm.IsConnected ? null
						: (PlayerID?)new PlayerID(LocalMACAddressHash, comm.CnetID.Value, comm.CnetClient.PlayerInfo.Name);
				}
				catch (Exception) {
					// If we lose connection at exactly the wrong moment,
					// comm.IsConnected returns true but comm.CnetID could be null 
					ret =  null;
				}
				if (ret != null) {
					if (!string.IsNullOrEmpty(ret?.Name)) LastKnownName = ret?.Name;
					else if (!string.IsNullOrEmpty(LastKnownName)) {
						PlayerID tmp = ret.Value;
						tmp.Name = LastKnownName;
						ret = tmp;
					}
				}
				return ret;
			}
		}
		public static PlayerID MyIDSafe { get { return MyID ?? Default; } }
		public static int LocalMACAddressHash {
			get {
				if (_localMACHash == null) SearchMACAddress();
				return _localMACHash ?? 0;
			}
		}
		private static int? _localMACHash;

		private static void SearchMACAddress() {
			try {
				_localMACHash = (
					from nic in NetworkInterface.GetAllNetworkInterfaces()
					where nic.OperationalStatus == OperationalStatus.Up
					select nic.GetPhysicalAddress().ToString()
				)?.FirstOrDefault()?.GetHashCode();
			}
			catch (Exception e) {
				Logger.Log(LogLevel.Error, "Head2Head", "Could not get MAC address: " + e.Message);
			}
		}
		public static PlayerID Default {
			get {
				return new PlayerID(null, uint.MaxValue, "");
			}
		}
		public static string LastKnownName { get; private set; }

		public PlayerID(int? addrHash, uint cnetID, string name) {
			MacAddressHash = addrHash;
			CNetID = cnetID;
			Name = name;
		}
		public PlayerID(PlayerID orig) {
			MacAddressHash = orig.MacAddressHash;
			CNetID = orig.CNetID;
			Name = orig.Name;
		}

		[JsonIgnore]
		public int? MacAddressHash {
			get => _macAddressHash;
			private set { _macAddressHash = value; }
		}
		[NonSerialized]
		private static int? _macAddressHash = null;

		[JsonIgnore]
		public string Name {
			get => _name;
			private set { _name = value; }
		}
		[NonSerialized]
		private static string _name = "";

		[JsonIgnore]
		public uint CNetID {
			get => _cnetId;
			private set { _cnetId = value; }
		}
		[NonSerialized]
		private static uint _cnetId = uint.MaxValue;

		[JsonIgnore]
		public bool IsDefault => MacAddressHash == null && string.IsNullOrEmpty(Name);

		[JsonInclude]
		public string DisplayName {
			get => Name;
			set { Name = value; }
		}

		[JsonInclude]
		public string SerializedID {
			get => $"{MacAddressHash ?? -1}^{CNetID}^{Name}";
			set {
				if (TryDeserializeID(value, out int? addrHash, out string name, out uint cnetID)) {
					MacAddressHash = addrHash;
					Name = name;
					CNetID = cnetID;
				}
			}
		}

		public static PlayerID FromSerialized(string serialized) {
			if (TryDeserializeID(serialized, out int? addrHash, out string name, out uint cnetID)) return new(addrHash, cnetID, name); 
			return Default;
			
		}

		private static bool TryDeserializeID(string serID, out int? addrHash, out string name, out uint cnetID) {
			addrHash = null;
			cnetID = uint.MaxValue;
			name = "";
			if (string.IsNullOrEmpty(serID)) return false;
			string[] split = serID.Split('^');
			if (split.Length != 3) return false;
			addrHash = int.TryParse(split[0], out int _addrHash) ? _addrHash : null;
			cnetID = uint.TryParse(split[1], out cnetID) ? cnetID : uint.MaxValue;
			name = split[2];
			return true;
		}

		public bool MatchAndUpdate(PlayerID id) {
			if (this.Equals(id)) {
				CNetID = id.CNetID;
				return true;
			}
			return false;
		}

		public override bool Equals(object obj) {
			return obj != null && obj is PlayerID id && id.MacAddressHash == MacAddressHash && id.Name == Name;
		}

		public override int GetHashCode() {
			return ((MacAddressHash ?? 0) + Name).GetHashCode();
		}
	}

	public static class PlayerIDExt {
		public static PlayerID ReadPlayerID(this CelesteNetBinaryReader r) {
			bool hasmac = r.ReadBoolean();
			int? mac = hasmac ? r.ReadInt32() : null;
			string name = r.ReadString();
			uint cnetid = r.ReadUInt32();
			return new PlayerID(mac, cnetid, name);
		}
		public static void Write(this CelesteNetBinaryWriter w, PlayerID id) {
			if (id.MacAddressHash == null) {
				w.Write(false);
			}
			else {
				w.Write(true);
				w.Write(id.MacAddressHash ?? 0);
			}
			w.Write(id.Name);
			w.Write(id.CNetID);
		}
	}
}
