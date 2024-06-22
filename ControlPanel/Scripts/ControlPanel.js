// http://www.websocket.org/echo.html
const wsUri = "ws://127.0.0.1:8080/";
const placeholderImage = "https://github.com/corkr900/Head2Head/blob/main/Graphics/Atlases/Gui/Head2Head/Categories/Custom.png?raw=true";
let websocket = {};
let clientToken = "TOKEN_NOT_PROVISIONED";
let imageCache = {};

HandleParams();
TryConnect();

function HandleParams() {
	const queryString = window.location.search;
	const urlParams = new URLSearchParams(queryString);
	const debug = urlParams.get('debug');
	if (debug) {
		for (const elem of document.querySelectorAll(".debugOnly")) {
			elem.classList.remove("debugOnly");
		}
	}
}

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
		writeToScreen(`RECEIVED: ${e.data}`);
		HandleMessage(JSON.parse(e.data));
	};
	websocket.onerror = (e) => {
		writeToScreen(`<span class="error">ERROR:</span> ${JSON.stringify(e, null, 4)}`);
	};
}

function HandleMessage(data) {
	writeToScreen(`Command triggered: ${data.Command}`);
	if (data.Command == "ALLOCATE_TOKEN") {
		clientToken = data.Data;
	}
	else if (data.Command == "CURRENT_MATCH") {
		RenderCurrentMatchInfo(data.Data);
		RenderOtherMatchInfo(data.Data);
	}
	else if (data.Command == "IMAGE") {
		imageCache[data.Data.Id] = data.Data.ImgSrc;
		for (const elem of document.querySelectorAll(`[h2h_img_src|="${data.Data.Id}"]`)) {
			elem.setAttribute("src", imageCache[data.Data.Id]);
		}
	}
}

function doSend(command, data) {
	const message = JSON.stringify({
		Command: command,
		Token: clientToken,
		Data: data
	});
	writeToScreen(`SENT: ${message}`);
	websocket.send(message);
}

function writeToScreen(message) {
	const output = document.querySelector("#output");
	if (output) {
		output.insertAdjacentHTML("afterbegin", `<p>[${new Date().toISOString()}] ${message}</p>`);
	}
}

function HandleConnectionUpdate(newIsConnected) {
	const statusSpan = document.querySelector("#rend_status");
	statusSpan.textContent = newIsConnected ? "Connected" : "Not Connected";
}

function GetH2HImageElement(image) {
	var src = image.ImgSrc;
	if (!src) {
		if (imageCache[image.Id]) src = imageCache[image.Id];
		else {
			// TODO this can send duplicates if multiple of the same image comes in on the same request
			doSend("REQUEST_IMAGE", image.Id);
			src = placeholderImage;
		}
	}
	const categoryIcon = document.createElement("img");
	categoryIcon.setAttribute("h2h_img_src", image.Id);
	categoryIcon.setAttribute("src", src);
	return categoryIcon
}

function RenderCurrentMatchInfo(data) {
	const container = document.querySelector("#curMatchBody");
	if (!data || !data.State || data.State == 0) {  // No current match
		container.textContent = "There is no current match.";
		return;
	}
	container.textContent = "";
	// Title
	let title = document.createElement("h5");
	title.textContent = `${data.DisplayName} - ${data.StateTitle}`;
	const categoryIcon = GetH2HImageElement(data.CategoryIcon);
	categoryIcon.className = "categoryIcon";
	title.prepend(categoryIcon);
	container.appendChild(title);
	RenderMatchInfoToContainer(data, container);
}

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
	collapseHeader.textContent = `${data.DisplayName} - ${data.StateTitle}`;
	const categoryIcon = GetH2HImageElement(data.CategoryIcon);
	categoryIcon.className = "categoryIcon";
	collapseHeader.prepend(categoryIcon);
	collapseHeader.addEventListener("click", function () {
		this.classList.toggle("collapsibleActive");
		var content = this.nextElementSibling;
		if (content.style.maxHeight) {
			content.style.maxHeight = null;
		} else {
			content.style.maxHeight = content.scrollHeight + "px";
		}
	});
	matchContainer.appendChild(collapseHeader);
	// Collapse body (the rest)
	const collapseBody = document.createElement("div");
	collapseBody.className = "collapsibleContent";
	collapseBody.style.display = prevMaxHeight;
	RenderMatchInfoToContainer(data, collapseBody);
	matchContainer.appendChild(collapseBody);
}

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
