using System;
using System.Collections.Generic;
using Assets.Scripts.Addons.FileStructure;
using NewBuildSystem;
using Sirenix.OdinInspector;
using UnityEngine;

public class AssetLoader : MonoBehaviour
{
	public static AssetLoader main;

	public Dictionary<string, PartData> parts = new Dictionary<string, PartData>();

	public Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();

	public Dictionary<string, HeightMap> heightmaps = new Dictionary<string, HeightMap>();

	public Dictionary<string, Planet> planets = new Dictionary<string, Planet>();

	public int maxOrbitalDepth;

	private int GetMaxOrbitalDepth()
	{
		int num = 0;
		foreach (Planet value in planets.Values)
		{
			num = Mathf.Max(value.orbitalDepth, num);
		}
		return num;
	}

	public static void Initialize(bool reload)
	{
		if (main != null)
		{
			if (!reload)
			{
				return;
			}
			UnityEngine.Object.Destroy(main.gameObject);
		}
		main = new GameObject("Asset Loader").AddComponent<AssetLoader>();
		main.LoadAssets();
		UnityEngine.Object.DontDestroyOnLoad(main.gameObject);
	}

	[Button]
	public void LoadAssets()
	{
		textures = LoadAssetType<Texture2D>("Textures", false);
		LoadTextures();
		parts = LoadAssetType<PartData>("Parts", false);
		LoadOldPartNames();
		heightmaps = LoadAssetType<HeightMap>("Height Maps", true);
		LoadPlanets();
	}

	[Button]
	public void LoadPlanets()
	{
		foreach (Planet value in planets.Values)
		{
			UnityEngine.Object.Destroy(value.gameObject);
		}
		planets = new Dictionary<string, Planet>();
		planets = LoadPlanetsFromFile();
		foreach (Planet value2 in planets.Values)
		{
			value2.SetupInteractions(this);
		}
		foreach (Planet value3 in planets.Values)
		{
			value3.orbitalDepth = value3.GetDepth();
			value3.satelliteIndex = value3.GetSatelliteIndex();
		}
		maxOrbitalDepth = GetMaxOrbitalDepth();
	}

	private void LoadOldPartNames()
	{
		List<string> list = new List<string>();
		List<PartData> list2 = new List<PartData>();
		foreach (PartData value in parts.Values)
		{
			if (value.oldName == null)
			{
				continue;
			}
			string[] oldName = value.oldName;
			foreach (string text in oldName)
			{
				if (!parts.ContainsKey(text))
				{
					list.Add(text);
					list2.Add(value);
				}
			}
		}
		for (int j = 0; j < list.Count; j++)
		{
			parts.Add(list[j], list2[j]);
		}
	}

	private static Dictionary<string, T> LoadAssetType<T>(string path, bool fromJson)
	{
		UnityEngine.Object[] array = Resources.LoadAll(path, (!fromJson) ? typeof(T) : typeof(TextAsset));
		Dictionary<string, T> dictionary = new Dictionary<string, T>();
		UnityEngine.Object[] array2 = array;
		foreach (UnityEngine.Object obj in array2)
		{
			if (fromJson)
			{
				dictionary.Add(obj.name, JsonUtility.FromJson<T>(((TextAsset)obj).text));
			}
			else
			{
				dictionary.Add(obj.name, (T)(object)obj);
			}
		}
		return dictionary;
	}

	private Dictionary<string, Planet> LoadPlanetsFromFile()
	{
		Dictionary<string, Planet> planetList = new Dictionary<string, Planet>();
		ObjectFile[] array = ((!Saving.PlanetDataLocation().PathExists()) ? new ObjectFile[0] : Saving.PlanetDataLocation().FilesAtLocation());
		foreach (ObjectFile objectFile in array)
		{
			LoadPlanet(objectFile.FileName(), objectFile.ReadContent(), ref planetList);
		}
		// Ref.controller 只在 Game 场景有效（编辑器里 planetList 非空时会空引用），故加 null 判定。
		if (planetList.Count > 0 && Ref.controller != null && planetList.ContainsKey(Ref.controller.startAdress))
		{
			return planetList;
		}
		Dictionary<string, TextAsset> dictionary = LoadAssetType<TextAsset>("Planets", false);
		foreach (TextAsset value in dictionary.Values)
		{
			LoadPlanet(value.name, value.text, ref planetList);
		}
		return planetList;
	}

	private void LoadTextures()
	{
		if (!Saving.TextureDataLocation().PathExists())
		{
			Saving.TextureDataLocation().CreatePath();
		}
		if (Saving.TextureDataLocation().PathExists())
		{
			ObjectFile[] array = Saving.TextureDataLocation().FilesAtLocation();
			foreach (ObjectFile objectFile in array)
			{
				WWW wWW = new WWW("file://" + objectFile.path);
				textures.Add(objectFile.FileName(), wWW.texture);
			}
		}
	}

	private void LoadPlanet(string planetName, string jsonData, ref Dictionary<string, Planet> planetList)
	{
		try
		{
			PlanetData planetData = PlanetData.FromJson(jsonData);
			Transform transform = new GameObject(planetName + " LoadedData").transform;
			transform.parent = base.transform;
			Planet planet = transform.gameObject.AddComponent<Planet>();
			planet.SetupData(planetData);
			planetList.Add(planetName, planet);
		}
		catch (Exception)
		{
			MenuManager.ShowMsg("Error Loading Planet File: " + planetName);
		}
	}

	[Button]
	public static void ExportPlanets()
	{
		if (Saving.PlanetDataLocation().PathExists())
		{
			return;
		}
		Saving.PlanetDataLocation().CreatePath();
		foreach (TextAsset value in LoadAssetType<TextAsset>("Planets", false).Values)
		{
			ObjectFile objectFile = Saving.PlanetDataLocation().ToFile();
			objectFile.Extend(value.name + ".txt");
			objectFile.FlushJson(value.text);
		}
	}
}
