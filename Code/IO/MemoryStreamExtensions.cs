using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Celeste.Mod.Head2Head.IO {
	public static class MemoryStreamExtensions {

		// String
		public static string ReadString(this MemoryStream stream) {
			ushort len = stream.ReadUInt16();
			byte[] data = new byte[len];
			stream.ReadExactly(data, 0, len);
			return Encoding.UTF8.GetString(data);
		}
		public static void Write(this MemoryStream stream, string val) {
			byte[] data = Encoding.UTF8.GetBytes(val);
			stream.Write(BitConverter.GetBytes((ushort)data.Length));
			stream.Write(data);
		}

		// bool
		public static bool ReadBoolean(this MemoryStream stream) {
			return stream.ReadByte() != 0;
		}
		public static void Write(this MemoryStream stream, bool val) {
			stream.WriteByte((byte)(val ? 1 : 0));
		}

		// byte
		// ReadByte already exists
		public static void Write(this MemoryStream stream, byte val) {
			stream.WriteByte(val);
		}

		// ushort
		public static ushort ReadUInt16(this MemoryStream stream) {
			byte[] lenBuf = new byte[2];
			stream.ReadExactly(lenBuf, 0, 2);
			return BitConverter.ToUInt16(lenBuf, 0);
		}
		public static void Write(this MemoryStream stream, ushort val) {
			stream.Write(BitConverter.GetBytes(val));
		}

		// uint
		public static uint ReadUInt32(this MemoryStream stream) {
			byte[] lenBuf = new byte[4];
			stream.ReadExactly(lenBuf, 0, 4);
			return BitConverter.ToUInt32(lenBuf, 0);
		}
		public static void Write(this MemoryStream stream, uint val) {
			stream.Write(BitConverter.GetBytes(val));
		}

		// ulong
		public static ulong ReadUInt64(this MemoryStream stream) {
			byte[] lenBuf = new byte[8];
			stream.ReadExactly(lenBuf, 0, 8);
			return BitConverter.ToUInt32(lenBuf, 0);
		}
		public static void Write(this MemoryStream stream, ulong val) {
			stream.Write(BitConverter.GetBytes(val));
		}

		// short
		public static short ReadInt16(this MemoryStream stream) {
			byte[] lenBuf = new byte[2];
			stream.ReadExactly(lenBuf, 0, 2);
			return BitConverter.ToInt16(lenBuf, 0);
		}
		public static void Write(this MemoryStream stream, short val) {
			stream.Write(BitConverter.GetBytes(val));
		}

		// int
		public static int ReadInt32(this MemoryStream stream) {
			byte[] lenBuf = new byte[4];
			stream.ReadExactly(lenBuf, 0, 4);
			return BitConverter.ToInt32(lenBuf, 0);
		}
		public static void Write(this MemoryStream stream, int val) {
			stream.Write(BitConverter.GetBytes(val));
		}

		// long
		public static long ReadInt64(this MemoryStream stream) {
			byte[] lenBuf = new byte[4];
			stream.ReadExactly(lenBuf, 0, 4);
			return BitConverter.ToInt32(lenBuf, 0);
		}
		public static void Write(this MemoryStream stream, long val) {
			stream.Write(BitConverter.GetBytes(val));
		}

	}
}
