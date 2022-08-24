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
				try {
					CNetComm comm = CNetComm.Instance;
					return !comm.IsConnected ? null
						: (PlayerID?)new PlayerID(LocalMACAddress, comm.CnetID.Value, comm.CnetClient.PlayerInfo.Name);
				}
				catch (Exception e) {
					// If we lose connection at exactly the wrong moment,
					// comm.IsConnected returns true but comm.CnetID could be null 
					return null;
				}
			}
		}
		public static PlayerID MyIDSafe { get { return MyID ?? Default; } }
		public static string LocalMACAddress {
			get {
				if (string.IsNullOrEmpty(_localMACAddress)) SearchMACAddress();
				return _localMACAddress;
			}
		}
		private static string _localMACAddress = null;
		private static void SearchMACAddress() {
			try {
				_localMACAddress = (
					from nic in NetworkInterface.GetAllNetworkInterfaces()
					where nic.OperationalStatus == OperationalStatus.Up
					select nic.GetPhysicalAddress().ToString()
				).FirstOrDefault();
			}
			catch (Exception e) {
				Engine.Commands.Log("ERROR: Could not get MAC address: " + e.Message);
			}
		}
		public static PlayerID Default {
			get {
				return new PlayerID("", uint.MaxValue, "");
			}
		}

		public PlayerID(string addr, uint cnetID, string name) {
			MacAddress = addr;
			CNetID = cnetID;
			Name = name;
		}
		public PlayerID(PlayerID orig) {
			MacAddress = orig.MacAddress;
			CNetID = orig.CNetID;
			Name = orig.Name;
		}
		public string MacAddress { get; private set; }
		public string Name { get; private set; }
		public uint CNetID { get; private set; }

		public bool MatchAndUpdate(PlayerID id) {
			if (this.Equals(id)) {
				CNetID = id.CNetID;
				return true;
			}
			return false;
		}

		public bool IsDefault() {
			return string.IsNullOrEmpty(MacAddress) && string.IsNullOrEmpty(Name);
		}

		public override bool Equals(object obj) {
			return obj != null && obj is PlayerID id && id.MacAddress == MacAddress && id.Name == Name;
		}

		public override int GetHashCode() {
			return (MacAddress + Name).GetHashCode();
		}
	}

	public static class PlayerIDExt {
		public static PlayerID ReadPlayerID(this CelesteNetBinaryReader r) {
			string mac = r.ReadString();
			string name = r.ReadString();
			uint cnetid = r.ReadUInt32();
			return new PlayerID(mac, cnetid, name);
		}
		public static void Write(this CelesteNetBinaryWriter w, PlayerID id) {
			w.Write(id.MacAddress);
			w.Write(id.Name);
			w.Write(id.CNetID);
		}
	}
}
