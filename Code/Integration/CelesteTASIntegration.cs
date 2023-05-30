using Celeste.Mod.Head2Head.Shared;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Integration {
	public static class CelesteTASIntegration {

		private static bool IsCelesteTASInstalled = false;

		private static Type Manager;
		private static MethodInfo Manager_EnableRun;

		private static IDetour Hook_Manager_EnableRun;

		internal static void Load() {
			try {
				// Get type info and functions
				Manager = Type.GetType("TAS.Manager,CelesteTAS-EverestInterop");
				if (Manager == null) {
					IsCelesteTASInstalled = false;
					return;
				}
				Manager_EnableRun = Manager.GetMethod("EnableRun", BindingFlags.Public | BindingFlags.Static);

				// Set up hooks
				Hook_Manager_EnableRun = new Hook(Manager_EnableRun,
					typeof(CelesteTASIntegration).GetMethod("OnEnableRun", BindingFlags.NonPublic | BindingFlags.Static));

				// Misc
				IsCelesteTASInstalled = true;
			}
			catch (Exception) {
				IsCelesteTASInstalled = false;
			}
		}

		internal static void Unload() {
			Hook_Manager_EnableRun?.Dispose();
			Hook_Manager_EnableRun = null;
		}

#pragma warning disable IDE0051  // Private method is unused

		private static void OnEnableRun(Action orig) {
			if (PlayerStatus.Current.IsInMatch(true)) {
				PlayerStatus.Current.CurrentMatch?.PlayerDNF(DNFReason.TAS);
			}
			orig();
		}

#pragma warning restore IDE0051  // Private method is unused

	}
}
