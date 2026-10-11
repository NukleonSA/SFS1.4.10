namespace ServerControlSoftware.Externals.Shared.Objects.Identification
{
	public class IDGenerator
	{
		public long LastID;

		public long GenerateID()
		{
			long lastID = LastID;
			LastID++;
			return lastID;
		}
	}
}
