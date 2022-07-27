using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace AppNet
{
    public class TcpClientAsyncMgr : MonoBehaviour
    {
        private Socket _socket;
        private readonly byte[] _receiveBuffer = new byte[1024];
        private int _bufferIndex = 0;
        private readonly Queue<BaseMsg> _receiveQueue = new();
        private readonly HeartMsg _heartMsg = new();
        private readonly int HEART_MSG_TIME_SPAN = 5;
        private static TcpClientAsyncMgr _instance = null;
        public static TcpClientAsyncMgr Instance => _instance;

        private void Awake()
		{
			_instance = this;
			DontDestroyOnLoad(gameObject);
			Connect("127.0.0.1", 9999);
			InvokeRepeating(nameof(SendHeartMsg), 0, HEART_MSG_TIME_SPAN);
		}

		private void Update()
		{
			if (_receiveQueue.Count > 0)
			{
				switch (_receiveQueue.Dequeue())
				{
					case PlayerMsg msg:
						print(msg.playerData.name);
						print(msg.playerData.lev);
						print(msg.playerData.atk);
						print(msg.playerID);
						break;
				}
			}
		}

        private void OnDestroy()
		{
			Close();
		}

        public void Connect(string ip, int port)
        {
            if(_socket != null && _socket.Connected)
                return;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            SocketAsyncEventArgs connectArgs = new();
			connectArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
			connectArgs.Completed += (sender, args) =>
			{
				if (args.SocketError == SocketError.Success)
				{
					print("连接成功");
					SocketAsyncEventArgs receiveArgs = new();
					receiveArgs.SetBuffer(_receiveBuffer, 0, _receiveBuffer.Length);
					receiveArgs.Completed += (sender, args) =>
					{
						if (args.SocketError == SocketError.Success)
						{
							HandleReceiveMsg(args.BytesTransferred);
							args.SetBuffer(_bufferIndex, args.Buffer.Length - _bufferIndex);
							if (_socket != null && _socket.Connected)
								_socket.ReceiveAsync(args);
							else
								Close();
						}
						else
						{
							print("接收消息出错: " + args.SocketError);
							Close();
						}
					};
					_socket.ReceiveAsync(receiveArgs);
				}
				else
				{
					print("连接失败: " + args.SocketError);
					Close();
				}
			};
			_socket.ConnectAsync(connectArgs);
        }

        public void SendBytes(byte[] bytes)
        {
	        SocketAsyncEventArgs args = new();
	        args.SetBuffer(bytes, 0, bytes.Length);
	        args.Completed += (sender, args) =>
	        {
		        if (args.SocketError != SocketError.Success)
		        {
			        print("发送失败: " + args.SocketError);
			        Close();
		        }
	        };
	        _socket.SendAsync(args);
        }

        public void Send(BaseMsg msg)
        {
	        if (_socket != null && _socket.Connected)
	        {
		        byte[] bytes = msg.Writing();
		        SocketAsyncEventArgs sendArgs = new();
		        sendArgs.SetBuffer(bytes, 0, bytes.Length);
		        sendArgs.Completed += (sender, args) =>
		        {
			        if (sendArgs.SocketError != SocketError.Success)
			        {
				        print("发送失败: " + sendArgs.SocketError);
				        Close();
			        }
		        };
		        _socket.SendAsync(sendArgs);
	        }
	        else
	        {
		        Close();
	        }
        }

        void SendHeartMsg() => Send(_heartMsg);

        private void Close()
        {
	        if (_socket != null)
	        {
		        QuitMsg msg = new();
		        _socket.Send(msg.Writing());
		        _socket.Shutdown(SocketShutdown.Both);
		        _socket.Disconnect(false);
		        _socket.Close();
		        _socket = null;
	        }
        }
        
        /// <summary>
        /// 处理分包、黏包
        /// </summary>
        /// <param name="count"></param>
        private void HandleReceiveMsg(int count)
        {
	        _bufferIndex += count;
	        while (true)
	        {
		        int readIndex = 0, msgID = -1, msgLength = -1;
		        if (_bufferIndex - readIndex >= 8)
		        {
			        msgID = BitConverter.ToInt32(_receiveBuffer, readIndex);
			        readIndex += 4;
			        msgLength = BitConverter.ToInt32(_receiveBuffer, readIndex);
			        readIndex += 4;
		        }
		        if (_bufferIndex - readIndex >= msgLength && msgLength != -1)
		        {
			        BaseMsg msg = null;
			        switch (msgID)
			        {
				        case 1001:
					        msg = new PlayerMsg();
					        msg.Reading(_receiveBuffer, readIndex);
					        break;
				        case 999:
					        msg = new HeartMsg();
					        break;;
			        }
			        readIndex += msgLength;
			        if(msg != null)
				        _receiveQueue.Enqueue(msg);
			        if (readIndex == _bufferIndex)
			        {
				        _bufferIndex = 0;
				        break;
			        }
		        }
		        else
		        {
			        if (msgLength != -1)
				        readIndex -= 8;
			        Array.Copy(_receiveBuffer, readIndex, _receiveBuffer, 0, _bufferIndex - readIndex);
			        _bufferIndex -= readIndex;
			        break;
		        }
	        }
        }
    }
}