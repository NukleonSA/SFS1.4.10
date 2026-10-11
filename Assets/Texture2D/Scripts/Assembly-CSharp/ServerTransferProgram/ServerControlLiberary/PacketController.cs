using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using ServerTransferProgram.ServerControlLiberary.DataControllers.PacketSystem;

namespace ServerTransferProgram.ServerControlLiberary
{
	public class PacketController
	{
		private static int up;

		private static int down;

		private int myUp;

		private int myDown;

		public TcpClient target;

		private NetworkStream baseStream;

		private List<DefaultPacket> toSend = new List<DefaultPacket>();

		public static int Up
		{
			get
			{
				return up;
			}
			set
			{
				if (value < 0)
				{
					value = 0;
				}
				up = value;
			}
		}

		public static int Down
		{
			get
			{
				return down;
			}
			set
			{
				if (value < 0)
				{
					value = 0;
				}
				down = value;
			}
		}

		private PacketController(NetworkStream stream)
		{
			baseStream = stream;
		}

		public static PacketController Create(SCReceiver client)
		{
			PacketController packetController = null;
			try
			{
				packetController = new PacketController(client.MyConnection().GetStream());
			}
			catch (Exception)
			{
				return null;
			}
			packetController.target = client.MyConnection();
			return packetController;
		}

		public bool SendPacket(DefaultPacket packet)
		{
			try
			{
				toSend.Insert(0, packet);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public void SendPackets()
		{
			if (target == null || !baseStream.CanWrite || toSend.Count == 0)
			{
				return;
			}
			up++;
			myUp++;
			DefaultPacket[] array = toSend.ToArray();
			toSend.Clear();
			List<string> list = new List<string>();
			DefaultPacket[] array2 = array;
			foreach (DefaultPacket defaultPacket in array2)
			{
				string text = defaultPacket.ToSendable();
				text = text.Replace("<JSON_SPLIT_INDICATOR>", "#CAST_NOT_SUPPORTED");
				list.Insert(0, text);
			}
			string value = string.Join("<JSON_SPLIT_INDICATOR>", list.ToArray());
			NetworkStream stream;
			try
			{
				stream = target.GetStream();
			}
			catch (Exception)
			{
				return;
			}
			StreamWriter streamWriter = new StreamWriter(stream);
			if (streamWriter != null && baseStream.CanWrite)
			{
				try
				{
					streamWriter.WriteLine(value);
					streamWriter.Flush();
				}
				catch (Exception)
				{
				}
				up--;
				myUp--;
			}
		}

		public bool IsConnected()
		{
			try
			{
				NetworkStream stream = target.GetStream();
				StreamWriter streamWriter = new StreamWriter(stream);
				streamWriter.WriteLine();
				streamWriter.Flush();
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public bool IsReady()
		{
			try
			{
				NetworkStream stream = target.GetStream();
			}
			catch (Exception)
			{
				return false;
			}
			return baseStream.CanWrite && baseStream.CanRead;
		}

		public List<DefaultPacket> GetPackets()
		{
			List<DefaultPacket> list = new List<DefaultPacket>();
			NetworkStream stream;
			try
			{
				stream = target.GetStream();
			}
			catch (Exception)
			{
				return list;
			}
			StreamReader streamReader = new StreamReader(stream);
			if (streamReader == null || !streamReader.BaseStream.CanRead)
			{
				return list;
			}
			if (!baseStream.DataAvailable)
			{
				return list;
			}
			Down++;
			myDown++;
			bool flag = false;
			for (string text = streamReader.ReadLine(); text != null; text = streamReader.ReadLine())
			{
				string[] array = Regex.Split(text, "<JSON_SPLIT_INDICATOR>");
				foreach (string text2 in array)
				{
					if (!(text2 == string.Empty))
					{
						DefaultPacket defaultPacket = null;
						try
						{
							defaultPacket = DefaultPacket.FromSendable(text2) as DefaultPacket;
						}
						catch (Exception)
						{
							continue;
						}
						list.Insert(0, defaultPacket);
					}
				}
				if (!baseStream.DataAvailable)
				{
					flag = true;
					break;
				}
			}
			Down--;
			myDown--;
			return list;
		}

		public void Remove()
		{
			Up -= myUp;
			Down -= myDown;
		}
	}
}
