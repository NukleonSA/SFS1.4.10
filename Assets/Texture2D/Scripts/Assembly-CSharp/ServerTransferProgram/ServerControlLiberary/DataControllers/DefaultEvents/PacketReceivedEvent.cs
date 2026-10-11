using ServerTransferProgram.LogicControllers.EventTypes;
using ServerTransferProgram.ServerControlLiberary.DataControllers.PacketSystem;

namespace ServerTransferProgram.ServerControlLiberary.DataControllers.DefaultEvents
{
	public class PacketReceivedEvent : Event
	{
		public DefaultPacket receivedPacket;

		public PacketReceivedEvent(DefaultPacket pack)
		{
			receivedPacket = pack;
		}
	}
}
