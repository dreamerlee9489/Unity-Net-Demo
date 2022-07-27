using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace AppNet
{
	public class UdpClientMgr : MonoBehaviour
	{
		private Socket _socket = null;
		private EndPoint _localEP = null, _serverEP = null;
		private Queue<BaseMsg> _receiveQueue = new(), _sendQueue = new();
		private byte[] _receiveBuffer = new byte[512];
		private static UdpClientMgr _instance = null;
		public static UdpClientMgr Instance => _instance;
		
		private void Awake()
		{
			_instance = this;
			StartUp("127.0.0.1", 9999);
			DontDestroyOnLoad(gameObject);
		}

		private void Update()
		{
			if (_receiveQueue.Count > 0)
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
				msg.playerID = 10086;
				msg.playerData = new PlayerData();
				msg.playerData.name = "UDP同步客户端消息";
				msg.playerData.atk = 77;
				msg.playerData.lev = 66;
				SendMsg(msg);
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
				_serverEP = new IPEndPoint(IPAddress.Parse(ip), port);
				_localEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);
				_socket.Bind(_localEP);
				ThreadPool.QueueUserWorkItem(ReceiveThread);
				ThreadPool.QueueUserWorkItem(SendThread);
				print("UDP同步客户端已启动!");
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				throw;
			}
		}

		private void SendThread(object state)
		{
			while (_socket != null)
			{
				if (_sendQueue.Count > 0)
				{
					try
					{
						_socket.SendTo(_sendQueue.Dequeue().Writing(), _serverEP);
					}
					catch (Exception e)
					{
						Console.WriteLine("发送失败:" + e.Message);
						throw;
					}
				}
			}
		}

		private void ReceiveThread(object state)
		{
			EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
			while (_socket != null)
			{
				if (_socket.Available > 0)
				{
					try
					{
						_socket.ReceiveFrom(_receiveBuffer, ref remoteEP);
						print( remoteEP + " " + _socket.Available);
						if (remoteEP.Equals(_serverEP))
						{
							int index = 0;
							int id = BitConverter.ToInt32(_receiveBuffer, index);
							index += 4;
							int length = BitConverter.ToInt32(_receiveBuffer, index);
							index += 4;
							BaseMsg msg = null;
							switch (id)
							{
								case 1001:
									msg = new PlayerMsg();
									msg.Reading(_receiveBuffer, index);
									break;
							}
							if(msg != null)
								_receiveQueue.Enqueue(msg);
						}
					}
					catch (Exception e)
					{
						Console.WriteLine("接收失败: " + e.Message);
						throw;
					}
				}
			}
		}

		void SendMsg(BaseMsg msg)
		{
			_sendQueue.Enqueue(msg);
		}

		public void Close()
		{
			_socket.SendTo(new QuitMsg().Writing(), _serverEP);
			_socket.Shutdown(SocketShutdown.Both);
			_socket.Close();
			_socket = null;
		}
	}
}
