// http://www.websocket.org/echo.html
const wsUri = "ws://127.0.0.1:8080/";
let websocket = {};
let clientToken = "TOKEN_NOT_PROVISIONED";

TryConnect();

function TryConnect() {
	websocket = new WebSocket(wsUri);
	websocket.onopen = (e) => {
		HandleConnectionUpdate(true);
	};
	websocket.onclose = (e) => {
		HandleConnectionUpdate(false);
		setTimeout(TryConnect, 500);
	};
	websocket.onmessage = (e) => {
		//writeToScreen(`RECEIVED: ${e.data}`);
		HandleMessage(JSON.parse(e.data));
	};
	websocket.onerror = (e) => {
		writeToScreen(`<span class="error">ERROR:</span> ${JSON.stringify(data, null, 4)}`);
	};
}

function HandleMessage(data) {
	writeToScreen(`Command triggered: ${data.Command}`);
	if (data.Command == "ALLOCATE_TOKEN") {
		clientToken = data.Data;
	}
	else if (data.Command == "CURRENT_MATCH") {
		RenderCurrentMatchInfo(data.Data);
	}
}

function doSend(message) {
	writeToScreen(`SENT: ${message}`);
	websocket.send(message);
}
function writeToScreen(message) {
	const output = document.querySelector("#output");
	output.insertAdjacentHTML("afterbegin", `<p>[${new Date().toISOString()}] ${message}</p>`);
}

function HandleConnectionUpdate(newIsConnected) {
	const statusSpan = document.querySelector("#rend_status");
	statusSpan.textContent = newIsConnected ? "Connected" : "Not Connected";
}

function RenderCurrentMatchInfo(data) {
	const container = document.querySelector("#curMatchBody");
	if (!data || !data.State || data.State == 0) {  // No current match
		container.textContent = "There is no current match.";
		return;
	}
	container.textContent = "";

	// Title
	let title = document.createElement("h4");
	title.textContent = `${data.DisplayName} - ${data.StateTitle}`;
	container.appendChild(title);
	// Players
	title = document.createElement("h4");
	title.textContent = "Players";
	container.appendChild(title);
	const table = document.createElement("table");
	table.className = "playerTable";
	for (const player of data.Players) {
		const row = document.createElement("tr");
		// Name
		const playerNameTd = document.createElement("td");
		playerNameTd.className = "playerName";
		playerNameTd.textContent = player.DisplayName;
		row.appendChild(playerNameTd);
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
				const icon = document.createElement("img");
				icon.setAttribute("src", objective.Icon);
				icon.setAttribute("alt", objective.DisplayName);
				icon.className = "objectiveIcon";
				playerNameTd.appendChild(icon);
				// Objective text
				let objContent = "";
				if (objective.CollectablesGoal > 0) {
					objContent = `${objective.CollectablesObtained} / ${objective.CollectablesGoal}`;
				}
				else if (objective.Completed) {
					objContent = "✔";
				}
				else {
					objContent = "✖";
				}
				const objectiveContent = document.createElement("span");
				objectiveContent.textContent = objContent;
				playerNameTd.appendChild(objectiveContent);
				row.appendChild(playerNameTd);
			}
		}

		table.appendChild(row);
	}
	container.appendChild(table);
}
