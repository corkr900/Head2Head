using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.Client;
using Celeste.Mod.CelesteNet.Client.Components;
using Celeste.Mod.Head2Head.Integration;
using Celeste.Mod.Head2Head.IO;
using Celeste.Mod.Head2Head.Shared;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Celeste.TextMenuExt;

namespace Celeste.Mod.Head2Head.UI
{
	public static class Menus
	{
		public class HelpdeskMenuContext
		{
			public HelpdeskMenuContext(Level level, int returnIndex, bool fromPause)
			{
				this.level = level;
				this.returnToIndex.Push(returnIndex);
				this.fromPauseMenu = fromPause;
			}

			public Level level;
			public bool fromPauseMenu;
			public Stack<int> returnToIndex = new Stack<int>();
			public Stack<Action<HelpdeskMenuContext>> menus = new Stack<Action<HelpdeskMenuContext>>();
			public string matchID;
			public MatchDefinition match {
				get {
					if (string.IsNullOrEmpty(matchID)) return null;
					if (!Head2HeadModule.knownMatches.ContainsKey(matchID)) return null;
					return Head2HeadModule.knownMatches[matchID];
				}
			}
			internal RandomizerCustomOptionsFile RandoOptionsFile;
			public RandomizerCustomOptionsCategory CustomRandoCategory;

			public PlayerID player;

			public Action<PlayerID> onPlayerSelection;

			public Action OnLeave;

			public void GoTo(Action<HelpdeskMenuContext> target, TextMenu current)
			{
				if (current == null) {
					Audio.Play("event:/ui/game/pause");
				}
				else {
					Audio.Play("event:/ui/main/button_select");
				}
				OnLeave?.Invoke();
				OnLeave = null;
				returnToIndex.Push(current == null ? 0 : current.IndexOf(current.Current));
				menus.Push(target);
				current?.RemoveSelf();
				target.Invoke(this);
			}

			public void Back(TextMenu current)
			{
				Audio.Play("event:/ui/main/button_back");
				OnLeave?.Invoke();
				OnLeave = null;
				current.RemoveSelf();
				menus.Pop();
				int index = returnToIndex.Pop();
				if (menus.Count != 0)
				{
					menus.Peek().Invoke(this);
				}
				else if (fromPauseMenu)
				{
					level.Pause(index, minimal: false);
				}
				else
				{
					Close(null);
				}
			}

			public void Refresh(TextMenu current) {
				Audio.Play("event:/ui/main/button_lowkey");
				OnLeave?.Invoke();
				OnLeave = null;
				current.RemoveSelf();
				menus.Peek().Invoke(this);
			}

			public void Close(TextMenu curr) {
				Audio.Play("event:/ui/game/unpause");
				OnLeave?.Invoke();
				OnLeave = null;
				if (curr != null) curr.RemoveSelf();
				level.unpauseTimer = 0.15f;
				level.Paused = false;
			}
		}

		#region Helpers

		private static ButtonExt AddButton(this TextMenu menu, string labelDesc, Action onPress, bool labelIsLiteral = false) {
			ButtonExt btn = new ButtonExt(labelIsLiteral ? labelDesc : Dialog.Clean(labelDesc));
			btn.ConfirmSfx = "";
			DynamicData dd = DynamicData.For(btn);
			dd.Set("H2H_SoftDisable", false);
			btn.Pressed(() => {
				DynamicData dd2 = DynamicData.For(btn);
				if (dd2.Get<bool>("H2H_SoftDisable")) {
					Audio.Play("event:/ui/main/button_invalid");
					return;
				}
				onPress();
			});
			menu.Add(btn);
			return btn;
		}

		private static void SoftDisable(this ButtonExt btn, TextMenu menu, string newSubtext, params string[] fmtArgs) {
			btn.TextColor = btn.TextColorDisabled;
			btn.SetSubtext(menu, string.Format(GetDialogWithLineBreaks(newSubtext), fmtArgs));
			DynamicData dd = DynamicData.For(btn);
			dd.Set("H2H_SoftDisable", true);
			if (menu.IndexOf(btn) == menu.Selection) {
				btn.OnEnter();
			}
		}

