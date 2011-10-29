﻿/*
 * MindTouch.Arpysee
 * 
 * Copyright (C) 2011 Arne F. Claassen
 * geekblog [at] claassen [dot] net
 * http://github.com/sdether/MindTouch.Arpysee
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Net.Sockets;
using System.Text;

namespace MindTouch.Arpysee.Server {
    public class AsyncClientHandler : AClientRequestHandler {

        private static readonly Logger.ILog _log = Logger.CreateLog();

        public AsyncClientHandler(Socket socket, ICommandDispatcher dispatcher, Action<IClientHandler> removeCallback)
            : base(socket, dispatcher, removeCallback) { }

        // 2.
        protected override void Receive(Action<int, int> continuation) {
            try {
                _bufferPosition = 0;
                _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, r => {

                    // 3.
                    try {
                        var received = _socket.EndReceive(r);
                        if(received == 0) {
                            _log.Debug("received nothing, connection must have gone away");
                            Dispose();
                            return;
                        }
                        continuation(0, received);
                    } catch(SocketException e) {
                        _log.Warn("EndReceive failed", e);
                        Dispose();
                    } catch(ObjectDisposedException) {
                        _log.Debug("socket was already disposed (EndReceive)");
                        return;
                    }
                }, null);
            } catch(SocketException e) {
                _log.Warn("BeginReceive failed", e);
                Dispose();
            } catch(ObjectDisposedException) {
                _log.Debug("socket was already disposed (BeginReceive)");
            }
        }

        // 13/14.
        protected override void ProcessResponse() {
            _handler.GetResponse(response => AsyncResponseHandler.SendResponse(_socket, response, e => {
                if(e != null) {
                    _log.Warn("Send failed", e);
                    Dispose();
                    return;
                }
                EndCommandRequest(response.Status);
            }));
        }
    }
}