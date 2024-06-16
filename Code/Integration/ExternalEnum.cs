using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Integration {
	internal class ExternalEnum {
		public readonly bool IsValid;
		public readonly Type EnumType;
		public readonly IReadOnlyDictionary<string, Enum> Values;

		public ExternalEnum(string typeIdentifier) {
			EnumType = Type.GetType(typeIdentifier);
			if (EnumType == null || !EnumType.IsEnum) {
				EnumType = null;
				IsValid = false;
				return;
			}
			string[] names = EnumType.GetEnumNames();
			Array vals = EnumType.GetEnumValues();
			Dictionary<string, Enum> tmp = new Dictionary<string, Enum>();
			for (int i = 0; i < names.Length; i++) {
				tmp.Add(names[i], (Enum)vals.GetValue(i));
			}
			Values = tmp;
			IsValid = true;
		}

		internal string Valiate(string val, string dflt = null) {
			if (IsValid && Values != null && Values.ContainsKey(val)) { return val; }
			return dflt;
		}

		internal Enum DefaultValue() {
			return IsValid ? (Enum)Activator.CreateInstance(EnumType) : null;
		}

		internal Enum ToVal(string name) {
			return IsValid && Values.ContainsKey(name) ? Values[name] : DefaultValue();
		}
	}
}