		private static void SetSubtext(this ButtonExt btn, TextMenu menu, string newSubtext, params string[] fmtArgs) {
			if (!menu.Items.Contains(btn)) return;
			int idx = menu.IndexOf(btn);
			if (menu.Items.Count >= menu.IndexOf(btn) + 2 && menu.Items[idx + 1] is EaseInSubHeaderExt oldDescription) {
				menu.Remove(oldDescription);
			}
			btn.AddDescription(menu, string.Format(GetDialogWithLineBreaks(newSubtext), fmtArgs));
			if (menu.IndexOf(btn) == menu.Selection) {
				btn.OnEnter();
			}
		}

		public static void Enable(this ButtonExt btn, TextMenu menu, string newSubtext) {
			btn.Enable();
			btn.SetSubtext(menu, newSubtext);
		}

		private static void Enable(this  ButtonExt btn) {
			btn.TextColor = Color.White;
			DynamicData dd = DynamicData.For(btn);
			dd.Set("H2H_SoftDisable", false);
		}

		private static string GetDialogWithLineBreaks(string key) {
			return (Dialog.Has(key) ? Dialog.Get(key) : key ?? "").Replace("{n}", "\n").Replace("{break}", "\n");
		}

		#endregion

		#region Actual Menus

		public static void Helpdesk(HelpdeskMenuContext cxt)
		{
			cxt.level.Paused = true;
			TextMenu menu = new TextMenu();
			menu.AutoScroll = false;
			menu.Position = new Vector2((float)Engine.Width / 2f, (float)Engine.Height / 2f - 100f);
			ButtonExt btn;
			MatchDefinition def_menu = PlayerStatus.Current.CurrentMatch;

			// Head 2 Head Helpdesk
			menu.Add(new TextMenu.Header(Dialog.Clean("Head2Head_menu_helpdesk")));

			// Back
			btn = menu.AddButton("Head2Head_menu_back", () => {
				menu.OnCancel();
			});
			
			// Why can't I Create a Match?
			if ((PlayerStatus.Current?.CurrentArea.Equals(GlobalAreaKey.Head2HeadLobby) ?? true) && !Head2HeadModule.Instance.CanBuildMatch()) {
				btn = menu.AddButton("Head2Head_menu_helpdesk_whynocreatematch", () => { });
				if (Util.IsUpdateAvailable())
					btn.SoftDisable(menu, "Head2Head_menu_helpdesk_whynocreatematch_update");
				else if (!CNetComm.Instance.IsConnected)
					btn.SoftDisable(menu, "Head2Head_menu_helpdesk_whynocreatematch_notconnected");
				else if (CNetComm.Instance.CurrentChannelIsMain)
					btn.SoftDisable(menu, "Head2Head_menu_helpdesk_whynocreatematch_mainchannel");
				else if (!RoleLogic.AllowMatchCreate())
					btn.SoftDisable(menu, "Head2Head_menu_helpdesk_whynocreatematch_role");
				else if (!PlayerStatus.Current.CanStageMatch())
					btn.SoftDisable(menu, "Head2Head_menu_helpdesk_whynocreatematch_playerstatus");
				else
					btn.SoftDisable(menu, "Head2Head_menu_helpdesk_whynocreatematch_other");
			}

			if (def_menu != null) {
				btn = menu.AddButton("Head2Head_menu_helpdesk_whatisthiscategory", () => {
					cxt.matchID = def_menu.MatchID;
					cxt.GoTo(DescribeCategory, menu);
				});
			}

			// Browse
			btn = menu.AddButton("Head2Head_menu_helpdesk_browse", () => {
				cxt.GoTo(BrowseMatches, menu);
			});

			// Drop Out
			bool menuHasDropOutButton = false;
			if (def_menu != null) {
				if (def_menu.PlayerCanLeaveFreely(PlayerID.MyIDSafe)) {
					btn = menu.AddButton("Head2Head_menu_helpdesk_removeOverlay", () => {
						ControlPanel.Commands.Outgoing.MatchNoLongerCurrent(def_menu.MatchID);
						PlayerStatus.Current.CurrentMatch = null;
						PlayerStatus.Current.Updated();
						cxt.Close(menu);
					});
					btn.AddDescription(menu, Dialog.Clean("Head2Head_menu_helpdesk_removeOverlay_subtext"));
				}
				else {
					btn = menu.AddButton("Head2Head_menu_helpdesk_dropout", () => {
						Head2HeadModule.Instance.DropOutOfCurrentMatch(); 
						cxt.Close(menu);
					});
					ResultCategory? rescatdrop = def_menu?.GetPlayerResultCat(PlayerID.MyIDSafe);
					if (def_menu == null)
						btn.SoftDisable(menu, "Head2Head_menu_helpdesk_forceend_nocurrent");
					else if (rescatdrop.Value == ResultCategory.NotJoined)
						btn.SoftDisable(menu, "Head2Head_menu_helpdesk_dropout_notjoined");
					else if (rescatdrop == ResultCategory.Completed || rescatdrop == ResultCategory.DNF)
						btn.SoftDisable(menu, "Head2Head_menu_helpdesk_dropout_completed");
					else {
						menuHasDropOutButton = true;
						btn.AddDescription(menu, Dialog.Clean("Head2Head_menu_helpdesk_dropout_subtext"));
					}
				}

				// Force End
				if (def_menu.State < MatchState.Completed && RoleLogic.AllowKillingMatch()) {
					btn = menu.AddButton("Head2Head_menu_helpdesk_forceend", () => {
						Head2HeadModule.Instance.KillMatch(PlayerStatus.Current.CurrentMatch);
						cxt.Refresh(menu);
					});
					if (def_menu == null)
						btn.SoftDisable(menu, "Head2Head_menu_helpdesk_forceend_nocurrent");
					else if (def_menu.State == MatchState.Completed)
						btn.SoftDisable(menu, "Head2Head_menu_helpdesk_forceend_completed");
					else btn.AddDescription(menu, Dialog.Clean("Head2Head_menu_helpdesk_forceend_subtext"));
				}
			}

			if (RoleLogic.IsDebug) {
				// Purge Data
				btn = menu.AddButton("Head2Head_menu_helpdesk_purge", () => {
					Head2HeadModule.Instance.PurgeAllData();
					cxt.Refresh(menu);
				});
				btn.AddDescription(menu, Dialog.Clean("Head2Head_menu_helpdesk_purge_subtext"));

				// Pull Data
				btn = menu.AddButton("Head2Head_menu_helpdesk_pulldata", () => {
					CNetComm.Instance.SendScanRequest();
					cxt.Refresh(menu);
				});
				btn.AddDescription(menu, Dialog.Clean("Head2Head_menu_helpdesk_pulldata_subtext"));
			}

			// Role-based additions
			if (RoleLogic.CanGrantMatchPass()) {
				btn = menu.AddButton("Head2Head_menu_helpdesk_giveMatchPass", () => {
					cxt.onPlayerSelection = (PlayerID id) => {
						CNetComm.Instance.SendMisc(Head2HeadModule.BTA_MATCH_PASS, id);
						cxt.Close(menu);
					};
					cxt.GoTo(PlayerSelection, menu);
				});
				btn.AddDescription(menu, Dialog.Clean("Head2Head_menu_helpdesk_giveMatchPass_subtext"));

				if (def_menu?.CanApplyTimeAdjustments() ?? false) {
					btn = menu.AddButton("Head2Head_menu_helpdesk_timeadjust", () => {
						cxt.GoTo(GiveAdditionalTime, menu);
					});
				}
			}

			// Return to Lobby
			if (!menuHasDropOutButton && !PlayerStatus.Current.CurrentArea.Equals(GlobalAreaKey.Head2HeadLobby)) {
				btn = menu.AddButton("Head2Head_menu_helpdesk_gotolobby", () => {
					new FadeWipe(menu.Scene, false, () => {
						LevelEnter.Go(new Session(GlobalAreaKey.Head2HeadLobby.Local.Value), false);
					});
					Head2HeadModule.Instance.ClearAutoLaunchInfo();
					cxt.Close(menu);
				});
			}

			// Settings Manager
			btn = menu.AddButton("Head2Head_SettingsManager_Title", () => {
				cxt.GoTo(SettingsManager, menu);
			});

			// handle Cancel button
			menu.OnCancel = () => {
				cxt.Back(menu);
			};
			menu.Selection = menu.FirstPossibleSelection;
			cxt.level.Add(menu);
		}

