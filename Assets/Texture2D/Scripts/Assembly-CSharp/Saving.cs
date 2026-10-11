using System.Collections.Generic;
using Assets.Scripts.Addons.FileStructure;
using Assets.Scripts.Save;
using NewBuildSystem;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Saving : MonoBehaviour
{
	public enum SaveKey
	{
		ToLaunch = 0,
		PersistantGameSave = 1,
		Settings = 2,
		BuildQuicksaves = 3,
		GameQuicksaves = 4,
		CrashInfo = 5
	}

	public enum SettingKey
	{
		seenBuildInstructions = 0,
		seenGameInstructions = 1,
		seenMapInstructions = 2,
		seenTimewarpInstructions = 3,
		disableMusic = 4,
		disableSound = 5,
		disableAutoRotate = 6,
		disable60fps = 7,
		hasPartDLC = 8,
		hasHackedDLC = 9,
		infiniteFuel = 10,
		noDrag = 11,
		unbreakableParts = 12,
		noGravity = 13,
		askedToRate1 = 14,
		askedToRate2 = 15,
		hasRated = 16,
		transferComplete = 17
	}

	public TouchScreenKeyboard touchKeyboard;

	public Text inputField;

	public GameObject confirmMenuHolder;

	[FoldoutGroup("Events", 0)]
	public UnityEvent onOpenSaveMenu;

	[FoldoutGroup("Events", 0)]
	public UnityEvent onOpenLoadMenu;

	[FoldoutGroup("Events", 0)]
	public UnityEvent onCloseMenus;

	[ShowInInspector]
	private bool displayingLoadMenu;

	public Transform saveFilePrefab;

	public Transform saveFilesHolder;

	public List<BoxCollider2D> saveFilesPool = new List<BoxCollider2D>();

	[ShowInInspector]
	private int selectedSave;

	[ShowInInspector]
	private int page;

	private const int savesPerPage = 6;

	private const int saveWidth = 430;

	private const int saveHeight = -70;

	public object SavingReference { get; private set; }

	public static string BasePath()
	{
		return (!Application.isMobilePlatform && !Application.isEditor) ? "/../Saving" : "Saving";
	}

	public static ObjectPath WorldSavingLocation()
	{
		return new ObjectPath(true, BasePath(), "Worlds");
	}

	public static ObjectPath BlueprintLocation()
	{
		return new ObjectPath(true, BasePath(), "Blueprints");
	}

	public static ObjectPath ResourcesLocation()
	{
		ObjectPath objectPath = new ObjectPath(true, (!Application.isEditor) ? "Resources" : "Mod Data");
		objectPath.CreatePath();
		return objectPath;
	}

	public static ObjectPath PlanetDataLocation()
	{
		return new ObjectPath(true, (!Application.isEditor) ? "Resources" : "Mod Data", "Planet Data");
	}

	public static ObjectPath TextureDataLocation()
	{
		return new ObjectPath(true, (!Application.isEditor) ? "Resources" : "Mod Data", "Textures");
	}

	private void Update()
	{
		if (touchKeyboard != null && inputField.text != touchKeyboard.text)
		{
			inputField.text = touchKeyboard.text;
		}
	}

	public void StartSaveProcess()
	{
		if (Ref.currentScene == Ref.SceneType.Build)
		{
			if (!Build.main.buildGrid.HasAnyParts())
			{
				MenuManager.ShowMsg("Cannot save a empty build");
				return;
			}
		}
		else if (Ref.mainVessel == null)
		{
			MenuManager.ShowMsg("Cannot save while not controlling a rocket");
			return;
		}
		OpenKeyboard();
		onOpenSaveMenu.Invoke();
		MenuManager.OpenMenuBack(true);
	}

	public void OpenKeyboard()
	{
		TouchScreenKeyboard.hideInput = true;
		touchKeyboard = TouchScreenKeyboard.Open(inputField.text, TouchScreenKeyboardType.EmailAddress, false, false, false, true, "Save Name");
	}

	public void CreateSave()
	{
		string text = ((touchKeyboard == null) ? string.Empty : touchKeyboard.text);
		MenuManager.ShowMsg("Saving...");
		AdManager.ShowFullscreen();
		if (Ref.currentScene == Ref.SceneType.Build)
		{
			Build.Blueprint.CreateBlueprint(text, Build.main.GetBlueprint());
		}
		else
		{
			World worldByName = World.GetWorldByName();
			worldByName.CreateQuicksave(text);
		}
		CloseMenus();
	}

	public void StartLoadProcess()
	{
		onOpenLoadMenu.Invoke();
		displayingLoadMenu = true;
		MenuManager.OpenMenuBack(true);
		LoadSaveFiles();
		MovePage(1000);
		SetSelectedSave(-1);
	}

	private void LoadSaveFiles()
	{
		List<string> list = ((Ref.currentScene != Ref.SceneType.Build) ? World.GetWorldByName().GetQuicksavesList() : Build.Blueprint.GetBlueprintNames());
		while (list.Count > saveFilesPool.Count)
		{
			Transform transform = Object.Instantiate(saveFilePrefab, saveFilesHolder);
			transform.localPosition = new Vector3(saveFilesPool.Count / 6 * 430, saveFilesPool.Count % 6 * -70);
			saveFilesPool.Add(transform.GetComponent<BoxCollider2D>());
		}
		for (int i = 0; i < list.Count || i < saveFilesPool.Count; i++)
		{
			bool flag = list.Count > i;
			if (flag)
			{
				saveFilesPool[i].GetComponentInChildren<Text>().text = list[i];
			}
			if (saveFilesPool[i].gameObject.activeSelf != flag)
			{
				saveFilesPool[i].gameObject.SetActive(flag);
			}
		}
	}

	public void MovePage(int change)
	{
		List<string> list = ((Ref.currentScene != Ref.SceneType.Build) ? World.GetWorldByName().GetQuicksavesList() : Build.Blueprint.GetBlueprintNames());
		page = Mathf.Clamp(page + change, 0, (list.Count - 1) / 6);
		saveFilesHolder.localPosition = new Vector2(page * 430 * -1, saveFilesHolder.localPosition.y);
	}

	public void OnClickUI(Vector2 pixelPos)
	{
		if (!displayingLoadMenu)
		{
			return;
		}
		GameObject gameObject = Ref.inputController.PointCastUI(pixelPos, saveFilesPool.ToArray());
		for (int i = 0; i < saveFilesPool.Count; i++)
		{
			if (gameObject == saveFilesPool[i].gameObject)
			{
				SetSelectedSave(i);
				break;
			}
		}
	}

	public void SetSelectedSave(int newSelectedSave)
	{
		if (selectedSave != -1)
		{
			saveFilesPool[selectedSave].transform.GetChild(0).gameObject.SetActive(false);
		}
		if (newSelectedSave != -1)
		{
			saveFilesPool[newSelectedSave].transform.GetChild(0).gameObject.SetActive(true);
			MovePage(-1000);
			MovePage(newSelectedSave / 6);
		}
		selectedSave = newSelectedSave;
	}

	public void LoadSelectedSave()
	{
		if (selectedSave != -1)
		{
			MenuManager.ShowMsg("Loading...");
			AdManager.ShowFullscreen();
			string selected = GetSelected();
			if (Ref.currentScene == Ref.SceneType.Build)
			{
				Build.main.LoadSave(Build.Blueprint.LoadBlueprint(selected));
			}
			else
			{
				GameSaver.LoadGame(World.GetWorldByName().LoadQuickSave(selected));
			}
			CloseMenus();
		}
	}

	public void DeleteSelectedSave()
	{
		if (selectedSave != -1)
		{
			string selected = GetSelected();
			if (Ref.currentScene == Ref.SceneType.Build)
			{
				Build.Blueprint.DeleteBlueprint(selected);
			}
			else
			{
				World.GetWorldByName().DeleteQuickSave(selected);
			}
			SetSelectedSave(-1);
			LoadSaveFiles();
		}
	}

	private string GetSelected()
	{
		if (Ref.currentScene == Ref.SceneType.Build)
		{
			return Build.Blueprint.GetBlueprintNames()[selectedSave];
		}
		return World.GetWorldByName().GetQuicksavesList()[selectedSave];
	}

	public void CloseMenus()
	{
		onCloseMenus.Invoke();
		displayingLoadMenu = false;
		MenuManager.CloseMenuBack();
		if (touchKeyboard != null)
		{
			touchKeyboard.active = false;
		}
	}

	public static void SaveSetting(SettingKey settingKey, bool value)
	{
		PlayerPrefs.SetInt(settingKey.ToString(), value ? 1 : 0);
		PlayerPrefs.Save();
	}

	public static bool LoadSetting(SettingKey settingKey)
	{
		return PlayerPrefs.GetInt(settingKey.ToString(), 0) == 1;
	}

	public static bool HasSetting(SettingKey settingKey)
	{
		return PlayerPrefs.HasKey(settingKey.ToString());
	}

	public static void ToggleSetting(SettingKey settingKey)
	{
		SaveSetting(settingKey, !LoadSetting(settingKey));
	}

	// ==== 帧率设置 (新增: 支持 30 / 60 / 90 / 120) ====
	public static readonly int[] FpsOptions = new int[4] { 30, 60, 90, 120 };

	// 读取当前帧率; 兼容旧存档(该键过去只存 0/1: 0=60fps, 1=30fps)
	public static int LoadFps()
	{
		int value = PlayerPrefs.GetInt(SettingKey.disable60fps.ToString(), 0);
		if (value <= 1)
		{
			return (value == 1) ? 30 : 60;
		}
		return value;
	}

	public static void SaveFps(int fps)
	{
		PlayerPrefs.SetInt(SettingKey.disable60fps.ToString(), fps);
		PlayerPrefs.Save();
	}

	// 在 30 -> 60 -> 90 -> 120 之间循环
	public static int NextFps(int current)
	{
		int index = 0;
		for (int i = 0; i < FpsOptions.Length; i++)
		{
			if (FpsOptions[i] == current)
			{
				index = i;
				break;
			}
		}
		return FpsOptions[(index + 1) % FpsOptions.Length];
	}
}
