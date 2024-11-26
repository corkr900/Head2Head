

HandleParams();


let socket = new H2HSocket();
socket.OnOpen = OnConnected;
socket.OnClose = OnDisconnected;
socket.OnMessage = OnMessage;

function OnConnected(e) {
	const statusSpan = document.querySelector("#rend_status");
	statusSpan.textContent = "Connected";
}

function OnDisconnected(e) {
	const statusSpan = document.querySelector("#rend_status");
	statusSpan.textContent = "Not Connected. Is the game running, Head 2 Head enabled, and Control Panel enabled in Head 2 Head mod settings?";
}

function OnMessage(data) {

}






function HandleParams() {
	const queryString = window.location.search;
	const urlParams = new URLSearchParams(queryString);

	isDebugMode = urlParams.get('debug');
	if (!isDebugMode) {
		for (const elem of document.querySelectorAll(".debugOnly")) {
			elem.remove();
		}
	}
}

