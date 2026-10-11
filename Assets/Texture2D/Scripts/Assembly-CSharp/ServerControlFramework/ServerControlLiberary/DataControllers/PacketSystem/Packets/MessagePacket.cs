using ServerTransferProgram.ServerControlLiberary.DataControllers.PacketSystem;

namespace ServerControlFramework.ServerControlLiberary.DataControllers.PacketSystem.Packets
{
	public class MessagePacket : DefaultPacket
	{
		public string inside;

		public string orgin;

		public MessagePacket(string content)
		{
			inside = content;
		}
	}
}
