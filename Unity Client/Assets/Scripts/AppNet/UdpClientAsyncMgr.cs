using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace AppNet
{
    public class UdpClientAsyncMgr : MonoBehaviour
    {
        private Socket _socket = null;
        private EndPoint _serverEP = null;
        private readonly byte[] _receiveBuffer = new byte[512];
        private readonly Queue<BaseMsg> _receiveQueue = new();
        private static UdpClientAsyncMgr _instance = null;
        public static UdpClientAsyncMgr Instance => _instance;

        private void Awake()
        {
            _instance = this;
            StartUp("127.0.0.1", 9999);
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            while (_receiveQueue.Count > 0)
            {
                switch (_receiveQueue.Dequeue())
                {
                    case PlayerMsg msg:
                        print(msg.playerID);
                        print(msg.playerData.name);
                        print(msg.playerData.atk);
                        print(msg.playerData.lev);
                        break;
                }
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                PlayerMsg msg = new();
                msg.playerData = new();
                msg.playerID = 10086;
                msg.playerData.name = "UDP异步客户端消息";
                msg.playerData.atk = 66;
                msg.playerData.lev = 77;
                SendTo(msg);
            }
        }

        private void OnDestroy()
        {
            Close();
        }

        public void StartUp(string ip, int port)
        {
            try
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _socket.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888));
                _serverEP = new IPEndPoint(IPAddress.Parse(ip), port);
                SocketAsyncEventArgs receiveArgs = new();
                receiveArgs.SetBuffer(_receiveBuffer, 0, _receiveBuffer.Length);
                receiveArgs.RemoteEndPoint = _serverEP;
                receiveArgs.Completed += ReceiveHandler;
                _socket.ReceiveFromAsync(receiveArgs);
                print("UDP异步客户端已启动");
            }
            catch (Exception e)
            {
                Console.WriteLine("启动失败: " + e.Message);
                throw;
            }
        }

        private void ReceiveHandler(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success && args.RemoteEndPoint.Equals(_serverEP))
            {
                try
                {
                    int index = 0;
                    int id = BitConverter.ToInt32(args.Buffer, index);
                    index += 4;
                    int length = BitConverter.ToInt32(args.Buffer, index);
                    index += 4;
                    BaseMsg msg = null;
                    switch (id)
                    {
                        case 1001:
                            msg = new PlayerMsg();
                            msg.Reading(args.Buffer, index);
                            break;
                        case 1003:
                            msg = new QuitMsg();
                            break;
                    }
                    if(msg != null)
                        _receiveQueue.Enqueue(msg);
                    if (_socket != null)
                    {
                        args.SetBuffer(0, _receiveBuffer.Length);
                        _socket.ReceiveFromAsync(args);
                    }
                }
                catch (Exception e)
                {
                    Close();
                    Console.WriteLine("接收失败:" + e.Message);
                    throw;
                }
            }
        }

        void SendTo(BaseMsg msg)
        {
            if (_socket != null)
            {
                try
                {
                    byte[] bytes = msg.Writing();
                    SocketAsyncEventArgs sendArgs = new();
                    sendArgs.SetBuffer(bytes, 0, bytes.Length);
                    sendArgs.RemoteEndPoint = _serverEP;
                    sendArgs.Completed += (sender, args) =>
                    {
                        if(args.SocketError != SocketError.Success)
                            print("发送失败: " + args.SocketError);
                    };
                    _socket.SendToAsync(sendArgs);
                }
                catch (Exception e)
                {
                    Console.WriteLine("发送异常: " + e.Message);
                    throw;
                }
            }
        }

        void Close()
        {
            if (_socket != null)
            {
                QuitMsg quitMsg = new QuitMsg();
                _socket.SendTo(quitMsg.Writing(), _serverEP);
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
                _socket = null;
            }
        }
    }
}