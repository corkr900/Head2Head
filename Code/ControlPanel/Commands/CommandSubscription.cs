using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.ControlPanel.Commands {
	internal class CommandSubscription {

		// LIST OF COMMANDS THAT EXIST BUT HAVE NO ASSOCIATED SUBSCRIPTION:
		// - WELCOME

		#region default-on subscriptions

		/// <summary>
		/// The websocket will reply to incoming commands to indicate the result.
		/// Subscribed by default since this is usually an essential function.
		/// Commands: RESULT
		/// </summary>
		public static CommandSubscription CommandResult => new CommandSubscription() {
			SubscriptionName = "COMMAND_RESULT",
			Commands = new string[] { "RESULT" },
			Subscribed = true,
		};

		/// <summary>
		/// The websocket may send match logs to control panel clients.
		/// Subscribed by default since this is only ever initiated by a CP client.
		/// Commands: MATCH_LOG
		/// </summary>
		public static CommandSubscription MatchLog => new CommandSubscription() {
			SubscriptionName = "MATCH_LOG",
			Commands = new string[] { "MATCH_LOG" },
			Subscribed = true,
		};

		/// <summary>
		/// The websocket may send serialized image data to clients
		/// Subscribed by default since this is initiated by the client.
		/// Commands: IMAGE
		/// </summary>
		public static CommandSubscription ImageData => new CommandSubscription() {
			SubscriptionName = "IMAGE",
			Commands = new string[] { "IMAGE" },
			Subscribed = true,
		};

		/// <summary>
		/// The websocket will reply to a GET_PLAYER_LIST command with a list of players.
		/// Subscribed by default since this is only sent as a response to a client command.
		/// Commands: PLAYER_LIST
		/// </summary>
		public static CommandSubscription PlayerListManual => new CommandSubscription() {
			SubscriptionName = "PLAYER_LIST",
			Commands = new string[] { "PLAYER_LIST" },
			Subscribed = true,
		};

		/// <summary>
		/// The websocket will reply to a GET_ENABLED_MODS command with a PLAYER_ENABLED_MODS command.
		/// Subscribed by default since this is only sent as a response to a client command.
		/// Commands: PLAYER_ENABLED_MODS
		/// </summary>
		public static CommandSubscription PlayerEnabledMods => new CommandSubscription() {
			SubscriptionName = "PLAYER_ENABLED_MODS",
			Commands = new string[] { "PLAYER_ENABLED_MODS" },
			Subscribed = true,
		};

		#endregion default-on subscriptions

		#region Optional Subscriptions

		/// <summary>
		/// Receive information about the current match.
		/// Commands: CURRENT_MATCH, MATCH_NOT_CURRENT
		/// </summary>
		public static CommandSubscription CurrentMatch => new CommandSubscription() {
			SubscriptionName = "CURRENT_MATCH",
			Commands = new string[] { "CURRENT_MATCH", "MATCH_NOT_CURRENT" },
		};

		/// <summary>
		/// Receive information about matches that are not the player's current.
		/// Commands: OTHER_MATCH
		/// </summary>
		public static CommandSubscription OtherMatch => new CommandSubscription() {
			SubscriptionName = "OTHER_MATCH",
			Commands = new string[] { "OTHER_MATCH" },
		};

		/// <summary>
		/// Notification to clients that a match was forgotten by Head 2 Head
		/// Commands: MATCH_FORGOTTEN
		/// </summary>
		public static CommandSubscription MatchForgotten => new CommandSubscription() {
			SubscriptionName = "MATCH_FORGOTTEN",
			Commands = new string[] { "MATCH_FORGOTTEN" },
		};

		/// <summary>
		/// Notifies clients when the set of available incoming commands changes
		/// Commands: UPDATE_ACTIONS
		/// </summary>
		public static CommandSubscription ActionsUpdate => new CommandSubscription() {
			SubscriptionName = "UPDATE_ACTIONS",
			Commands = new string[] { "UPDATE_ACTIONS" },
		};

		#endregion Optional Subscriptions

		/// <summary>
		/// Enumerates all available subscriptions
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<CommandSubscription> AllAvailableSubscriptions() {
			yield return CommandResult;
			yield return MatchLog;
			yield return ImageData;
			yield return PlayerListManual;
			yield return PlayerEnabledMods;
			yield return CurrentMatch;
			yield return OtherMatch;
			yield return MatchForgotten;
			yield return ActionsUpdate;
		}

		///////////////////////////////////////////////////////////////

		#region Member Values

		public string SubscriptionName { get; private set; } = "DEFAULT_SUBSCRIPTION";
		public string[] Commands { get; private set; } = Array.Empty<string>();
		public bool Subscribed { get; set; } = false;

		#endregion Member Values

	}
}