		public static void BrowseMatches(HelpdeskMenuContext cxt)
		{
			cxt.level.Paused = true;
			TextMenu menu = new TextMenu();
			menu.AutoScroll = false;
			menu.Position = new Vector2((float)Engine.Width / 2f, (float)Engine.Height / 2f - 100f);
			ButtonExt btn;

			// Head 2 Head Helpdesk
			menu.Add(new TextMenu.Header(Dialog.Clean("Head2Head_menu_helpdesk_browse")));

			// Back
			btn = menu.AddButton("Head2Head_menu_back", () => {
				menu.OnCancel();
			});

			// Loop over known matches
			Head2HeadModule.Instance.DiscardStaleData();
			foreach (MatchDefinition def in Head2HeadModule.knownMatches.Values)
			{
				btn = menu.AddButton(def.MatchDisplayName, () => {
					cxt.matchID = def.MatchID;
					cxt.GoTo(KnownMatchMenu, menu);
				}, true);
				string desc = string.Format(GetDialogWithLineBreaks("Head2Head_menu_browsematchdescription"),
					def.Owner.Name, def.Players.Count, Util.TranslatedMatchState(def.State));
				btn.AddDescription(menu, desc);
			}

			// handle Cancel button
			menu.OnCancel = () => {
				cxt.Back(menu);
			};
			menu.Selection = menu.FirstPossibleSelection;
			cxt.level.Add(menu);
		}

