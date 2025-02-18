// http://www.websocket.org/echo.html
let socketIP = "127.0.0.1";
const placeholderImage = "https://github.com/corkr900/Head2Head/blob/main/Graphics/Atlases/Gui/Head2Head/Categories/Custom.png?raw=true";
let imageCache = {};
let printErrors = false;
let isDebugMode = false;
let enableIpEntry = false;

HandleParams();

// Set up the socket
let socket = new H2HSocket(socketIP);
socket.Subscriptions([
	"current_match",
	"other_match",
	"match_forgotten",
	"update_actions",
]);
socket.OnReady = OnReady;
socket.OnClose = OnDisconnected;
socket.OnMessage = OnMessage;

function GetConnectionUri() {
	return `ws://${socketIP}:8080/`;
}

function HandleParams() {
	const queryString = window.location.search;
	const urlParams = new URLSearchParams(queryString);

	isDebugMode = urlParams.get('debug');
	enableIpEntry = urlParams.get('enableipentry');
	const paramIP = urlParams.get('ip');

	if (paramIP) socketIP = paramIP;
	if (!isDebugMode) {
		for (const elem of document.querySelectorAll(".debugOnly")) {
			elem.remove();
		}
	}
	if (!enableIpEntry) {
		for (const elem of document.querySelectorAll(".ipEntryEnabled")) {
			elem.remove();
		}
	}
}

function OnReady() {
	HandleConnectionUpdate(true);
}

function OnDisconnected(e) {
	HandleConnectionUpdate(false);
}

function OnMessage(msg) {
	if (isDebugMode) {
		writeToScreen(`RECEIVED: ${JSON.stringify(msg, null, 4)}`);
	}
	if (msg.Command == "CURRENT_MATCH") {
		RenderCurrentMatchInfo(msg.Data);
		RenderOtherMatchInfo(msg.Data);
	}
	else if (msg.Command == "OTHER_MATCH") {
		RenderOtherMatchInfo(msg.Data);
	}
	else if (msg.Command == "IMAGE") {
		imageCache[msg.Data.Id] = msg.Data.ImgSrc;
		for (const elem of document.querySelectorAll(`[h2h_img_src|="${msg.Data.Id}"]`)) {
			elem.setAttribute("src", imageCache[msg.Data.Id]);
		}
	}
	else if (msg.Command == "MATCH_NOT_CURRENT") {
		if (GetCurMatchId() == msg.Data) {
			RenderCurrentMatchInfo(null);
		}
	}
	else if (msg.Command == "MATCH_FORGOTTEN") {
		RemoveMatchInfo(msg.Data);
	}
	else if (msg.Command == "MATCH_LOG") {
		RenderLog(msg.Data);
	}
	else if (msg.Command == "UPDATE_ACTIONS") {
		UpdateActions(msg.Data);
	}
}

function writeToScreen(message) {
	const output = document.querySelector("#output");
	if (output) {
		output.insertAdjacentHTML("afterbegin", `<p>[${new Date().toISOString()}] ${message}</p>`);
	}
}

function HandleConnectionUpdate(newIsConnected) {
	const statusSpan = document.querySelector("#rend_status");
	statusSpan.textContent = newIsConnected ? "Connected" : "Not Connected. Is the game running, Head 2 Head enabled, and Control Panel enabled in Head 2 Head mod settings?";
	UpdateActions({AvailableActions: []});
}

function GetH2HImageElement(image) {
	var src = image.ImgSrc;
	if (!src) {
		if (imageCache[image.Id]) src = imageCache[image.Id];
		else {
			// TODO this can send duplicates if multiple of the same image comes in on the same request
			socket.Send("REQUEST_IMAGE", image.Id);
			src = placeholderImage;
		}
	}
	const categoryIcon = document.createElement("img");
	categoryIcon.setAttribute("h2h_img_src", image.Id);
	categoryIcon.setAttribute("src", src);
	return categoryIcon
}

// ==========================================================

function onConnectToIpClick() {
	socketIP = document.getElementById("ipInput")?.value ?? socketIP;
	socket.ChangeConnection(socketIP);
}

