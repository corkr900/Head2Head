

function __H2HSocketMkDelegate(obj, func) {
	return () => {
		func.call(obj);
	}
}

class H2HSocket {
	_socketIP = "127.0.0.1";
	_websocket = {};
	_clientToken = "TOKEN_NOT_PROVISIONED";
	_isConnected = false;

	constructor() {
		this.TryConnect();
	}

	// Expect these to be changed out for something else
	OnMessage(msg) { }
	OnOpen(e) { }
	OnClose(e) { }
	OnError(err) { }

	Send(command, data) {
		if (!this._isConnected) return;
		const message = JSON.stringify({
			Command: command,
			Token: this._clientToken,
			Data: data ?? {}
		});
		_websocket.send(message);
	}

	// Internals
	TryConnect() {
		this._websocket = new WebSocket(this.GetConnectionUri());
		this._websocket.onopen = (e) => {
			this._isConnected = true;
			this.OnOpen(e);
		};
		this._websocket.onclose = (e) => {
			this._isConnected = false;
			this.OnClose(e);
			if (this.immediateTryReconnect) {
				this.immediateTryReconnect = false;
				this.TryConnect();
			}
			else {
				setTimeout(__H2HSocketMkDelegate(this, this.TryConnect), 500);
			}
		};
		this._websocket.onmessage = (e) => {
			const data = JSON.parse(e.data);
			if (data.Command == "ALLOCATE_TOKEN") {
				this._clientToken = data.Data;
			}
			else {
				this.OnMessage(data);
			}
		};
		this._websocket.onerror = (e) => {
			this.OnError(e);
		};
	}

	GetConnectionUri() {
		return `ws://${this._socketIP}:8080/`;
	}

	IsConnected() {
		return this._isConnected;
	}
}
