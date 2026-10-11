using System;
using System.Diagnostics;
using System.Threading;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine;

namespace GoogleMobileAds.Android
{
	public class InterstitialClient : AndroidJavaProxy, IInterstitialClient
	{
		private AndroidJavaObject interstitial;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private EventHandler<EventArgs> OnAdLoaded__BackingField;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private EventHandler<AdFailedToLoadEventArgs> OnAdFailedToLoad__BackingField;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private EventHandler<EventArgs> OnAdOpening__BackingField;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private EventHandler<EventArgs> OnAdClosed__BackingField;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private EventHandler<EventArgs> OnAdLeavingApplication__BackingField;

		public event EventHandler<EventArgs> OnAdLoaded
		{
			add
			{
				EventHandler<EventArgs> eventHandler = OnAdLoaded__BackingField;
				EventHandler<EventArgs> eventHandler2;
				do
				{
					eventHandler2 = eventHandler;
					eventHandler = Interlocked.CompareExchange(ref OnAdLoaded__BackingField, (EventHandler<EventArgs>)Delegate.Combine(eventHandler2, value), eventHandler);
				}
				while ((object)eventHandler != eventHandler2);
			}
			remove
			{
				EventHandler<EventArgs> eventHandler = OnAdLoaded__BackingField;
				EventHandler<EventArgs> eventHandler2;
				do
				{
					eventHandler2 = eventHandler;
					eventHandler = Interlocked.CompareExchange(ref OnAdLoaded__BackingField, (EventHandler<EventArgs>)Delegate.Remove(eventHandler2, value), eventHandler);
				}
				while ((object)eventHandler != eventHandler2);
			}
		}

		public event EventHandler<AdFailedToLoadEventArgs> OnAdFailedToLoad
		{
			add
			{
				EventHandler<AdFailedToLoadEventArgs> eventHandler = OnAdFailedToLoad__BackingField;
				EventHandler<AdFailedToLoadEventArgs> eventHandler2;
				do
				{
					eventHandler2 = eventHandler;
					eventHandler = Interlocked.CompareExchange(ref OnAdFailedToLoad__BackingField, (EventHandler<AdFailedToLoadEventArgs>)Delegate.Combine(eventHandler2, value), eventHandler);
				}
				while ((object)eventHandler != eventHandler2);
			}
			remove
			{
				EventHandler<AdFailedToLoadEventArgs> eventHandler = OnAdFailedToLoad__BackingField;
				EventHandler<AdFailedToLoadEventArgs> eventHandler2;
				do
				{
					eventHandler2 = eventHandler;
					eventHandler = Interlocked.CompareExchange(ref OnAdFailedToLoad__BackingField, (EventHandler<AdFailedToLoadEventArgs>)Delegate.Remove(eventHandler2, value), eventHandler);
				}
				while ((object)eventHandler != eventHandler2);
			}
		}

		public event EventHandler<EventArgs> OnAdOpening
		{
			add
			{
				EventHandler<EventArgs> eventHandler = OnAdOpening__BackingField;
				EventHandler<EventArgs> eventHandler2;
				do
				{
					eventHandler2 = eventHandler;
					eventHandler = Interlocked.CompareExchange(ref OnAdOpening__BackingField, (EventHandler<EventArgs>)Delegate.Combine(eventHandler2, value), eventHandler);
				}
				while ((object)eventHandler != eventHandler2);
			}
			remove
			{
				EventHandler<EventArgs> eventHandler = OnAdOpening__BackingField;
				EventHandler<EventArgs> eventHandler2;
				do
				{
					eventHandler2 = eventHandler;
					eventHandler = Interlocked.CompareExchange(ref OnAdOpening__BackingField, (EventHandler<EventArgs>)Delegate.Remove(eventHandler2, value), eventHandler);
				}
				while ((object)eventHandler != eventHandler2);
			}
		}

