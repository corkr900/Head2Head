using Celeste.Mod.Head2Head.IO;
using Celeste.Mod.CelesteNet;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Shared {
	public struct PlayerID {
		public static PlayerID? MyID {
			get {
				PlayerID? ret;
				try {
					CNetComm comm = CNetComm.Instance;
					ret = !comm.IsConnected ? null
						: (PlayerID?)new PlayerID(LocalMACAddressHash, comm.CnetID.Value, comm.CnetClient.PlayerInfo.Name);
				}
				catch (Exception e) {
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
		private static int? _localMACHash = null;
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
		public int? MacAddressHash { get; private set; }
		public string Name { get; private set; }
		public uint CNetID { get; private set; }
		public static string LastKnownName { get; private set; }

		public bool MatchAndUpdate(PlayerID id) {
			if (this.Equals(id)) {
				CNetID = id.CNetID;
				return true;
			}
			return false;
		}

		public bool IsDefault() {
			return MacAddressHash == null && string.IsNullOrEmpty(Name);
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
			int? mac = hasmac ? (int?)r.ReadInt32() : null;
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
