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

function OnMessage(data) {

}

// Page-specific functions!

function TODO() {

}




