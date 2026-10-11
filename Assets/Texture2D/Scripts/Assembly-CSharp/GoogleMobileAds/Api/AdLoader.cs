using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using GoogleMobileAds.Common;

namespace GoogleMobileAds.Api
{
	public class AdLoader
	{
		public class Builder
		{
			internal string AdUnitId { get; private set; }

			internal HashSet<NativeAdType> AdTypes { get; private set; }

			internal HashSet<string> TemplateIds { get; private set; }

			internal Dictionary<string, Action<CustomNativeTemplateAd, string>> CustomNativeTemplateClickHandlers { get; private set; }

			public Builder(string adUnitId)
			{
				AdUnitId = adUnitId;
				AdTypes = new HashSet<NativeAdType>();
				TemplateIds = new HashSet<string>();
				CustomNativeTemplateClickHandlers = new Dictionary<string, Action<CustomNativeTemplateAd, string>>();
			}

			public Builder ForCustomNativeAd(string templateId)
			{
				TemplateIds.Add(templateId);
				AdTypes.Add(NativeAdType.CustomTemplate);
				return this;
			}

			public Builder ForCustomNativeAd(string templateId, Action<CustomNativeTemplateAd, string> callback)
			{
				TemplateIds.Add(templateId);
				CustomNativeTemplateClickHandlers[templateId] = callback;
				AdTypes.Add(NativeAdType.CustomTemplate);
				return this;
			}

			public AdLoader Build()
			{
				return new AdLoader(this);
			}
		}

		private IAdLoaderClient adLoaderClient;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private EventHandler<AdFailedToLoadEventArgs> OnAdFailedToLoad__BackingField;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private EventHandler<CustomNativeEventArgs> OnCustomNativeTemplateAdLoaded__BackingField;

		public Dictionary<string, Action<CustomNativeTemplateAd, string>> CustomNativeTemplateClickHandlers { get; private set; }

		public string AdUnitId { get; private set; }

		public HashSet<NativeAdType> AdTypes { get; private set; }

		public HashSet<string> TemplateIds { get; private set; }

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

		public event EventHandler<CustomNativeEventArgs> OnCustomNativeTemplateAdLoaded
		{
			add
			{
				EventHandler<CustomNativeEventArgs> eventHandler = OnCustomNativeTemplateAdLoaded__BackingField;
				EventHandler<CustomNativeEventArgs> eventHandler2;
				do
				{
					eventHandler2 = eventHandler;
					eventHandler = Interlocked.CompareExchange(ref OnCustomNativeTemplateAdLoaded__BackingField, (EventHandler<CustomNativeEventArgs>)Delegate.Combine(eventHandler2, value), eventHandler);
				}
				while ((object)eventHandler != eventHandler2);
			}
			remove
			{
				EventHandler<CustomNativeEventArgs> eventHandler = OnCustomNativeTemplateAdLoaded__BackingField;
				EventHandler<CustomNativeEventArgs> eventHandler2;
				do
				{
					eventHandler2 = eventHandler;
					eventHandler = Interlocked.CompareExchange(ref OnCustomNativeTemplateAdLoaded__BackingField, (EventHandler<CustomNativeEventArgs>)Delegate.Remove(eventHandler2, value), eventHandler);
				}
				while ((object)eventHandler != eventHandler2);
			}
		}

		private AdLoader(Builder builder)
		{
			AdUnitId = string.Copy(builder.AdUnitId);
			CustomNativeTemplateClickHandlers = new Dictionary<string, Action<CustomNativeTemplateAd, string>>(builder.CustomNativeTemplateClickHandlers);
			TemplateIds = new HashSet<string>(builder.TemplateIds);
			AdTypes = new HashSet<NativeAdType>(builder.AdTypes);
			Type type = Type.GetType("GoogleMobileAds.GoogleMobileAdsClientFactory,Assembly-CSharp");
			MethodInfo method = type.GetMethod("BuildAdLoaderClient", BindingFlags.Static | BindingFlags.Public);
			adLoaderClient = (IAdLoaderClient)method.Invoke(null, new object[1] { this });
			Utils.CheckInitialization();
			adLoaderClient.OnCustomNativeTemplateAdLoaded += (object sender, CustomNativeEventArgs args) =>
			{
				OnCustomNativeTemplateAdLoaded__BackingField(this, args);
			};
			adLoaderClient.OnAdFailedToLoad += (object sender, AdFailedToLoadEventArgs args) =>
			{
				if (OnAdFailedToLoad__BackingField != null)
				{
					OnAdFailedToLoad__BackingField(this, args);
				}
			};
		}

		public void LoadAd(AdRequest request)
		{
			adLoaderClient.LoadAd(request);
		}
	}
}
