namespace ServerTransferProgram.ServerControlLiberary.DataControllers.PacketSystem.Packets
{
	public class AuthenticationHeaderPacket : DefaultPacket
	{
		public int ID;

		public string KEY1;

		public string KEY2;

		public AuthenticationHeaderPacket(int id, string key1, string key2)
		{
			ID = id;
			KEY1 = key1;
			KEY2 = key2;
		}
	}
}
