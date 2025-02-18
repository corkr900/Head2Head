

function __H2HSocketMkDelegate(obj, func) {
	return () => {
		func.call(obj);
	}
}

class H2HSocket {
	_socketIP = "127.0.0.1";
	_websocket = {};
	_isConnected = false;
	_retryConnectionImmediately = false;
	_reqIDCounter = 1;
	_cpVersion = -1;
	_clientToken = "TOKEN_NOT_PROVISIONED";
	_readyEventSent = false;
	_randoAvailable = false;
	_subscriptions = null;

	constructor(targetIP) {
		if (targetIP) this._socketIP = targetIP;
		this.TryConnect();
	}

	// Expect these to be changed out for something else
	OnMessage(msg) { }
	OnOpen(e) { }
	OnClose(e) { }
	OnError(err) { }
	OnReady() { }

	NewRequestID() {
		return (++this._reqIDCounter).toString();
	}

	Send(command, data) {
		if (!this._isConnected) return;
		const message = JSON.stringify({
			Command: command,
			Token: this._clientToken,
			RequestID: this.NewRequestID(),
			Data: data ?? {}
		});
		this._websocket.send(message);
	}

	// Internals
	TryConnect() {
		this._websocket = new WebSocket(this.GetConnectionUri());
		this._websocket.onopen = (e) => {
			this._isConnected = true;
			this._readyEventSent = false;
			this.OnOpen(e);
		};
		this._websocket.onclose = (e) => {
			this._isConnected = false;
			this._readyEventSent = false;
			this.OnClose(e);
			if (this._retryConnectionImmediately) {
				this._retryConnectionImmediately = false;
				this.TryConnect();
			}
			else {
				setTimeout(__H2HSocketMkDelegate(this, this.TryConnect), 500);
			}
		};
		this._websocket.onmessage = (e) => {
			const data = JSON.parse(e.data);
			if (data.Command == "WELCOME") {
				this._clientToken = data.Data?.Token;
				this._cpVersion = data.Data?.Version;
				this._randoAvailable = data.Data?.RandomizerInstalled;

				if (!this._readyEventSent) {
					this.OnReady();
					this._readyEventSent = true;
				}
				if (this._subscriptions) {
					this.Send("subscriptions", this._subscriptions);
				}
			}
			else {
				this.OnMessage(data);
			}
		};
		this._websocket.onerror = (e) => {
			this.OnError(e);
		};
	}

	Subscriptions(add, remove) {
		this._subscriptions = {
			"add": add,
			"remove": remove,
		};
		if (this.IsReady()) {
			this.Send("subscriptions", this._subscriptions);
		}
	}

	ChangeConnection(ipToConnect) {
		this._socketIP = ipToConnect;
		this._retryConnectionImmediately = true;
		this._websocket.close();
	}

	GetConnectionUri() {
		return `ws://${this._socketIP}:8080/`;
	}

	IsConnected() {
		return this._isConnected;
	}

	IsReady(requireVersion) {
		return this._isConnected
			&& this._clientToken !== "TOKEN_NOT_PROVISIONED"
			&& (!requireVersion || this._cpVersion >= requireVersion);
	}

	Version() {
		return this._cpVersion;
	}

	IsRandoAvailable() {
		return this._randoAvailable;
	}

}
