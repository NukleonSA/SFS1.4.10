using System;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using UnityEngine;

public class AdManager
{
	private static float lastTimeShown;

	private static BannerView banner;

	private static InterstitialAd fullscreenAd;

	public static bool isBannerShowing = false;

	public static bool isFullscreenShowing = false;

	public static bool isBannerReady = false;

	public static bool isFullscreenLoaded = false;

	private static bool initialized = false;

	private static Dictionary<int, string> Banners = new Dictionary<int, string>
	{
		{ 30, "ca-app-pub-6007035016786046/4609360240" },
		{ 60, "ca-app-pub-6007035016786046/7155378941" },
		{ 90, "ca-app-pub-6007035016786046/5245307577" },
		{ 120, "ca-app-pub-6007035016786046/9206827214" }
	};

	private static string BackToBuild = "ca-app-pub-6007035016786046/4518549262";

	private static string MainMenu = "ca-app-pub-6007035016786046/8529946464";

	private static string OnLoad = "ca-app-pub-6007035016786046/9104661539";

	private static string OnSave = "ca-app-pub-6007035016786046/7427095273";

	private static string OnRecover = "ca-app-pub-6007035016786046/6530469084";

	public static bool ShowAds
	{
		get
		{
			return !Ref.hasPartsExpansion;
		}
	}

	public static void InitializeAds()
	{
		if (!initialized)
		{
			initialized = true;
			if (ShowAds)
			{
				MobileAds.Initialize("ca-app-pub-6007035016786046~7263443741");
			}
		}
	}

	public static void LoadBannerAd()
	{
		if (ShowAds)
		{
			InitializeAds();
			banner = new BannerView("ca-app-pub-6007035016786046/7155378941", AdSize.Banner, AdPosition.Top);
			banner.OnAdLoaded += Banner_OnAdLoaded;
			AdRequest request = new AdRequest.Builder().Build();
			banner.LoadAd(request);
		}
	}

	public static void HideBannerAd()
	{
		if (isBannerShowing)
		{
			banner.Hide();
			banner.Destroy();
			banner = null;
			isBannerShowing = false;
		}
	}

	public static void LoadFullscreenAd()
	{
		if (ShowAds)
		{
			InitializeAds();
			if (!isFullscreenLoaded)
			{
				fullscreenAd = new InterstitialAd("ca-app-pub-6007035016786046/7427095273");
				fullscreenAd.OnAdOpening += FullscreenAd_OnAdOpening;
				fullscreenAd.OnAdClosed += FullscreenAd_OnAdClosed;
				fullscreenAd.OnAdLoaded += FullscreenAd_OnAdLoaded;
				fullscreenAd.OnAdFailedToLoad += FullscreenAd_OnAdFailedToLoad;
				AdRequest request = new AdRequest.Builder().Build();
				fullscreenAd.LoadAd(request);
			}
		}
	}

	private static void FullscreenAd_OnAdFailedToLoad(object sender, AdFailedToLoadEventArgs e)
	{
	}

	private static void FullscreenAd_OnAdLoaded(object sender, EventArgs e)
	{
		isFullscreenLoaded = true;
	}

	private static void FullscreenAd_OnAdClosed(object sender, EventArgs e)
	{
		isFullscreenShowing = false;
	}

	private static void FullscreenAd_OnAdOpening(object sender, EventArgs e)
	{
		isFullscreenShowing = true;
	}

	private static void Banner_OnAdLoaded(object sender, EventArgs e)
	{
		isBannerReady = true;
	}

	public static void ShowBanner()
	{
		if (ShowAds)
		{
			if (isBannerReady)
			{
				isBannerReady = false;
				banner.Show();
				isBannerShowing = true;
			}
			LoadBannerAd();
		}
	}

	public static void ShowFullscreen()
	{
		if (ShowAds && !(Time.time - lastTimeShown < 80f))
		{
			lastTimeShown = Time.time;
			if (isFullscreenLoaded)
			{
				isFullscreenShowing = true;
				isFullscreenLoaded = false;
				fullscreenAd.Show();
			}
			LoadFullscreenAd();
		}
	}
}