function UpdateActions(data) {
	const mainActionsDiv = document.querySelector("#mainActionsDiv");
	mainActionsDiv.textContent = "";
	if (data.AvailableActions.includes("UNSTAGE_MATCH")) {
		const btn = MakeButton("Remove Overlay", () => {
			socket.Send("UNSTAGE_MATCH");
		});
		mainActionsDiv.appendChild(btn);
	}
	if (data.AvailableActions.includes("DBG_PURGE_DATA")) {
		const btn = MakeButton("(Debug) Purge Data)", () => {
			socket.Send("DBG_PURGE_DATA");
		});
		mainActionsDiv.appendChild(btn);

	}
	if (data.AvailableActions.includes("DBG_PULL_DATA")) {
		const btn = MakeButton("(Debug) Pull Data", () => {
			socket.Send("DBG_PULL_DATA");
		});
		mainActionsDiv.appendChild(btn);

	}
	if (data.AvailableActions.includes("GIVE_MATCH_PASS")) {
		const btn = MakeButton("Give Match Pass", () => {
			//socket.Send("GIVE_MATCH_PASS");
			alert("This has not been implemented yet.");
		});
		mainActionsDiv.appendChild(btn);

	}
	if (data.AvailableActions.includes("GO_TO_LOBBY")) {
		const btn = MakeButton("Go To H2H Lobby", () => {
			socket.Send("GO_TO_LOBBY");
		});
		mainActionsDiv.appendChild(btn);

	}
}

function GetCurMatchId() {
	const container = document.querySelector("#curMatchBody");
	return container.getAttribute("h2h_match_source");
}

function RemoveMatchInfo(id) {
	if (GetCurMatchId() == id) {
		RenderCurrentMatchInfo(null);
	}
	let matchContainer = document.querySelector(`#allMatchesBody #matchContainer_${id}`);
	if (!matchContainer) return;
	const body = matchContainer.querySelector(".collapsibleContent");
	const prevMaxHeight = body?.style?.maxHeight;
	const btnContainer = body.querySelector(".actionButtonsContainer");
	const forgottenMsg = document.createElement("div");
	forgottenMsg.textContent = "This match was forgotten from Head 2 Head";
	forgottenMsg.className = "margin6";
	body.prepend(forgottenMsg);
	for (const btn of body.querySelectorAll("button:not(.keepAfterForgotten):not(.keepAfterForgotten *)")) {
		btn.remove();
	}
	btnContainer.appendChild(MakeButton("Remove", () => {
		matchContainer.remove();
	}));
	if (prevMaxHeight && prevMaxHeight != "0px") {
		body.style.maxHeight = body.scrollHeight + "px";
	}
}

function RenderCurrentMatchInfo(data) {
	const container = document.querySelector("#curMatchBody");
	if (!data || !data.State || data.State == 0) {  // No current match
		container.textContent = "There is no current match.";
		container.removeAttribute("h2h_match_source");
		return;
	}
	container.setAttribute("h2h_match_source", data.InternalID);
	container.textContent = "";
	// Title
	let title = document.createElement("h5");
	title.textContent = `${data.DisplayName} - ${data.StateTitle}`;
	const categoryIcon = GetH2HImageElement(data.CategoryIcon);
	categoryIcon.className = "categoryIcon";
	title.prepend(categoryIcon);
	container.appendChild(title);
	RenderMatchActionButtons(data, container);
	RenderMatchInfoToContainer(data, container);
}

/**
 * 
 * @param {*} data 
 * @returns 
 */
function RenderOtherMatchInfo(data) {
	if (!data || !data.State || data.State == 0) {
		return;
	}
	let matchContainer = document.querySelector(`#allMatchesBody #matchContainer_${data.InternalID}`);
	if (matchContainer) {
	}
	else {
		matchContainer = document.createElement("div");
		matchContainer.setAttribute("id", `matchContainer_${data.InternalID}`);
		matchContainer.className = "collapseContainer";
		document.querySelector("#allMatchesBody").appendChild(matchContainer);
	}
	const prevMaxHeight = matchContainer.querySelector(".collapsibleContent")?.style?.maxHeight;
	matchContainer.textContent = "";
	// Collapse header (icon and title)
	const collapseHeader = document.createElement("button");
	collapseHeader.setAttribute("type", "button");
	collapseHeader.className = "collapsibleHeader";
	// Title, icon, buttons
	collapseHeader.textContent = `${data.DisplayName} - ${data.StateTitle}`;
	const categoryIcon = GetH2HImageElement(data.CategoryIcon);
	categoryIcon.className = "categoryIcon";
	collapseHeader.prepend(categoryIcon);
	// Click
	collapseHeader.addEventListener("click", function () {
		collapseHeader.classList.toggle("collapsibleActive");
		var content = collapseHeader.nextElementSibling;
		if (content.style.maxHeight && content.style.maxHeight != "0px") {
			content.style.maxHeight = "0px";
		} else {
			content.style.maxHeight = content.scrollHeight + "px";
		}
	});
	matchContainer.appendChild(collapseHeader);
	// Collapse body (the rest)
	const collapseBody = document.createElement("div");
	RenderMatchActionButtons(data, collapseBody);
	RenderMatchInfoToContainer(data, collapseBody);
	matchContainer.appendChild(collapseBody);
	if (prevMaxHeight && prevMaxHeight != "0px") {
		collapseBody.style.maxHeight = collapseBody.scrollHeight + "px";
	}
	collapseBody.className = "collapsibleContent";
}