		public static void KnownMatchMenu(HelpdeskMenuContext cxt)
		{
			cxt.level.Paused = true;
			TextMenu menu = new TextMenu();
			menu.AutoScroll = false;
			menu.Position = new Vector2((float)Engine.Width / 2f, (float)Engine.Height / 2f - 100f);
			ButtonExt btn;
			MatchDefinition cxtMatch = cxt.match;

			if (cxtMatch == null)
			{
				// Header
				menu.Add(new TextMenu.Header(Dialog.Clean("Head2Head_menu_match_invalidmatch")));

				// Back
				btn = menu.AddButton("Head2Head_menu_back", () => {
					menu.OnCancel();
				});
			}
			else {
				MatchDefinition curmatch = PlayerStatus.Current.CurrentMatch;

				// Header
				menu.Add(new TextMenu.SubHeader(cxtMatch.MatchDisplayName));
				string desc = string.Format(GetDialogWithLineBreaks("Head2Head_menu_browsematchdescription"),
					cxtMatch.Owner.Name, cxtMatch.Players.Count, Util.TranslatedMatchState(cxtMatch.State));
				menu.Add(new TextMenu.SubHeader(desc));

				// Back
				btn = menu.AddButton("Head2Head_menu_back", () => {
					menu.OnCancel();
				});

				// Stage
				btn = menu.AddButton("Head2Head_menu_match_stage", () => {
					Head2HeadModule.Instance.StageMatch(cxtMatch);
					cxt.Close(menu);
				});
				GlobalAreaKey? vchk = cxtMatch.VersionCheck();
				if (Util.IsUpdateAvailable())
					btn.SoftDisable(menu, "Head2Head_menu_match_stage_update");
				else if (!cxtMatch.AllPhasesExistLocal()) {
					btn.SoftDisable(menu, "Head2Head_menu_match_join_notinstalled");
				}
				else if (cxtMatch.MatchID == curmatch?.MatchID)
					btn.SoftDisable(menu, "Head2Head_menu_match_stage_alreadycurrent");
				else if (vchk != null)
					btn.SoftDisable(menu, "Head2Head_menu_match_join_versionmismatch", vchk?.LocalVersion.ToString(), vchk?.Version.ToString());
				else if (curmatch?.PlayerCanLeaveFreely(PlayerID.MyIDSafe) == false)
					btn.SoftDisable(menu, "Head2Head_menu_match_stage_dropfirst");
				else
					btn.AddDescription(menu, Dialog.Clean("Head2Head_menu_match_stage_subtext"));

				// Join
				btn = menu.AddButton("Head2Head_menu_match_join", () => {
					if (cxtMatch.MatchID != curmatch?.MatchID)
					{
						Head2HeadModule.Instance.StageMatch(cxtMatch);
					}
					Head2HeadModule.Instance.JoinStagedMatch();
					cxt.Close(menu);
				});
				GlobalAreaKey? k = curmatch?.VersionCheck();
				if (Util.IsUpdateAvailable())
					btn.SoftDisable(menu, "Head2Head_menu_match_stage_update");
				else if (!RoleLogic.AllowMatchJoin(cxtMatch))
					btn.SoftDisable(menu, "Head2Head_menu_match_join_role");
				else if (!cxtMatch.AllPhasesExistLocal()) {
					btn.SoftDisable(menu, "Head2Head_menu_match_join_notinstalled");
				}
				else if (k != null)
					btn.SoftDisable(menu, "Head2Head_menu_match_join_versionmismatch", k.Value.LocalVersion.ToString(), k.Value.Version.ToString());
				else if (cxtMatch.State != MatchState.Staged)
					btn.SoftDisable(menu, "Head2Head_menu_match_stage_wrongstatus");
				else if (cxtMatch.GetPlayerResultCat(PlayerID.MyIDSafe) >= ResultCategory.Joined)
					btn.SoftDisable(menu, "Head2Head_menu_match_join_alreadyjoined");
				else if (curmatch?.PlayerCanLeaveFreely(PlayerID.MyIDSafe) == false)
					btn.SoftDisable(menu, "Head2Head_menu_match_stage_dropfirst");
				else
					btn.AddDescription(menu, Dialog.Clean("Head2Head_menu_match_join_subtext"));

				// Export Log
				if (ActionLogger.LogFileExists(cxtMatch.MatchID)) {
					btn = menu.AddButton("Head2Head_menu_match_export", () => {
						ActionLogger.Export(cxtMatch.MatchID);
						cxt.Close(menu);
					});
					btn.AddDescription(menu, Dialog.Clean("Head2Head_menu_match_export_subtext"));
				}

				// Forget
				btn = menu.AddButton("Head2Head_menu_match_forget", () => {
					Head2HeadModule.Instance.TryForgetMatch(cxtMatch.MatchID);
					cxt.Back(menu);
				});
				ResultCategory cat = cxtMatch.GetPlayerResultCat(PlayerID.MyIDSafe);
				if (cxtMatch.MatchID == PlayerStatus.Current.CurrentMatchID
					&& (cat == ResultCategory.Joined || cat == ResultCategory.InMatch))
					btn.SoftDisable(menu, "Head2Head_menu_match_forget_current");
				else
					btn.AddDescription(menu, Dialog.Clean("Head2Head_menu_match_forget_subtext"));
			}

			// handle Cancel button
			menu.OnCancel = () => {
				cxt.Back(menu);
			};
			menu.Selection = menu.FirstPossibleSelection;
			cxt.level.Add(menu);
		}

