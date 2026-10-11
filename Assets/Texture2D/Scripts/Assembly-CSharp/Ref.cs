using System;
using System.Collections.Generic;
using Assets.Scripts.Save;
using JMtech.Main;
using NewBuildSystem;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Ref : MonoBehaviour
{
	public enum SceneType
	{
		MainMenu = 0,
		Build = 1,
		Game = 2
	}

	[Serializable]
	public class SaveTranfer
	{
		[Serializable]
		public class BlueprintsOld
		{
			public List<Build.Blueprint> buildSaves = new List<Build.Blueprint>();
		}

		[Serializable]
		public class QuicksavesOld
		{
			public List<GameSaver.Quicksave> quicksaves;
		}

		public static void Transfer()
		{
			if (Saving.LoadSetting(Saving.SettingKey.transferComplete))
			{
				return;
			}
			Saving.SaveSetting(Saving.SettingKey.transferComplete, true);
			World worldByName = World.GetWorldByName();
			foreach (Build.Blueprint buildSafe in LoadBlueprints().buildSaves)
			{
				Build.Blueprint.CreateBlueprint(buildSafe.saveName, buildSafe);
			}
			foreach (GameSaver.Quicksave quicksafe in LoadQuicksavesOld().quicksaves)
			{
				worldByName.CreateQuicksave(quicksafe.saveName + " (Old)", quicksafe);
			}
			GameSaver.Quicksave quicksave = JsonUtility.FromJson<GameSaver.Quicksave>(LoadJsonString(Saving.SaveKey.PersistantGameSave));
			worldByName.CreateQuicksave("Persistant (Old)", quicksave);
		}

		public static BlueprintsOld LoadBlueprints()
		{
			string text = LoadJsonString(Saving.SaveKey.BuildQuicksaves);
			return (!(text == string.Empty)) ? JsonUtility.FromJson<BlueprintsOld>(text) : new BlueprintsOld();
		}

		public static QuicksavesOld LoadQuicksavesOld()
		{
			string text = LoadJsonString(Saving.SaveKey.GameQuicksaves);
			return (!(text == string.Empty)) ? JsonUtility.FromJson<QuicksavesOld>(text) : new QuicksavesOld();
		}
	}

	public static Ref main;

	[BoxGroup("All Scenes", true, false, 0)]
	[Space]
	public Camera Camera;

	[BoxGroup("All Scenes", true, false, 0)]
	public InputController InputController;

	public static InputController inputController;

	[BoxGroup("All Scenes", true, false, 0)]
	[Space]
	public SceneType CurrentScene;

	public static SceneType currentScene;

	public static SceneType lastScene;

	[BoxGroup("Game Scene", true, false, 0)]
	[Space]
	public Controller Controller;

	public static Controller controller;

	[BoxGroup("Game Scene", true, false, 0)]
	public PlanetManager PlanetManager;

	public static PlanetManager planetManager;

	[BoxGroup("Game Scene", true, false, 0)]
	public Map Map;

	public static Map map;

	public static Vessel mainVessel;

	public static bool timeWarping;

	public static bool mapView;

	public static double mainVesselHeight;

	public static double mainVesselTerrainHeight;

	public static float mainVesselAngleToPlanet;

	public static Double3 positionOffset;

	public static Double3 velocityOffset;

	[BoxGroup("Some Scenes", true, false, 0)]
	[Space]
	public Saving Saving;

	public static Saving saving;

	public GameObject FuelBars;

	public static int connectionCheckId;

	public static bool openTutorial;

	public static bool openSalePage;

	public static bool loadLaunchedRocket;

	public AudioListener MainAudioListener;

	public static AudioListener mainAudioListener;

	public static bool hasPartsExpansion;

	public static bool infiniteFuel;

	public static bool noDrag;

	public static bool unbreakableParts;

	public static bool noGravity;

	public Shader terrainShader;

	public Shader atmosphereShader;

	public static int explosionCount;

	[BoxGroup("All Scenes", true, false, 0)]
	[ShowInInspector]
	[ReadOnly]
	private SceneType LastScene
	{
		get
		{
			return lastScene;
		}
		set
		{
		}
	}

	[BoxGroup("Game Scene", true, false, 0)]
	[ShowInInspector]
	[Title("", null, TitleAlignments.Left, false, false)]
	private Vessel MainVessel
	{
		get
		{
			return mainVessel;
		}
		set
		{
		}
	}

	[BoxGroup("Game Scene", true, false, 0)]
	[ShowInInspector]
	private bool TimeWarping
	{
		get
		{
			return timeWarping;
		}
		set
		{
		}
	}

	[BoxGroup("Game Scene", true, false, 0)]
	[ShowInInspector]
	private double MainVesselHeight
	{
		get
		{
			return mainVesselHeight;
		}
		set
		{
		}
	}

	[BoxGroup("Game Scene", true, false, 0)]
	[ShowInInspector]
	private double MainVTerrainHeight
	{
		get
		{
			return mainVesselTerrainHeight;
		}
		set
		{
		}
	}

	[BoxGroup("Game Scene", true, false, 0)]
	[ShowInInspector]
	private float MainVAngleToPlanet
	{
		get
		{
			return mainVesselAngleToPlanet;
		}
		set
		{
		}
	}

	[BoxGroup("Game Scene", true, false, 0)]
	[ShowInInspector]
	private Double3 PositionOffset
	{
		get
		{
			return positionOffset;
		}
		set
		{
			positionOffset = value;
		}
	}

	[BoxGroup("Game Scene", true, false, 0)]
	[ShowInInspector]
	private Double3 VelocityOffset
	{
		get
		{
			return velocityOffset;
		}
		set
		{
			velocityOffset = value;
		}
	}

	public static void LoadScene(SceneType sceneToLoad)
	{
		lastScene = currentScene;
		SceneManager.LoadScene(sceneToLoad.ToString(), LoadSceneMode.Single);
	}

	public static int GetFigure(double value)
	{
		int num = 0;
		double num2 = 1.0;
		while (value >= num2)
		{
			num2 *= 10.0;
			num++;
		}
		return num;
	}

	public static string GetTimeString(double time)
	{
		int num = (int)(time / 86400.0);
		int num2 = (int)(time / 3600.0) % 24;
		int num3 = (int)(time / 60.0) % 60;
		int num4 = (int)time % 60;
		string text = string.Empty;
		if (num > 0)
		{
			text = text + num + "d ";
			if (num2 < 10)
			{
				text += "0";
			}
		}
		if (num2 > 0 || num > 0)
		{
			text = text + num2 + "h ";
			if (num3 < 10)
			{
				text += "0";
			}
		}
		if (num3 > 0 || num2 > 0 || num > 0)
		{
			text = text + num3 + "m ";
			if (num4 < 10)
			{
				text += "0";
			}
		}
		return text + num4 + "s";
	}

	private void Awake()
	{
		main = this;
		hasPartsExpansion = true;
		infiniteFuel = Saving.LoadSetting(Saving.SettingKey.infiniteFuel);
		noDrag = Saving.LoadSetting(Saving.SettingKey.noDrag);
		unbreakableParts = Saving.LoadSetting(Saving.SettingKey.unbreakableParts);
		noGravity = Saving.LoadSetting(Saving.SettingKey.noGravity);
		inputController = InputController;
		saving = Saving;
		currentScene = CurrentScene;
		lastScene = LastScene;
		controller = Controller;
		planetManager = PlanetManager;
		map = Map;
		GameTracker.Initialize();
		SaveTranfer.Transfer();
		AssetLoader.Initialize(false);
		if (AdManager.ShowAds)
		{
			AdManager.InitializeAds();
			AdManager.LoadFullscreenAd();
			if (currentScene != SceneType.Game)
			{
			}
		}
	}

	private void ShowBannerAd()
	{
		AdManager.ShowBanner();
	}

	public static void Label(float x, float y, float width, float height, Texture2D texture)
	{
		GUI.Label(new Rect(x, y, width, height), texture);
	}

	public static void Box(float x, float y, float width, float height, Texture2D texture)
	{
		GUI.Box(new Rect(x, y, width, height), texture);
	}

	public static void SaveJsonString(string jsonData, Saving.SaveKey fileName)
	{
		PlayerPrefs.SetString(fileName.ToString(), jsonData);
		PlayerPrefs.Save();
	}

	public static string LoadJsonString(Saving.SaveKey fileName)
	{
		return PlayerPrefs.GetString(fileName.ToString(), string.Empty);
	}

	public static bool FileExists(Saving.SaveKey fileName)
	{
		return PlayerPrefs.HasKey(fileName.ToString());
	}

	public static void DeleteFile(Saving.SaveKey fileName)
	{
		if (FileExists(fileName))
		{
			PlayerPrefs.DeleteKey(fileName.ToString());
		}
	}

	public static float GetSizeOfWord(TextMesh text, string word)
	{
		float num = 0f;
		foreach (char ch in word)
		{
			CharacterInfo info;
			text.font.GetCharacterInfo(ch, out info, text.fontSize);
			num += (float)info.advance;
		}
		return num;
	}
}