/**
 * 
 * @param {*} data 
 * @param {*} container 
 */
function RenderMatchInfoToContainer(data, container) {
	// Players
	title = document.createElement("h5");
	title.textContent = "Players";
	container.appendChild(title);
	const table = document.createElement("table");
	table.className = "playerTable";
	if (!(data.Players.length)) {
		const row = document.createElement("tr");
		const playerNameTd = document.createElement("td");
		playerNameTd.textContent = "Nobody has joined this match";
		row.appendChild(playerNameTd);
		table.appendChild(row);
	}
	else {
		data.Players.sort((a, b) => {
			if (a.Status != b.Status) {
				const order = [30, 20, 999, 10, 0];  // Completed, In Match, DNF, Joined, Not Joined
				return order.indexOf(a.Status ?? 0) < order.indexOf(b.Status ?? 0) ? -1 : 1;
			}
			if (a.Status == 30 && b.Status == 30) {
				return a.Timer > b.Timer ? 1 : -1;
			}
			return 0;
		});
		for (const player of data.Players) {
			RenderMatchPlayerRow(player, table, data);
		}
	}
	container.appendChild(table);
}

function RenderMatchPlayerRow(player, table, match) {
	const row = document.createElement("tr");
	// Name
	const playerNameTd = document.createElement("td");
	playerNameTd.className = "playerName";
	playerNameTd.textContent = player.DisplayName;
	row.appendChild(playerNameTd);
	// full ID
	if (isDebugMode) {
		const playerIdTd = document.createElement("td");
		playerIdTd.textContent = player.Id;
		row.appendChild(playerIdTd);
	}
	// Status
	const playerStatusTd = document.createElement("td");
	playerStatusTd.className = `playerStatus ${player.StatusTitle}`;
	playerStatusTd.textContent = player.StatusTitle;
	row.appendChild(playerStatusTd);
	// Timer
	const playerTimerTd = document.createElement("td");
	playerTimerTd.className = `playerTimer`;
	playerTimerTd.textContent = player.FormattedTimer;
	row.appendChild(playerTimerTd);
	// Objectives
	for (const phase of player.Phases) {
		for (const objective of phase.Objectives) {
			// Icon
			const playerNameTd = document.createElement("td");
			playerNameTd.className = "objectiveTd";
			const icon = GetH2HImageElement(objective.Icon);
			icon.className = "objectiveIcon";
			playerNameTd.appendChild(icon);
			// Objective text
			let objContent = "";
			if (objective.Completed) {
				objContent += "✔";
			}
			else {
				objContent += "✖";
			}
			if (objective.CollectablesGoal > 0) {
				objContent += ` ${objective.CollectablesObtained} / ${objective.CollectablesGoal}`;
			}
			else if (objective.ObjectiveType == 10) {  // Time Limit
				objContent += ` ${objective.TimeRemaining} / ${objective.TimeLimit}`
			}
			const objectiveContent = document.createElement("span");
			objectiveContent.textContent = objContent;
			playerNameTd.appendChild(objectiveContent);
			row.appendChild(playerNameTd);
		}
	}
	// Actions
	const actionsTd = document.createElement("td");
	if (isDebugMode || player.Actions.includes("GET_OTHER_MATCH_LOG")) {
		const btn = MakeButton("Get Log", () => {
			socket.Send("GET_OTHER_MATCH_LOG", {
				matchID: match.InternalID,
				playerID: player.Id,
			});
		});
		btn.classList.add("keepAfterForgotten");
		actionsTd.appendChild(btn);
	}
	row.appendChild(actionsTd);
	// Misc
	table.appendChild(row);
}

/**
 * 
 * @param {*} label 
 * @param {*} onClick 
 * @returns 
 */
function MakeButton(label, onClick) {
	const btn = document.createElement("button");
	btn.setAttribute("type", "button");
	btn.className = "actionBtn";
	btn.textContent = label;
	btn.addEventListener("click", onClick);
	return btn;
}