		public static void PlayerSelection(HelpdeskMenuContext cxt) {
			cxt.level.Paused = true;
			TextMenu menu = new TextMenu();
			menu.AutoScroll = false;
			menu.Position = new Vector2((float)Engine.Width / 2f, (float)Engine.Height / 2f - 100f);
			ButtonExt btn;

			// Head 2 Head Helpdesk
			menu.Add(new TextMenu.Header(Dialog.Clean("Head2Head_menu_helpdesk_playerselect")));

			// Back
			btn = menu.AddButton("Head2Head_menu_back", () => {
				menu.OnCancel();
			});

			// Loop over known players
			foreach (PlayerID id in Head2HeadModule.knownPlayers.Keys) {
				btn = menu.AddButton(id.Name, () => {
					cxt.onPlayerSelection?.Invoke(id);
					cxt.Close(menu);
				}, true);
			}

			// handle Cancel button
			menu.OnCancel = () => {
				cxt.Back(menu);
			};
			menu.Selection = menu.FirstPossibleSelection;
			cxt.level.Add(menu);
		}

		public static void GiveAdditionalTime(HelpdeskMenuContext cxt) {
			cxt.level.Paused = true;
			TextMenu menu = new TextMenu();
			menu.AutoScroll = false;
			menu.Position = new Vector2((float)Engine.Width / 2f, (float)Engine.Height / 2f - 100f);
			ButtonExt btn;
			MatchDefinition curdef = PlayerStatus.Current.CurrentMatch;
			Dictionary<PlayerID, int> adjustments = new Dictionary<PlayerID, int>();

			// Head 2 Head Helpdesk
			menu.Add(new TextMenu.Header(Dialog.Clean("Head2Head_menu_helpdesk_timeadjust")));

			if (curdef != null && curdef.CanApplyTimeAdjustments()) {
				MatchObjective ob = curdef.Phases[0].Objectives[0];
				int maxAdjust = (int)(ob.TimeLimit / Util.TimeValueInternal(0, 1));

				// Loop over known players
				foreach (PlayerID id in curdef.Players) {
					int curadj = (int)Util.TimeToSeconds(ob.GetAdjustment(id));
					adjustments.Add(id, curadj);

					TextMenuExt.IntSlider slider = new TextMenuExt.IntSlider(id.Name, -maxAdjust + 1, maxAdjust * 2, curadj);
					slider.Change((int newVal) => {
						adjustments[id] = newVal;
					});
					menu.Add(slider);
				}

				// Confirm
				btn = menu.AddButton("Head2Head_menu_confirm", () => {
					foreach(KeyValuePair<PlayerID, int> kvp in adjustments) {
						ob.SetAdjustment(kvp.Key, Util.TimeValueInternal(0, kvp.Value));
					}
					curdef.BroadcastUpdate();
					cxt.Close(menu);
				});
			}
			else {
				menu.Add(new TextMenu.SubHeader(Dialog.Clean("Head2Head_menu_helpdesk_notimeadjust")));
			}

			// Cancel
			btn = menu.AddButton("Head2Head_menu_cancel", () => {
				menu.OnCancel();
			});

			// handle Cancel button
			menu.OnCancel = () => {
				cxt.Back(menu);
			};
			menu.Selection = menu.FirstPossibleSelection;
			cxt.level.Add(menu);
		}

