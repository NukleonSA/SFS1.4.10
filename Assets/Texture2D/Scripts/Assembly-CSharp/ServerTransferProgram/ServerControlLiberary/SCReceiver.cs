using System;
using System.Net.Sockets;
using System.Threading;
using Assets.Scripts.Addons.ShareScript.Events;
using ServerControlFramework.ServerControlLiberary.DataControllers.DefaultEvents;
using ServerControlFramework.ServerControlLiberary.DataControllers.PacketSystem.Packets;
using ServerTransferProgram.LogicControllers;
using ServerTransferProgram.ServerControlLiberary.DataControllers.DefaultEvents;
using ServerTransferProgram.ServerControlLiberary.DataControllers.PacketSystem;
using ServerTransferProgram.ServerControlLiberary.DataControllers.PacketSystem.Packets;

namespace ServerTransferProgram.ServerControlLiberary
{
	[DelayedStart]
	public class SCReceiver : UpdateableObject
	{
		private TcpClient client = new TcpClient();

		private bool authed;

		private bool Ready;

		public bool Connected;

		public EventManager eventSystem = new EventManager();

		public PacketController controller;

		public SCReceiver(string host, int port)
		{
			SCReceiver sCReceiver = this;
			eventSystem.RegisterListener(new EventListener<PacketReceivedEvent>(PacketReceived, null));
			eventSystem.RegisterListener(new EventListener<ConnectionReadyEvent>(ConnectionReady, null));
			UpdateableObject uo = this;
			ThreadPool.QueueUserWorkItem((object s) =>
			{
				IAsyncResult asyncResult = sCReceiver.client.BeginConnect(host, port, null, null);
				if (asyncResult.AsyncWaitHandle.WaitOne(1000))
				{
					sCReceiver.controller = PacketController.Create(sCReceiver);
					uo.SetActive(true);
					sCReceiver.Connected = true;
				}
				else
				{
					sCReceiver.eventSystem.Invoke(new InitialTimeoutEvent());
				}
			});
		}

		public TcpClient MyConnection()
		{
			return client;
		}

		private void PacketReceived(PacketReceivedEvent e)
		{
			if (e.receivedPacket.Is<PingPacket>())
			{
				controller.SendPacket(new PingPacket());
			}
		}

		private void Update()
		{
			if (controller != null && controller.IsReady() && !Ready)
			{
				Ready = true;
				eventSystem.Invoke(new ConnectionReadyEvent());
			}
			else if (controller != null)
			{
			}
			foreach (DefaultPacket packet in controller.GetPackets())
			{
				eventSystem.Invoke(new PacketReceivedEvent(packet));
			}
			controller.SendPackets();
		}

		private void ConnectionReady(ConnectionReadyEvent e)
		{
			ThreadPool.QueueUserWorkItem((object s) =>
			{
				controller.SendPacket(new ClientReadyPacket());
				eventSystem.Invoke(new ClientInitiatedEvent());
			});
		}

		public void Request(DefaultPacket toSend, Action<DefaultPacket> onResponse)
		{
			EventListener<PacketReceivedEvent> PRE = null;
			PRE = new EventListener<PacketReceivedEvent>((PacketReceivedEvent e) =>
			{
				if (e.receivedPacket.ResponseTo == toSend.PacketID)
				{
					onResponse(e.receivedPacket);
					eventSystem.RemoveListener(PRE);
				}
			});
			eventSystem.RegisterListener(PRE);
			controller.SendPacket(toSend);
		}

		protected void OnDelete()
		{
		}
	}
}
