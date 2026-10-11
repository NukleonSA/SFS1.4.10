using System.Collections.Generic;

namespace ServerControlSoftware.Externals.Shared.RequestPackets
{
	public class DownloadRequestPacket : AuthedPacket
	{
		public string targetRocket;

		public List<string> exclude = new List<string>();
	}
}
