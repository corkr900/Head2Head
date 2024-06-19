// http://www.websocket.org/echo.html
const button = document.querySelector("button");
const output = document.querySelector("#output");
const cmdtextarea = document.querySelector("#cmdField");
const textarea = document.querySelector("#dataField");
const wsUri = "ws://127.0.0.1:8080/";
const websocket = new WebSocket(wsUri);
let myToken = "TOKEN_NOT_PROVISIONED";

button.addEventListener("click", onClickButton);

websocket.onopen = (e) => {
	writeToScreen("CONNECTED");
	doSend("TEST_COMMAND", { someInfo: "Hello World!" });
};

websocket.onclose = (e) => {
	writeToScreen("DISCONNECTED");
};

websocket.onmessage = (e) => {
	const data = JSON.parse(e.data)
	writeToScreen(`Command triggered: ${data.Command} with data: ${JSON.stringify(data.Data, null, 4)}`);
	if (data.Command == "ALLOCATE_TOKEN") {
		myToken = data.Data;
	}
};

websocket.onerror = (e) => {
	writeToScreen(`<span class="error">ERROR:</span> ${JSON.stringify(data, null, 4)}`);
};

function doSend(command, data) {
	const message = JSON.stringify({
		Command: command,
		Token: myToken,
		Data: data
	});
	writeToScreen(`SENT: ${message}`);
	websocket.send(message);
}

function writeToScreen(message) {
	output.insertAdjacentHTML("afterbegin", `<p>${message}</p>`);
}

function onClickButton() {
	let cmd = cmdtextarea.value;
	const text = textarea.value;
	if (!cmd) cmd = "TEST_INCOMING";
	doSend(cmd, text);
	textarea.focus();
}