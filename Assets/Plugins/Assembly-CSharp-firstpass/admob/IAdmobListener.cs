namespace admob
{
	internal interface IAdmobListener
	{
		void onAdmobEvent(string adtype, string eventName, string paramString);
	}
}
