using UnityEngine;

namespace admob
{
	public class AdmobListenerProxy : AndroidJavaProxy
	{
		private IAdmobListener listener;

		internal AdmobListenerProxy(IAdmobListener listener)
			: base("com.admob.plugin.IAdmobListener")
		{
			this.listener = listener;
		}

		private void onAdmobEvent(string adtype, string eventName, string paramString)
		{
			if (listener != null)
			{
				listener.onAdmobEvent(adtype, eventName, paramString);
			}
		}

		private new string toString()
		{
			return "AdmobListenerProxy";
		}
	}
}