		public event EventHandler<EventArgs> OnAdClosed
		{
			add
			{
				EventHandler<EventArgs> eventHandler = OnAdClosed__BackingField;
				EventHandler<EventArgs> eventHandler2;
				do
				{
					eventHandler2 = eventHandler;
					eventHandler = Interlocked.CompareExchange(ref OnAdClosed__BackingField, (EventHandler<EventArgs>)Delegate.Combine(eventHandler2, value), eventHandler);
				}
				while ((object)eventHandler != eventHandler2);
			}
			remove
			{
				EventHandler<EventArgs> eventHandler = OnAdClosed__BackingField;
				EventHandler<EventArgs> eventHandler2;
				do
				{
					eventHandler2 = eventHandler;
					eventHandler = Interlocked.CompareExchange(ref OnAdClosed__BackingField, (EventHandler<EventArgs>)Delegate.Remove(eventHandler2, value), eventHandler);
				}
				while ((object)eventHandler != eventHandler2);
			}
		}

		public event EventHandler<EventArgs> OnAdLeavingApplication
		{
			add
			{
				EventHandler<EventArgs> eventHandler = OnAdLeavingApplication__BackingField;
				EventHandler<EventArgs> eventHandler2;
				do
				{
					eventHandler2 = eventHandler;
					eventHandler = Interlocked.CompareExchange(ref OnAdLeavingApplication__BackingField, (EventHandler<EventArgs>)Delegate.Combine(eventHandler2, value), eventHandler);
				}
				while ((object)eventHandler != eventHandler2);
			}
			remove
			{
				EventHandler<EventArgs> eventHandler = OnAdLeavingApplication__BackingField;
				EventHandler<EventArgs> eventHandler2;
				do
				{
					eventHandler2 = eventHandler;
					eventHandler = Interlocked.CompareExchange(ref OnAdLeavingApplication__BackingField, (EventHandler<EventArgs>)Delegate.Remove(eventHandler2, value), eventHandler);
				}
				while ((object)eventHandler != eventHandler2);
			}
		}

		public InterstitialClient()
			: base("com.google.unity.ads.UnityAdListener")
		{
			AndroidJavaClass androidJavaClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			AndroidJavaObject androidJavaObject = androidJavaClass.GetStatic<AndroidJavaObject>("currentActivity");
			interstitial = new AndroidJavaObject("com.google.unity.ads.Interstitial", androidJavaObject, this);
		}

		public void CreateInterstitialAd(string adUnitId)
		{
			interstitial.Call("create", adUnitId);
		}

		public void LoadAd(AdRequest request)
		{
			interstitial.Call("loadAd", Utils.GetAdRequestJavaObject(request));
		}

		public bool IsLoaded()
		{
			return interstitial.Call<bool>("isLoaded", new object[0]);
		}

		public void ShowInterstitial()
		{
			interstitial.Call("show");
		}

		public void DestroyInterstitial()
		{
			interstitial.Call("destroy");
		}

		public string MediationAdapterClassName()
		{
			return interstitial.Call<string>("getMediationAdapterClassName", new object[0]);
		}

		public void onAdLoaded()
		{
			if (OnAdLoaded__BackingField != null)
			{
				OnAdLoaded__BackingField(this, EventArgs.Empty);
			}
		}

		public void onAdFailedToLoad(string errorReason)
		{
			if (OnAdFailedToLoad__BackingField != null)
			{
				AdFailedToLoadEventArgs e = new AdFailedToLoadEventArgs();
				e.Message = errorReason;
				AdFailedToLoadEventArgs e2 = e;
				OnAdFailedToLoad__BackingField(this, e2);
			}
		}

		public void onAdOpened()
		{
			if (OnAdOpening__BackingField != null)
			{
				OnAdOpening__BackingField(this, EventArgs.Empty);
			}
		}

		public void onAdClosed()
		{
			if (OnAdClosed__BackingField != null)
			{
				OnAdClosed__BackingField(this, EventArgs.Empty);
			}
		}

		public void onAdLeftApplication()
		{
			if (OnAdLeavingApplication__BackingField != null)
			{
				OnAdLeavingApplication__BackingField(this, EventArgs.Empty);
			}
		}
	}
}
