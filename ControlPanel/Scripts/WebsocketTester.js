// http://www.websocket.org/echo.html
const button = document.querySelector("button");
const output = document.querySelector("#output");
const textarea = document.querySelector("textarea");
const wsUri = "ws://127.0.0.1:8080/";
const websocket = new WebSocket(wsUri);

button.addEventListener("click", onClickButton);

websocket.onopen = (e) => {
	writeToScreen("CONNECTED");
	doSend("UNREGISTERED|NOTIFY_CONNECT|Hello World!");
};

websocket.onclose = (e) => {
	writeToScreen("DISCONNECTED");
};

websocket.onmessage = (e) => {
	writeToScreen(`<span>RESPONSE: ${e.data}</span>`);
};

websocket.onerror = (e) => {
	writeToScreen(`<span class="error">ERROR:</span> ${JSON.stringify(data, null, 4)}`);
};

function doSend(message) {
	writeToScreen(`SENT: ${message}`);
	websocket.send(message);
}

function writeToScreen(message) {
	output.insertAdjacentHTML("afterbegin", `<p>${message}</p>`);
}

function onClickButton() {
	const text = textarea.value;

	text && doSend("UNREGISTERED|TEST_INCOMING|" + text);
	textarea.value = "";
	textarea.focus();
}