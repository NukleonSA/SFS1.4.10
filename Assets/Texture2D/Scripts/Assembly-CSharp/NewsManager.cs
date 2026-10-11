using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class NewsManager : MonoBehaviour
{
	[SerializeField]
	private Text newsTitle;

	[SerializeField]
	private Text newsText;

	public GameObject newsHeader;

	private void Start()
	{
		try
		{
			string response = GetResponse("<!news>(.*?)<!news>", "https://jmnet.one/sfs/forum/index.php?threads/sfsnews-page-for-game.748/");
			response = response.Replace("<!news>", string.Empty);
			string[] array = response.Split(':');
			StringBuilder stringBuilder = new StringBuilder();
			string[] array2 = array[1].Split(';');
			foreach (string text in array2)
			{
				stringBuilder.Append(text + Environment.NewLine);
			}
			int num = stringBuilder.ToString().LastIndexOf(Environment.NewLine);
			if (num >= 0)
			{
				stringBuilder.Remove(num, Environment.NewLine.Length);
			}
			newsTitle.text = array[0];
			newsText.text = stringBuilder.ToString();
			if (!(PlayerPrefs.GetString("newsTitle") == newsTitle.text))
			{
				PlayerPrefs.SetString("newsTitle", newsTitle.text);
				newsHeader.SetActive(true);
			}
		}
		catch (Exception ex)
		{
			newsTitle.text = "Something went wrong...";
			newsText.text = ex.Message;
		}
	}

	public static string GetResponse(string pattern, string address)
	{
		try
		{
			WebClient webClient = new WebClient();
			ServicePointManager.ServerCertificateValidationCallback = RemoteCertificateValidationCallback;
			string input = webClient.DownloadString(address);
			Regex regex = new Regex(pattern);
			Match match = regex.Match(input);
			return match.Value;
		}
		catch (Exception ex)
		{
			Debug.Log(ex.Message);
		}
		return null;
	}

	private static bool RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
	{
		bool result = true;
		if (sslPolicyErrors != SslPolicyErrors.None)
		{
			for (int i = 0; i < chain.ChainStatus.Length; i++)
			{
				if (chain.ChainStatus[i].Status != X509ChainStatusFlags.RevocationStatusUnknown)
				{
					chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
					chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
					chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
					chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
					if (!chain.Build((X509Certificate2)certificate))
					{
						result = false;
						break;
					}
				}
			}
		}
		return result;
	}
}
