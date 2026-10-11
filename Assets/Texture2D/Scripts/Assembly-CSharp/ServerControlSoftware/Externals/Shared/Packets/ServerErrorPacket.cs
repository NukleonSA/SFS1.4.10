using ServerTransferProgram.ServerControlLiberary.DataControllers.PacketSystem;

namespace ServerControlSoftware.Externals.Shared.Packets
{
	public class ServerErrorPacket : DefaultPacket
	{
		public string Task = string.Empty;

		public ServerErrorPacket(string task, string error)
		{
			SetError(error);
			Task = task;
		}
	}
}