		public static void DescribeCategory(HelpdeskMenuContext cxt) {
			cxt.level.Paused = true;
			TextMenu menu = new TextMenu();
			menu.AutoScroll = false;
			menu.Position = new Vector2((float)Engine.Width / 2f, (float)Engine.Height / 2f - 100f);
			ButtonExt btn;
			MatchDefinition def = cxt.match;

			if (def != null) {
				menu.Add(new TextMenu.Header(def.MatchDisplayName));

				foreach (MatchPhase ph in def.Phases) {
					btn = menu.AddButton(string.Format(Dialog.Get("Head2Head_menu_AreaLabel"), ph.Area.DisplayName), () => { }, true);

					foreach (MatchObjective ob in ph.Objectives) {
						btn = menu.AddButton("- " + ob.Description, () => {}, true);
						btn.Disabled = true;
					}
				}

			}

			// Cancel
			btn = menu.AddButton("Head2Head_menu_back", () => {
				menu.OnCancel();
			});

			// handle Cancel button
			menu.OnCancel = () => {
				cxt.Back(menu);
			};
			menu.Selection = menu.FirstPossibleSelection;
			cxt.level.Add(menu);
		}

		#endregion

		#region Settings Menus

		public static void SettingsManager(HelpdeskMenuContext cxt) {
			cxt.level.Paused = true;
			TextMenu menu = new TextMenu();
			menu.AutoScroll = false;
			menu.Position = new Vector2((float)Engine.Width / 2f, (float)Engine.Height / 2f - 100f);
			ButtonExt btn;
			ButtonExt connectCnetBtn = null;
			ButtonExt ctrlPanelBtn = null;
			ButtonExt randoBldrBtn = null;
			ButtonExt joinChnlH2HBtn = null;
			ButtonExt joinChnlMainBtn = null;

			// Header
			menu.Add(new TextMenu.Header(Dialog.Clean("Head2Head_SettingsManager_Title")));

			// Settings
			btn = menu.AddButton("menu_modoptions", () => {
				cxt.GoTo(ModOptionsStandalone, menu);
			});

			// Settings
			if (CNetComm.Instance?.IsConnected != true) {
				connectCnetBtn = menu.AddButton("Head2Head_SettingsManager_ConnectToCnet", () => {
					CelesteNetClientModule.Settings.Connected = true;
					connectCnetBtn.SoftDisable(menu, "Head2Head_SettingsManager_ConnectionStarted");
				});
				if (CNetComm.Instance.IsConnected) {
					connectCnetBtn.SoftDisable(menu, "Head2Head_SettingsManager_AlreadyConnected");
				}
				else {
					connectCnetBtn.SetSubtext(menu, "Head2Head_SettingsManager_ConnectToCnet_Sub");
				}
			}

			// Channel (h2h)
			joinChnlH2HBtn = menu.AddButton("Head2Head_SettingsManager_JoinChannel_h2h", () => {
				CelesteNetChatComponent chat = CNetComm.Instance?.CnetContext?.Chat;
				if (chat == null) {
					joinChnlH2HBtn.SetSubtext(menu, "Head2Head_SettingsManager_SentCnetCommand");
				}
				else {
					chat?.Send("/join h2h");
					joinChnlH2HBtn.SetSubtext(menu, "Head2Head_SettingsManager_SentCnetCommand");
				}
			});
			if (CNetComm.Instance?.IsConnected != true) {
				joinChnlH2HBtn.SoftDisable(menu, "Head2Head_menu_helpdesk_whynocreatematch_notconnected");
			}
			else if (CNetComm.Instance?.CurrentChannel?.Name == "h2h") {
				joinChnlH2HBtn.SoftDisable(menu, "Head2Head_SettingsManager_JoinChannel_AlreadyInThisChannel");
			}
			else {
				joinChnlH2HBtn.SetSubtext(menu, "Head2Head_SettingsManager_JoinChannel_h2h_Sub");
			}

			// Channel (main)
			joinChnlMainBtn = menu.AddButton("Head2Head_SettingsManager_JoinChannel_main", () => {
				CelesteNetChatComponent chat = CNetComm.Instance?.CnetContext?.Chat;
				if (chat == null) {
					joinChnlMainBtn.SetSubtext(menu, "Head2Head_SettingsManager_SentCnetCommand");
				}
				else {
					chat?.Send("/join main");
					joinChnlMainBtn.SetSubtext(menu, "Head2Head_SettingsManager_SentCnetCommand");
				}
			});
			if (CNetComm.Instance?.IsConnected != true) {
				joinChnlMainBtn.SoftDisable(menu, "Head2Head_menu_helpdesk_whynocreatematch_notconnected");
			}
			else if (CNetComm.Instance?.CurrentChannel?.Name == "main") {
				joinChnlMainBtn.SoftDisable(menu, "Head2Head_SettingsManager_JoinChannel_AlreadyInThisChannel");
			}

			// Control Panel
			ctrlPanelBtn = menu.AddButton("Head2Head_SettingsManager_OpenControlPanel", () => {
				string url = "https://corkr900.github.io/Head2Head/ControlPanel/ControlPanel.html";
				if (!Util.OpenUrl(url)) {
					ctrlPanelBtn.SoftDisable(menu, "Head2Head_SettingsManager_CantOpenURLs", url);
				}
				else {
					ctrlPanelBtn.SetSubtext(menu, "Head2Head_SettingsManager_OpenedURL");
				}
			});

			// Randomizer Category Builder
			randoBldrBtn = menu.AddButton("Head2Head_SettingsManager_OpenRandoBuilder", () => {
				string url = "https://corkr900.github.io/Head2Head/ControlPanel/RandoCategoryBuilder.html";
				if (!Util.OpenUrl(url)) {
					randoBldrBtn.SoftDisable(menu, "Head2Head_SettingsManager_CantOpenURLs", url);
				}
				else {
					randoBldrBtn.SetSubtext(menu, "Head2Head_SettingsManager_OpenedURL");
				}
			});

			// Manage Custom Categories
			btn = menu.AddButton("Head2Head_SettingsManager_ManageCustomCategories", () => {
				cxt.GoTo(ManageCustomCategories, menu);
			});

			// Back
			btn = menu.AddButton("Head2Head_menu_back", () => {
				menu.OnCancel();
			});

			// Handle live updates to connection status and channel
			CNetComm.OnConnectedHandler onConn = (cxt) => {
				connectCnetBtn?.SoftDisable(menu, "Head2Head_SettingsManager_AlreadyConnected");
				string newChannel = CNetComm.Instance.CurrentChannel?.Name;
				if (newChannel == "main") {
					joinChnlMainBtn?.SoftDisable(menu, "Head2Head_SettingsManager_JoinChannel_AlreadyInThisChannel");
					joinChnlH2HBtn?.Enable(menu, "Head2Head_SettingsManager_JoinChannel_h2h_Sub");
				}
				else if (newChannel == "h2h") {
					joinChnlMainBtn?.Enable(menu, "");
					joinChnlH2HBtn?.SoftDisable(menu, "Head2Head_SettingsManager_JoinChannel_AlreadyInThisChannel");
				}
			};
			CNetComm.OnDisonnectedHandler onDiscon = (conn) => {
				connectCnetBtn?.Enable(menu, "");
				joinChnlH2HBtn?.SoftDisable(menu, "Head2Head_menu_helpdesk_whynocreatematch_notconnected");
				joinChnlMainBtn?.SoftDisable(menu, "Head2Head_menu_helpdesk_whynocreatematch_notconnected");
			};
			CNetComm.OnReceiveChannelMoveHandler onChan = (data) => {
				string newChannel = CNetComm.Instance.CurrentChannel?.Name;
				if (newChannel == "main") {
					joinChnlMainBtn?.SoftDisable(menu, "Head2Head_SettingsManager_JoinChannel_AlreadyInThisChannel");
					joinChnlH2HBtn?.Enable(menu, "");
				}
				else if (newChannel == "h2h") {
					joinChnlMainBtn?.Enable(menu, "");
					joinChnlH2HBtn?.SoftDisable(menu, "Head2Head_SettingsManager_JoinChannel_AlreadyInThisChannel");
				}
			};

			CNetComm.OnConnected += onConn;
			CNetComm.OnDisconnected += onDiscon;
			CNetComm.OnReceiveChannelMove += onChan;
			cxt.OnLeave = () => {
				CNetComm.OnConnected -= onConn;
				CNetComm.OnDisconnected -= onDiscon;
				CNetComm.OnReceiveChannelMove -= onChan;
			};

			// handle Cancel button
			menu.OnCancel = () => {
				cxt.Back(menu);
			};
			menu.Selection = menu.FirstPossibleSelection;
			cxt.level.Add(menu);
		}

