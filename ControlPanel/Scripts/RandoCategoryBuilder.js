// State variables
let ready = false;

// Handle page parameters
const queryString = window.location.search;
const urlParams = new URLSearchParams(queryString);
const isDebugMode = urlParams.get('debug');
const paramIP = urlParams.get('ip');
if (!isDebugMode) {
	for (const elem of document.querySelectorAll(".debugOnly")) {
		elem.remove();
	}
}

// Set up the socket
let socket = new H2HSocket(paramIP);
socket.OnOpen = OnConnected;
socket.OnReady = OnReady;
socket.OnClose = OnDisconnected;
socket.OnMessage = OnMessage;

// Function defs

function OnConnected(e) {
	const statusSpan = document.querySelector("#rend_status");
	statusSpan.textContent = "Connected, waiting for version info...";
	setTimeout(OnReady, 400);
}

function OnDisconnected(e) {
	ready = false;
	const statusSpan = document.querySelector("#rend_status");
	statusSpan.textContent = "Not Connected. Is the game running, Head 2 Head enabled, and Control Panel enabled in Head 2 Head mod settings?";
}

function OnReady() {
	const statusSpan = document.querySelector("#rend_status");
	if (socket.Version() >= 2) {
		ready = true;
		statusSpan.textContent = "Connected and ready!";
	}
	else {
		ready = false;
		statusSpan.textContent = "Incompatible Head 2 Head version. Update Head 2 Head to use this page.";
	}
}

function OnMessage(msg) {
	if (msg.Command == "RESULT") {
		OnCommandResult(msg.Data);
	}
}

// Page-specific functions!

function GetRadioSelection(name) {
	return document.querySelector(`input[name="${name}"]:checked`)?.value;
}

function AddSelectionToPayload(radioName, payload, propName) {
	const val = GetRadioSelection(radioName);
	if (!val) {
		return false;
	}
	payload[propName] = val;
	return true;
}

function StageCustomRando() {
	const payload = BuildCategoryDef();
	if (payload) {
		socket.Send("stage_custom_rando", payload);
		SetResultText("Message sent to Head 2 Head");
		return;
	}
	else {
		SetResultText("Could not send: missing required information");
	}
}

function BuildCategoryDef() {
	const payload = {
		name: document.getElementById("categoryNameEntry").value ?? "Custom Match",
	};
	if (!AddSelectionToPayload("radio_darkness", payload, "darkness")) return null;
	if (!AddSelectionToPayload("radio_difficulty", payload, "difficulty")) return null;
	if (!AddSelectionToPayload("radio_eagerness", payload, "difficultyEagerness")) return null;
	if (!AddSelectionToPayload("radio_logictype", payload, "logicType")) return null;
	if (!AddSelectionToPayload("radio_length", payload, "mapLength")) return null;
	if (!AddSelectionToPayload("radio_dashes", payload, "numDashes")) return null;
	//if (!AddSelectionToPayload("", payload, "seedType")) return null;
	if (!AddSelectionToPayload("radio_shine", payload, "shineLights")) return null;
	if (!AddSelectionToPayload("radio_berries", payload, "strawberryDensity")) return null;
	if (!AddSelectionToPayload("radio_seed", payload, "seedType")) return null;
	payload.seed = document.getElementById("seedEntry").value ?? "";
	return payload;
}

function SaveCustomRando() {
	const payload = BuildCategoryDef();
	if (payload) {
		socket.Send("save_custom_rando", payload);
		SetResultText("Message sent to Head 2 Head");
		return;
	}
	else {
		SetResultText("Could not send: missing required information");
	}
}

function SetResultText(txt) {
	document.querySelector("#resultDisplay").textContent = txt;
}

function OnCommandResult(data) {
	SetResultText((data.Result ? "Success! " : "Failed. ") + data.Info);
}


