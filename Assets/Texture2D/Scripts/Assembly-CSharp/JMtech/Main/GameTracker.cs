using System;
using System.Threading;
using Assets;
using Assets.Scripts.Addons.ShareScript;
using Assets.Scripts.Addons.ShareScript.Events;
using JMtech.Translations;
using ServerControlFramework;
using ServerControlSoftware.Externals.Shared.Packets;
using ServerTransferProgram.LogicControllers;
using ServerTransferProgram.ServerControlLiberary.DataControllers.DefaultEvents;
using UnityEngine;

namespace JMtech.Main
{
	public class GameTracker : MonoBehaviour
	{
		public static GameTracker main;

		public long tickSpeed;

		public long clientSpeed;

		private bool initatedPosted;

		public Client client;

		public bool clientInitiated;

		public static SynchronizationContext sync;

		private Action doAfterFetch;

		public static void Initialize()
		{
			if (!(main != null))
			{
				main = new GameObject().AddComponent<GameTracker>();
				UnityEngine.Object.DontDestroyOnLoad(main);
				main.name = "Game Tracker";
				main.SetContext();
				UpdateableObject.Initiate();
			}
		}

		private void SetContext()
		{
			if (SyncContext.context == null)
			{
				SyncContext.context = SynchronizationContext.Current;
			}
		}

		private void Update()
		{
			UpdateableObject.InternalTick();
			SetContext();
			tickSpeed = UpdateableObject.CoreTicksPerSecond;
			if (client != null && client.rec != null)
			{
				clientSpeed = client.rec.TicksPerSecond;
			}
		}

		public void StartConnection(Action after)
		{
			doAfterFetch = after;
			MasterInfo masterInfo = MasterInfo.Fetch();
			if (masterInfo == null)
			{
				SyncContext.RunOnUI(MenuManager.ShowMsg, SFST.STP_Request_Timeout);
				return;
			}
			client = new Client(masterInfo.REDIRECT, 100);
			client.rec.eventSystem.RegisterListener(new EventListener<ClientAuthedEvent>((ClientAuthedEvent e) =>
			{
				SyncContext.RunOnUI(after);
			}));
			client.rec.eventSystem.RegisterListener(new EventListener<InitialTimeoutEvent>((InitialTimeoutEvent e) =>
			{
				SyncContext.RunOnUI(MenuManager.ShowMsg, SFST.STP_Request_Timeout);
			}));
			client.rec.eventSystem.RegisterListener(new EventListener<PacketReceivedEvent>((PacketReceivedEvent e) =>
			{
				if (e.receivedPacket.Is<ServerErrorPacket>())
				{
					ServerErrorPacket serverErrorPacket = e.receivedPacket.ToTargetPacket<ServerErrorPacket>();
					if (serverErrorPacket.Error == "AUTHENTICATION")
					{
						SyncContext.RunOnUI(() =>
						{
							MenuManager.ShowMsg(SFST.STP_Old_Credentials);
							client.rec.Delete();
							client.CleanCreds();
						});
						StartConnection(() =>
						{
							SyncContext.RunOnUI(MenuManager.ShowMsg, SFST.STP_Reconnected);
						});
					}
				}
			}));
		}

		public void StartedConnection()
		{
			clientInitiated = true;
			SyncContext.RunOnUI(client.FetchCreds);
		}
	}
}