		public static void ModOptionsStandalone(HelpdeskMenuContext cxt) {
			cxt.level.Paused = true;
			TextMenu menu = new TextMenu();
			menu.AutoScroll = false;
			menu.Position = new Vector2((float)Engine.Width / 2f, (float)Engine.Height / 2f - 100f);

			// Header
			menu.Add(new TextMenu.Header(Dialog.Clean("menu_modoptions")));

			// Options
			Head2HeadModule.Instance.CreateModMenuSection(menu, false, null);

			// Back
			ButtonExt btn = menu.AddButton("Head2Head_menu_back", () => {
				menu.OnCancel();
			});

			// handle Cancel button
			menu.OnCancel = () => {
				cxt.Back(menu);
			};
			menu.Selection = menu.FirstPossibleSelection;
			cxt.level.Add(menu);
		}

		public static void ManageCustomCategories(HelpdeskMenuContext cxt) {
			cxt.level.Paused = true;
			TextMenu menu = new TextMenu();
			menu.AutoScroll = false;
			menu.Position = new Vector2((float)Engine.Width / 2f, (float)Engine.Height / 2f - 100f);
			ButtonExt btn;

			// Title
			menu.Add(new TextMenu.Header(Dialog.Clean("Head2Head_SettingsManager_ManageCustomCategories")));

			// Loop over known matches
			cxt.RandoOptionsFile = RandomizerCustomOptionsFile.Instance;
			if (cxt.RandoOptionsFile.Categories.Count == 0) {
				menu.Add(new TextMenu.SubHeader(Dialog.Clean("Head2Head_SettingsManager_NoCustomRandoCategories")));
			}
			else {
				menu.Add(new TextMenu.SubHeader(Dialog.Clean("Head2Head_SettingsManager_CustomRandoCategories")));
				foreach (RandomizerCustomOptionsCategory cat in cxt.RandoOptionsFile.Categories) {
					btn = menu.AddButton(cat.Name, () => {
						cxt.CustomRandoCategory = cat;
						cxt.GoTo(CustomRandoCategoryOptions, menu);
					}, true);
				}
			}

			// Back
			btn = menu.AddButton("Head2Head_menu_back", () => {
				menu.OnCancel();
			});

			// handle Cancel button
			menu.OnCancel = () => {
				cxt.Back(menu);
			};
			menu.Selection = menu.FirstPossibleSelection;
			cxt.level.Add(menu);
		}

		private static void CustomRandoCategoryOptions(HelpdeskMenuContext cxt) {
			cxt.level.Paused = true;
			TextMenu menu = new TextMenu();
			menu.AutoScroll = false;
			menu.Position = new Vector2((float)Engine.Width / 2f, (float)Engine.Height / 2f - 100f);
			ButtonExt btn;

			// Title
			menu.Add(new TextMenu.Header(cxt.CustomRandoCategory.Name));

			// Delete
			btn = menu.AddButton("Head2Head_SettingsManager_Delete", () => {
				cxt.RandoOptionsFile.Categories.Remove(cxt.CustomRandoCategory);
				RandomizerCustomOptionsFile.Save();
				menu.OnCancel();
			});

			// Back
			btn = menu.AddButton("Head2Head_menu_back", () => {
				menu.OnCancel();
			});

			// handle Cancel button
			menu.OnCancel = () => {
				cxt.Back(menu);
			};
			menu.Selection = menu.FirstPossibleSelection;
			cxt.level.Add(menu);
		}

		#endregion
	}
}
