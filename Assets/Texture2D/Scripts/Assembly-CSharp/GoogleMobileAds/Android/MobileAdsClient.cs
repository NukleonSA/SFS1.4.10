using GoogleMobileAds.Common;
using UnityEngine;

namespace GoogleMobileAds.Android
{
	public class MobileAdsClient : IMobileAdsClient
	{
		private static MobileAdsClient instance = new MobileAdsClient();

		public static MobileAdsClient Instance
		{
			get
			{
				return instance;
			}
		}

		private MobileAdsClient()
		{
		}

		public void Initialize(string appId)
		{
			AndroidJavaClass androidJavaClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			AndroidJavaObject androidJavaObject = androidJavaClass.GetStatic<AndroidJavaObject>("currentActivity");
			AndroidJavaClass androidJavaClass2 = new AndroidJavaClass("com.google.android.gms.ads.MobileAds");
			androidJavaClass2.CallStatic("initialize", androidJavaObject, appId);
		}

		public void SetApplicationVolume(float volume)
		{
			AndroidJavaClass androidJavaClass = new AndroidJavaClass("com.google.android.gms.ads.MobileAds");
			androidJavaClass.CallStatic("setAppVolume", volume);
		}

		public void SetApplicationMuted(bool muted)
		{
			AndroidJavaClass androidJavaClass = new AndroidJavaClass("com.google.android.gms.ads.MobileAds");
			androidJavaClass.CallStatic("setAppMuted", muted);
		}

		public void SetiOSAppPauseOnBackground(bool pause)
		{
		}
	}
}
