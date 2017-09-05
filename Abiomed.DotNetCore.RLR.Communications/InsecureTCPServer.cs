﻿/*
 * Remote Link - Copyright 2017 ABIOMED, Inc.
 * --------------------------------------------------------
 * Description:
 * tcpserver.cs: ASYNCH TCP Server
 * --------------------------------------------------------
 * Author: Alessandro Agnello 
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Abiomed.DotNetCore.Models;
using System.Collections.Concurrent;
using Abiomed.DotNetCore.Business;

namespace Abiomed.DotNetCore.RLR.Communications
{
    public class InsecureTCPServer
    {
        private static ManualResetEvent allDone = new ManualResetEvent(false);
        private ConcurrentDictionary<string, TCPStateObjectInsecure> _tcpStateObjectList = new ConcurrentDictionary<string, TCPStateObjectInsecure>();
        private ILogManager _logManager;
        private RLMCommunication _RLMCommunication;

        public InsecureTCPServer(ILogManager logManger, RLMCommunication rLMCommunication)
        {
            _logManager = logManger;
            _RLMCommunication = rLMCommunication;
        }

        public void Run()
        {
            // Bind the socket to the local endpoint port 443 and listen for incoming connections.            
            try
            {
                var listener = new TcpListener(IPAddress.Any, 443);
                listener.Start();

                while (true)
                {
                    // Set the event to nonsignaled state.
                    allDone.Reset();

                    listener.BeginAcceptTcpClient(
                        new AsyncCallback(AcceptCallback),
                        listener);

                    // Wait until a connection is made before continuing.
                    allDone.WaitOne();
                }
            }
            catch (Exception e)
            {
                _logManager.TraceIt(Definitions.LogType.Exception, "Failed to create TCP Listener");
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            string deviceIpAddress = string.Empty;
            TCPStateObjectInsecure state = new TCPStateObjectInsecure();
            try
            {
                // Signal the main thread to continue.
                allDone.Set();

                TcpListener listener = (TcpListener)ar.AsyncState;
                TcpClient handler = listener.EndAcceptTcpClient(ar);
                deviceIpAddress = handler.Client.RemoteEndPoint.ToString(); // TODO: Set Device Serial Number?

                // TODO: add try catch with bad creds!?

                // Ensure RLM serial number is on approved list!

                // Connect to Stream and Authenticate
                var networkStream = handler.GetStream();

                // Create the state object and add to list
                state.TcpClient = handler;
                state.WorkStream = networkStream;
                state.DeviceIpAddress = deviceIpAddress;

                _tcpStateObjectList.TryAdd(state.DeviceIpAddress, state);

                _logManager.TraceIt(Definitions.LogType.Information, string.Format("RLM connected at connection {0}", state.DeviceIpAddress));
                state.WorkStream.BeginRead(state.buffer, 0, TCPStateObjectInsecure.BufferSize, new AsyncCallback(ReadCallback), state);
            }
            catch (Exception e)
            {
                TcpListener listener = (TcpListener)ar.AsyncState;
                _logManager.TraceIt(Definitions.LogType.Exception, e.Message);
            }
        }

        private void ReadCallback(IAsyncResult ar)
        {
            TCPStateObjectInsecure state = (TCPStateObjectInsecure)ar.AsyncState;

            try
            {
                // Retrieve the state object and the handler socket
                // from the asynchronous state object.
                NetworkStream handler = state.WorkStream;

                // Read data from the client socket. 
                int bytesRead = handler.EndRead(ar);

                if (bytesRead > 0)
                {
                    var receivedBuffer = state.buffer.Take(bytesRead);

                    string traceMessage = string.Format("Message received from RLM {0}, data {1}", state.DeviceIpAddress, General.ByteArrayToHexString(receivedBuffer.ToArray()));
                    _logManager.TraceIt(Definitions.LogType.Information, traceMessage);

                    // Process message
                    RLMStatus Status;

                    // Check state.DeviceId multiple messages, if so separate and process individually
                    var messages = _RLMCommunication.SeperateMessages(state.DeviceIpAddress, receivedBuffer.ToArray());

                    foreach (var message in messages)
                    {
                        byte[] returnMessage = _RLMCommunication.ProcessMessage(state.DeviceIpAddress, message, out Status);

                        // Send Message if there is something to send back
                        if (returnMessage.Length > 0)
                        {
                            //traceMessage = string.Format("Sending message to RLM {0}, data {1}", state.DeviceIpAddress, General.ByteArrayToHexString(returnMessage));
                            _logManager.TraceIt(Definitions.LogType.Information, traceMessage);

                            Send(state.DeviceIpAddress, handler, returnMessage);
                        }
                    }
                }

                // Check if still connected, Await for more data
                if (state.TcpClient.Connected)
                {
                    handler.BeginRead(state.buffer, 0, TCPStateObjectInsecure.BufferSize, new AsyncCallback(ReadCallback), state);
                }
                else
                {
                    _logManager.Log(state.DeviceIpAddress, "", state, Definitions.LogMessageType.ReadCallback, Definitions.LogType.Error, string.Format("ReadCallback - RLM {0} closed connection", state.DeviceIpAddress));

                    // kill connection
                    RemoveConnection(state.DeviceIpAddress);
                }
            }
            catch (Exception e)
            {
                _logManager.Log(state.DeviceIpAddress, "", state, Definitions.LogMessageType.ReadCallback, Definitions.LogType.Exception, string.Format("ReadCallback: RLM {0} closed connection, Exception Raised: {1}", state.DeviceIpAddress, e.ToString()));
                RemoveConnection(state.DeviceIpAddress);
            }
        }

        private void Send(string deviceIpAddress, NetworkStream handler, byte[] data)
        {
            try
            {
                // Begin sending the data to the remote device.
                handler.BeginWrite(data, 0, data.Length, new AsyncCallback(SendCallback), handler);
            }
            catch (Exception e)
            {
                _logManager.Log(deviceIpAddress, "", handler, Definitions.LogMessageType.SendCallback, Definitions.LogType.Exception, string.Format("Send Error, Closing connection ", e));
                RemoveConnection(deviceIpAddress);
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            // Retrieve the socket from the state object.
            NetworkStream handler = (NetworkStream)ar.AsyncState;

            // Complete sending the data to the remote device.
            handler.EndWrite(ar);
        }

        private void RemoveConnection(string deviceIpAddress)
        {
            // Try to find entry. If not available, then already removed from list.          
            TCPStateObjectInsecure tcpState;
            _tcpStateObjectList.TryGetValue(deviceIpAddress, out tcpState);

            if (tcpState != null)
            {
                // Remove from list, Send Close Connection (if possible) and close connection
                // If connection alive, generate and send close session message, otherwise just close
                if (tcpState.TcpClient.Connected)
                {
                    byte[] closeMessage = _RLMCommunication.GenerateCloseSession(deviceIpAddress);

                    // Synchronous Write
                    tcpState.WorkStream.Write(closeMessage, 0, closeMessage.Length);
                    
                }
                tcpState.WorkStream.Close();
                tcpState.TcpClient.Close();

                _tcpStateObjectList.TryRemove(deviceIpAddress, out tcpState);
            }

            // Clean up list
            _RLMCommunication.RemoveRLMDeviceFromList(deviceIpAddress);
        }


        private void ProcessUserInteractionEvent(string deviceIpAddress, string message, string[] options)
        {
            TCPStateObjectInsecure tcpState;
            _tcpStateObjectList.TryGetValue(deviceIpAddress, out tcpState);

            if (tcpState != null)
            {
                byte[] returnMessage = _RLMCommunication.ProcessEvent(deviceIpAddress, message, options);

                // Send off Client
                // Send Message if there is something to send back
                if (returnMessage.Length > 0)
                {
                    _logManager.TraceIt(Definitions.LogType.Information, string.Format("Sending message to RLM {0}, data {1}", tcpState.DeviceIpAddress, General.ByteArrayToHexString(returnMessage)));
                    Send(deviceIpAddress, tcpState.WorkStream, returnMessage);
                }
            }
            else // Kill Connection if not active
            {
                RemoveConnection(deviceIpAddress);
            }
        }
    }

    public class TCPStateObjectInsecure
    {
        // Device Id
        public string DeviceIpAddress = string.Empty;

        // TCP Client Object
        public TcpClient TcpClient;

        // Client  socket.
        public NetworkStream WorkStream = null;

        // Max size of payload is 1024 + 6 bytes of header
        public const int BufferSize = 2000;

        public const int MaxPayload = 1024;

        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];

        // Received data bytes
        public IEnumerable<byte> receivedBuffer = Enumerable.Empty<byte>();

        // Full message received, assume yes on first round
        public bool fullPayloadReceived = true;

        // First message
        public bool firstMessage = true;

        // Total Payload of message
        public int payloadLength = 0;
    }
}
