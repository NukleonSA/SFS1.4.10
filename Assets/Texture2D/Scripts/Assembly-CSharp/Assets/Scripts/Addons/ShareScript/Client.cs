using System;
using System.IO;
using Assets.Scripts.Addons.ShareScript.Events;
using JMtech.Main;
using ServerControlSoftware.Externals.Localized.MassData;
using ServerControlSoftware.Externals.Shared;
using ServerControlSoftware.Externals.Shared.Objects;
using ServerControlSoftware.Externals.Shared.Packets;
using ServerTransferProgram.LogicControllers;
using ServerTransferProgram.ServerControlLiberary;
using ServerTransferProgram.ServerControlLiberary.DataControllers.PacketSystem;
using UnityEngine;

namespace Assets.Scripts.Addons.ShareScript
{
	public class Client
	{
		public SCReceiver rec;

		public AuthID myCredentials;

		public Client(string host, int port)
		{
			rec = new SCReceiver(host, port);
			rec.eventSystem.RegisterListener(new EventListener<ClientInitiatedEvent>(ClientReady, null));
		}

		public void ClientReady(ClientInitiatedEvent e)
		{
			GameTracker.main.StartedConnection();
		}

		public void AuthedRequest(AuthedPacket toSend, Action<DefaultPacket> response)
		{
			toSend.Credentials = myCredentials;
			rec.Request(toSend, response);
		}

		public void CleanCreds()
		{
			string path = Path.Combine(Application.persistentDataPath, "myCredentials");
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}

		public void FetchCreds()
		{
			string path = Path.Combine(Application.persistentDataPath, "myCredentials");
			if (!File.Exists(path))
			{
				rec.Request(new GenerateAuthPacket(), (DefaultPacket r) =>
				{
					if (r.Success)
					{
						myCredentials = r.ToTargetPacket<AuthedPacket>().Credentials;
						new DefaultIOContext().StoreToFile(path, myCredentials);
						rec.eventSystem.Invoke(new ClientAuthedEvent());
					}
				});
			}
			else
			{
				new DefaultIOContext().FetchFromFile<AuthID>(path, out myCredentials);
				rec.eventSystem.Invoke(new ClientAuthedEvent());
			}
		}
	}
}
