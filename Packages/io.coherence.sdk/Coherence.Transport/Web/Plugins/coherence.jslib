// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

var CoherenceWeb = {
    $deps: {
        connections: {},
        runningId: 0,
    },

    $connection: function (id, onConnect, onPacket, onError) {
        /**
         * @readonly
         * @enum {string}
         */
        var errorType = {
            OFFER: "offerError",
            CHANNEL: "channelError",
        };

        /**
         * @constructor
         * @param {errorType} errorType
         * @param {int} statusCode
         * @param {string} errorMessage
         * @param {object} errorResponse
         */
        var JsError = function (errorType, statusCode, errorMessage, errorResponse) {
            return {
                errorType: errorType,
                statusCode: statusCode,
                errorMessage: errorMessage,
                errorResponse: errorResponse
            }
        };

        var obj = {
            peer: null,

            url: "",
            token: "",
            region: "",
            schemaId: "",
            uniqueRoomId: "",
            worldId: "",
            roomId: 0,
            id: id,
            sessionId: -1,
            onConnect: onConnect,
            onPacket: onPacket,
            onError: onError,

            readyForIce: false,
            pendingIce: [],
            iceServers: [],

            dataChannel: null,

            handleBuffer: function (buffer) {
                var ptr = _malloc(buffer.byteLength);
                var ub = new Uint8Array(buffer);
                var dataHeap = new Uint8Array(HEAPU8.buffer, ptr, buffer.byteLength);
                dataHeap.set(ub);
                Module["dynCall_viii"](obj.onPacket, obj.id, buffer.byteLength, ptr);
                _free(ptr)
            },
            handleBufferEx: function (ex) {
                console.error("[WRTC] Buffer exception", ex);
            },
            handleReceiveMessage: function (event) {
                var data = event.data;
                if (data instanceof ArrayBuffer) {
                    obj.handleBuffer(data);
                }
                else {
                    data.arrayBuffer().then(obj.handleBuffer).catch(obj.handleBufferEx);
                }
            },

            sendError: function (error) {
                var errorMsg = error ? error : JsError(null, 0, "Unknown Error");
                console.error("[WRTC] Error", errorMsg);
                var bufferMsg = JSON.stringify(errorMsg);
                var bufferSize = lengthBytesUTF8(bufferMsg) + 1;
                var buffer = _malloc(bufferSize);
                stringToUTF8(bufferMsg, buffer, bufferSize);
                Module["dynCall_vii"](obj.onError, obj.id, buffer);
            },

            handleOpen: function (event) {
                console.debug("[WRTC] Data channel open");
                Module["dynCall_vi"](obj.onConnect, obj.id);
            },

            handleClose: function (event) {
                console.debug("[WRTC] Data channel closed");
                obj.sendError(JsError(errorType.CHANNEL, 0, "Channel closed"));
                obj.reset();
            },

            sendData: function (data, length) {
                if (obj.dataChannel) {
                    var bytes = new Uint8Array(length);
                    for (var i = 0; i < length; i++) {
                        bytes[i] = HEAPU8[data + i];
                    }

                    if (bytes.byteLength > 0) {
                        obj.dataChannel.send(bytes);
                    }
                }
            },

            getNumberBytes: function (num) {
                var buffer1 = new ArrayBuffer(2);
                var view = new DataView(buffer1)
                view.setUint16(0, num, false);
                return new Uint8Array(buffer1);
            },

            sendIceCandidate: function (candidate) {
                if (obj.url === "") {
                    console.warn("[WRTC] Signalling url not set, cannot send ice candidate");
                    return;
                }

                var iceMsg = { ice: candidate, roomId: obj.roomId, id: obj.sessionId };
                var fetchUrl = obj.getPath(obj.url, 'webrtc/ice');

                console.debug("[WRTC] Sending ICE candidate", candidate);

                fetch(fetchUrl, {
                    method: 'post',
                    headers: {
                        'Accept': 'application/json, text/plain, */*',
                        'Content-Type': 'application/json',
                        'X-Coherence-Runtime-Key': obj.token,
                        'X-Coherence-Unique-Room-ID': obj.uniqueRoomId,
                        'X-Coherence-World-ID': obj.worldId,
                        'X-Coherence-Region': obj.region
                    },
                    body: JSON.stringify(iceMsg)
                })
                    .then(function (res) {
                        return res.json()
                    })
                    .then(function (res) {
                        if (res.data) {
                            res.data.forEach(function (ice) {
                                obj.peer.addIceCandidate(ice);
                            });
                        }
                    })
                    .catch(function(ex) {
                        console.error("[WRTC] ICE candidate request error", ex);
                    });
            },

            sendPendingIce: function () {
                console.debug("[WRTC] Sending pending ICE");
                obj.pendingIce.forEach(function (candidate) {
                    obj.sendIceCandidate(candidate);
                });
                obj.pendingIce = [];
            },

            handleICECandidateEvent: function (evt) {
                console.debug("[WRTC] ICE candidate event", evt);
                const candidate = evt.candidate;
                if (!candidate) {
                    // null ice candidate means end of ice candidate search.
                    return;
                }

                if (obj.readyForIce) {
                    obj.sendIceCandidate(candidate);
                }
                else {
                    obj.pendingIce.push(candidate);
                }
            },

            handleICEConnectionStateChangeEvent: function (evt) {
                if (obj.peer != null) {
                    console.debug("[WRTC] ICE state change", obj.peer.iceConnectionState);
                    if (obj.peer.iceConnectionState === "disconnected") {
                        obj.sendError(JsError(errorType.CHANNEL, 0, "ICE disconnected"));
                        obj.reset();
                    }
                }
            },

            disconnect: function () {
                console.debug("[WRTC] Peer disconnect", obj.peer);
                if (obj.peer) {
                    obj.peer.close();
                    obj.reset();
                }
            },

            reset: function () {
                obj.peer = null;
                obj.readyForIce = false;
                obj.pendingIce = [];
                obj.dataChannel = null;
            },

            handleError: function (event) {
                obj.sendError(JsError(errorType.CHANNEL, 0, event.message));
            },

            getPath: function (host, path) {
                if (!/^https?:\/\//i.test(host)) {
                    host = 'http://' + host;
                }
                var url = new URL(host);
                var paths = url.pathname.substring(1).split('/');
                paths.push(path);
                url.pathname = paths.join('/');
                return url.toString();
            },

            createOffer: function (url, token, uniqueRoomId, worldId, region) {
                const peer = obj.peer;
                peer.createOffer()
                    .then(function (offer) {
                        console.debug("[WRTC] Sending offer", offer);
                        peer.setLocalDescription(offer);
                        var offerMsg = {
                            offer: offer,
                            roomId: obj.roomId,
                            roomUid: obj.uniqueRoomId,
                            schemaId: obj.schemaId
                        };
                        var fetchUrl = obj.getPath(url, 'webrtc/offer');
                        return fetch(fetchUrl.toString(), {
                            method: 'post',
                            headers: {
                                'Accept': 'application/json, text/plain, */*',
                                'Content-Type': 'application/json',
                                'X-Coherence-Runtime-Key': token,
                                'X-Coherence-Region': region,
                                'X-Coherence-Unique-Room-ID': uniqueRoomId,
                                'X-Coherence-World-ID': worldId
                            },
                            body: JSON.stringify(offerMsg)
                        });
                    })
                    .then(function (res) {
                        console.debug("[WRTC] Offer response", res.status);
                        if (res.ok) {
                            return res.json();
                        }

                        return res.text()
                            .then(function (body) {
                                var errorResponse = null;
                                try {
                                    errorResponse = JSON.parse(body);
                                } catch (ex) {
                                    // Response is raw text
                                    throw JsError(errorType.OFFER, res.status, body);
                                }

                                // Response is a JSON
                                throw JsError(errorType.OFFER, res.status, null, errorResponse);
                            }, function (ex) {
                                // Failed to read body
                                throw JsError(errorType.OFFER, res.status);
                            });
                    })
                    .then(function (res) {
                        obj.sessionId = res.id;
                        peer.setRemoteDescription(res);
                        obj.readyForIce = true;
                        obj.sendPendingIce();
                    })
                    .catch(function (ex) {
                        try {
                            if (ex.errorType == errorType.OFFER) {
                                // Offer error
                                obj.sendError(ex);
                            } else {
                                // Unknown error
                                var error = JsError(errorType.OFFER, 0, JSON.stringify(ex));
                                obj.sendError(error);
                            }
                        } finally {
                            obj.disconnect();
                        }
                    });
            },

            fetchIceServers: function() {
                var now = Date.now();
                if (window.iceServers && window.iceServersExpireTs > now) {
                    console.debug("[WRTC] ICE servers already fetched");
                    obj.iceServers = window.iceServers;
                    return Promise.resolve();
                }

                console.debug("[WRTC] fetching ICE servers");
                var fetchUrl = obj.getPath(obj.url, 'webrtc/ice');
                return fetch(fetchUrl, {
                    method: 'get',
                    headers: {
                        'Accept': 'application/json, text/plain, */*',
                        'Content-Type': 'application/json',
                        'X-Coherence-Runtime-Key': obj.token,
                        'X-Coherence-Region': obj.region
                    },
                })
                    .then(function (res) {
                        return res.json()
                    })
                    .then(function (res) {
                        console.debug("[WRTC] ICE servers response =" + JSON.stringify(res))
                        obj.iceServers = res.ice_servers
                        window.iceServers = res.ice_servers;
                        window.iceServersExpireTs = now + 1 * 60 * 60 * 1000; // refetch after 1h
                    })
                    .catch(function(ex) {
                        console.error("[WRTC] fetch ICE servers request error", ex);
                    });

            },

            connect: function (url, roomId, token, uniqueRoomId, worldId, region, schemaId) {
                if (obj.peer != null) {
                    console.warn("[WRTC] Connection exists, existing early");
                    return;
                }

                obj.reset();

                obj.fetchIceServers().then(function() {
                    obj.url = url;
                    obj.roomId = roomId;
                    obj.schemaId = schemaId;
                    var peer = new RTCPeerConnection({ 'iceServers': obj.iceServers });

                    peer.onicecandidate = obj.handleICECandidateEvent;
                    peer.oniceconnectionstatechange = obj.handleICEConnectionStateChangeEvent;

                    var receiveChannel = peer.createDataChannel('data');
                    receiveChannel.onmessage = obj.handleReceiveMessage;
                    receiveChannel.onopen = obj.handleOpen;
                    receiveChannel.onclose = obj.handleClose;
                    receiveChannel.onerror = obj.handleError;

                    obj.dataChannel = receiveChannel;
                    obj.peer = peer;

                    obj.createOffer(url, token, uniqueRoomId, worldId, region);
                });
            }
        };

        return obj;
    },

    WebInitialize: function (onConnect, onPacket, onError) {
        var id = deps.runningId;
        var conn = connection(id, onConnect, onPacket, onError);
        deps.connections[id] = conn;
        deps.runningId += 1;

        return id;
    },

    WebSend: function (id, data, length) {
        var conn = deps.connections[id];
        if (conn) {
            conn.sendData(data, length);
        }
    },

    WebConnect: function (id, url, roomId, token, uniqueRoomId, worldId, region, schemaId) {
        var conn = deps.connections[id];
        console.debug("[WRTC] Connect", "id", id, "connection", conn, "roomID", roomId,
            "roomUID", uniqueRoomId, "region", region, "schemaID", schemaId);
        if (conn) {
            conn.url = UTF8ToString(url);
            conn.region = UTF8ToString(region);
            conn.schemaId = UTF8ToString(schemaId);
            conn.roomId = roomId;
            conn.token = UTF8ToString(token);
            conn.uniqueRoomId = UTF8ToString(uniqueRoomId);
            conn.worldId = UTF8ToString(worldId);
            conn.connect(conn.url, roomId, conn.token, conn.uniqueRoomId, conn.worldId, conn.region, conn.schemaId);
        }
    },

    WebDisconnect: function (id) {
        var conn = deps.connections[id];
        console.debug("[WRTC] Disconnect", "id", id, "connection", conn);
        if (conn) {
            conn.disconnect();
        }
    }
};

autoAddDeps(CoherenceWeb, '$deps');
autoAddDeps(CoherenceWeb, '$connection');
mergeInto(LibraryManager.library, CoherenceWeb);
