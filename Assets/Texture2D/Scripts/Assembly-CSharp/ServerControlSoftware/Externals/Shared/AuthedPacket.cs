using ServerControlSoftware.Externals.Shared.Objects;
using ServerTransferProgram.ServerControlLiberary.DataControllers.PacketSystem;

namespace ServerControlSoftware.Externals.Shared
{
	public class AuthedPacket : DefaultPacket
	{
		public AuthID Credentials;

		public void Authenticate(AuthID creds)
		{
			Credentials = creds;
		}

		public bool Authed()
		{
			return Credentials != null;
		}
	}
}
