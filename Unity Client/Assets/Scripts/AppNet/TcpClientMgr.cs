using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

namespace AppNet
{
	public class TcpClientMgr : MonoBehaviour
	{
		private bool _connected = false;
		private Socket _socket = null;
		private IPEndPoint _serverEP = null;
		private readonly Queue<BaseMsg> _sendQueue = new();
		private readonly Queue<BaseMsg> _receiveQueue = new();
		private readonly HeartMsg _heartMsg = new();
		private readonly int HEART_MSG_TIME_SPAN = 5;
		private readonly byte[] _receiveBuffer = new byte[1024];
		private int _bufferIndex = 0;
		private static TcpClientMgr _instance = null;
		public static TcpClientMgr Instance => _instance;
		
		private void Awake()
		{
			_instance = this;
			DontDestroyOnLoad(gameObject);
			Connect("127.0.0.1", 9999);
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
			if (!_connected)
			{
				try
				{
					_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
					_serverEP = new IPEndPoint(IPAddress.Parse(ip), port);
					_socket.Connect(_serverEP);
					_connected = true;
					Task.Run(SendMsgTask);
					Task.Run(RecvMsgTask);
					InvokeRepeating(nameof(SendHeartMsg), 0, HEART_MSG_TIME_SPAN);
				}
				catch (SocketException e)
				{
					Console.WriteLine(e.Message);
					throw new SocketException(e.ErrorCode);
				}
			}
		}
		
		private void SendHeartMsg() => SendMsg(_heartMsg);

		/// <summary>
		/// 主线程将消息压入队列, 副线程执行实际发送
		/// </summary>
		/// <param name="msg"></param>
		private void SendMsg(BaseMsg msg)
		{
			if (_connected)
				_sendQueue.Enqueue(_heartMsg);	
		}

		private void SendBytes(byte[] bytes)
		{
			_socket.Send(bytes);
		}

		private void SendMsgTask()
		{
			while (_connected)
			{
				if (_sendQueue.Count > 0)
				{
					BaseMsg msg = _sendQueue.Dequeue();
					_socket.Send(msg.Writing());
				}
			}
		}

		private void RecvMsgTask()
		{
			while (_connected)
			{
				// Available 已从网络接收并可供读取的字节数
				// 在调用Receive之前确定数据是否在排队等待读取
				if (_socket.Available > 0)
				{
					byte[] buffer = new byte[1024];
					int count = _socket.Receive(buffer);
					HandleMsg(buffer, count);
				}
			}
		}
		
		/// <summary>
		/// 处理分包、黏包
		/// </summary>
		/// <param name="receiveBuffer"></param>
		/// <param name="recvCount"></param>
		private void HandleMsg(byte[] receiveBuffer, int recvCount)
		{
			receiveBuffer.CopyTo(_receiveBuffer, _bufferIndex);
			_bufferIndex += recvCount;

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

		public void Close()
		{
			if (_socket != null)
			{
				_socket.Send(new QuitMsg().Writing());
				_socket.Shutdown(SocketShutdown.Both);
				_socket.Disconnect(false);
				_socket.Close();
				_socket = null;
				_connected = false;
			}
		}
	}
}