/**
 * 
 * @param {*} actions 
 * @param {*} tr 
 */
function RenderMatchActionButtons(match, container) {
	const containerType = "div";
	const elemType = "span";

	const buttonsContainer = document.createElement(containerType);
	buttonsContainer.className = "actionButtonsContainer";

	if (match.AvailableActions.includes("STAGE_MATCH")) {
		const cell = document.createElement(elemType);
		cell.appendChild(MakeButton("Stage", () => {
			socket.Send("STAGE_MATCH", match.InternalID);
		}));
		buttonsContainer.appendChild(cell);
	}

	if (match.AvailableActions.includes("JOIN_MATCH")) {
		const cell = document.createElement(elemType);
		cell.appendChild(MakeButton("Join", () => {
			socket.Send("JOIN_MATCH", match.InternalID);
		}));
		buttonsContainer.appendChild(cell);
	}

	if (match.AvailableActions.includes("START_MATCH")) {
		const cell = document.createElement(elemType);
		cell.appendChild(MakeButton("Start", () => {
			socket.Send("START_MATCH", match.InternalID);
		}));
		buttonsContainer.appendChild(cell);
	}

	if (match.AvailableActions.includes("GET_MY_MATCH_LOG")) {
		const cell = document.createElement(elemType);
		cell.classList.add("keepAfterForgotten");
		cell.appendChild(MakeButton("Get Log", () => {
			socket.Send("GET_MY_MATCH_LOG", match.InternalID);
		}));
		buttonsContainer.appendChild(cell);
	}

	if (match.AvailableActions.includes("MATCH_DROP_OUT")) {
		const cell = document.createElement(elemType);
		cell.appendChild(MakeButton("Drop Out", () => {
			socket.Send("MATCH_DROP_OUT", match.InternalID);
		}));
		buttonsContainer.appendChild(cell);
	}

	if (match.AvailableActions.includes("KILL_MATCH")) {
		const cell = document.createElement(elemType);
		cell.appendChild(MakeButton("End Match", () => {
			if (confirm("The match will be ended for all players. This is a disruptive action. Continue?") == true) {
				socket.Send("KILL_MATCH", match.InternalID);
			}
		}));
		buttonsContainer.appendChild(cell);
	}

	if (match.AvailableActions.includes("FORGET_MATCH")) {
		const cell = document.createElement(elemType);
		cell.appendChild(MakeButton("Forget", () => {
			socket.Send("FORGET_MATCH", match.InternalID);
		}));
		buttonsContainer.appendChild(cell);
	}

	container.appendChild(buttonsContainer);
}

function RenderLog(log) {
	const container = document.querySelector("#logCardBody");
	if (!log) {  // No current log
		container.textContent = "There is no match log loaded.";
		document.querySelector("#logCard").classList.remove("hasLog");
		return;
	}
	container.textContent = "";

	const mainSec = document.createElement("div");
	let p = document.createElement("div");
	p.textContent += `${log.MatchDispName}`;
	mainSec.appendChild(p);
	p = document.createElement("div");
	p.textContent += `ID: ${log.MatchID}`;
	mainSec.appendChild(p);
	p = document.createElement("div");
	p.textContent += `Date: ${log.MatchBeginDate}`;
	mainSec.appendChild(p);
	p = document.createElement("div");
	p.textContent += `Match Creator: ${log.MatchCreator}`;
	mainSec.appendChild(p);
	p = document.createElement("div");
	p.textContent += `Runner: ${log.RunnerID?.DisplayName}`;
	mainSec.appendChild(p);
	container.appendChild(mainSec);
	
	let table = document.createElement("table");
	table.className = "logTable";
	RenderLogRow(table, {
		Instant: "Instant",
		MatchTimer: "Match Timer",
		FileTimer: "File Timer",
		Label: "Event",
		Room: "Room",
		Checkpoint: "Checkpoint",
		AreaSID: "Area",
		LevelExitMode: "Exit Mode",
		SaveDataIndex: "Save Data Slot",
		MatchID: "Match ID",
	});
	for (const evt of log.Events) {
		RenderLogRow(table, evt);
	}
	container.appendChild(table);
	document.querySelector("#logCard").classList.add("hasLog");
}

function RenderLogRow(table, evt) {
	const template = document.querySelector("#LogRowTemplate");
	const newRow = template.content.cloneNode(true);
	for (const key in evt) {
		const td = newRow.querySelector(`[field|="${key}"]`);
		if (td) {
			td.textContent = evt[key];
		}
	}
	table.appendChild(newRow);
}
