// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

var CoherenceWebSocket = {
    $socketDeps: {
        initialized: false,
        connections: {},
    },

    $socketConn: function(id, onConnect, onDisconnect, onMessage, onError) {
        var obj = {
            ws: null,
            id: id,
            onConnect: onConnect,
            onDisconnect: onDisconnect,
            onMessage: onMessage,
            onError: onError,

            sendData: function(data) {
                obj.waitForSocketConnection(function (){
                    obj.ws.send(data)
                })
            },

            waitForSocketConnection: function(callback) {
                setTimeout(function (){
                    if (obj.ws !== null && obj.ws.readyState === 1) {
                        if (callback != null) {callback();}
                    } else {
                        // console.log('[WS] Wait for connection...');
                        obj.waitForSocketConnection(callback);
                    }
                }, 500);
            },

            disconnect: function() {
                console.debug("[WS] disconnect()")

                if (obj.ws) {
                    obj.ws.close();
                    obj.reset();
                }
            },

            reset: function() {
                console.debug("[WS] reset()")

                if (obj.ws) {
                    obj.ws.close()
                }
                obj.ws = null;
            },

            handleError: function(event) {
                var reason;
                // See https://www.rfc-editor.org/rfc/rfc6455#section-7.4.1
                if (event.code === 1000)
                    reason = "Normal closure, meaning that the purpose for which the connection was established has been fulfilled.";
                else if(event.code === 1001)
                    reason = "An endpoint is \"going away\", such as a server going down or a browser having navigated away from a page.";
                else if(event.code === 1002)
                    reason = "An endpoint is terminating the connection due to a protocol error";
                else if(event.code === 1003)
                    reason = "An endpoint is terminating the connection because it has received a type of data it cannot accept (e.g., an endpoint that understands only text data MAY send this if it receives a binary message).";
                else if(event.code === 1004)
                    reason = "Reserved. The specific meaning might be defined in the future.";
                else if(event.code === 1005)
                    reason = "No status code was actually present.";
                else if(event.code === 1006)
                    reason = "The connection was closed abnormally, e.g., without sending or receiving a Close control frame";
                else if(event.code === 1007)
                    reason = "An endpoint is terminating the connection because it has received data within a message that was not consistent with the type of the message (e.g., non-UTF-8 [https://www.rfc-editor.org/rfc/rfc3629] data within a text message).";
                else if(event.code === 1008)
                    reason = "An endpoint is terminating the connection because it has received a message that \"violates its policy\". This reason is given either if there is no other sutible reason, or if there is a need to hide specific details about the policy.";
                else if(event.code === 1009)
                    reason = "An endpoint is terminating the connection because it has received a message that is too big for it to process.";
                else if(event.code === 1010) // Note that this status code is not used by the server, because it can fail the WebSocket handshake instead.
                    reason = "An endpoint (client) is terminating the connection because it has expected the server to negotiate one or more extension, but the server didn't return them in the response message of the WebSocket handshake. <br /> Specifically, the extensions that are needed are: " + event.reason;
                else if(event.code === 1011)
                    reason = "A server is terminating the connection because it encountered an unexpected condition that prevented it from fulfilling the request.";
                else if(event.code === 1015)
                    reason = "The connection was closed due to a failure to perform a TLS handshake (e.g., the server certificate can't be verified).";
                else
                    reason = "Unknown reason";

                obj.reset();
                console.error("[WS] Error: ", reason, "Event: ", event)

                var bufferSize = lengthBytesUTF8(reason) + 1;
                var buffer = _malloc(bufferSize);
                stringToUTF8(reason, buffer, bufferSize);
                Module["dynCall_vii"](obj.onError, id, buffer);
            },
            handleReceiveMessage: function(event) {
                var msg = event.data

                var bufferSize = lengthBytesUTF8(msg) + 1;
                var buffer = _malloc(bufferSize);
                stringToUTF8(msg, buffer, bufferSize);
                Module["dynCall_vii"](obj.onMessage, id, buffer);
            },

            handleOpen: function () {
                console.debug("[WS] Connected");
                Module["dynCall_vi"](obj.onConnect, id);
            },

            handleClose: function () {
                obj.reset();
                console.debug("[WS] Disconnected");
                Module["dynCall_vi"](obj.onDisconnect, id);
            },

            connect: function(url) {
                if (obj.ws != null) {
                    console.warn("[WS] Can't connect, connection already exists");
                    return;
                }

                obj.reset();

                const ws = new WebSocket(url);
                ws.onopen = obj.handleOpen
                ws.onclose = obj.handleClose
                ws.onerror = obj.handleError
                ws.onmessage = obj.handleReceiveMessage
                obj.ws = ws
            }
        };

        return obj;
    },

    InitSocket: function(id, onMessage, onConnect, onDisconnect, onError) {
        var conn = socketDeps.connections[id];
        
        if (!conn) {
            socketDeps.connections[id] = socketConn(id, onConnect, onDisconnect, onMessage, onError);
        }
    },

    ConnectSocket: function(id, connectionUri) {
        var conn = socketDeps.connections[id];

        if (!conn) {
            console.warn("[WS] Run Initialize first");
            return;
        }
        
        console.debug("[WS] Connecting")
        const uri = UTF8ToString(connectionUri)
        conn.connect(uri)
    },

    SendSocketMessage: function (id, data) {
        var conn = socketDeps.connections[id];
        
        if (!conn) {
            console.warn("[WS] Run Initialize first");
            return;
        }
        
        conn.sendData(UTF8ToString(data))
    },

    DisconnectSocket: function(id) {
        var conn = socketDeps.connections[id];
        
        if (!conn) {
            console.warn("[WS] Run Initialize first");
            return;
        }
        
        console.debug("[WS] Disconnecting")
        conn.reset()
    },
};

autoAddDeps(CoherenceWebSocket, '$socketDeps');
autoAddDeps(CoherenceWebSocket, '$socketConn');
mergeInto(LibraryManager.library, CoherenceWebSocket);
