using NewBuildSystem;
using UnityEngine;

public static class CasterUtility
{
	public static PartData PartData(this string input)
	{
		if (AssetLoader.main.parts.ContainsKey(input))
		{
			return AssetLoader.main.parts[input];
		}
		Debug.LogError("Could not get part by name: " + input);
		return null;
	}

	public static Planet Planet(this string input)
	{
		return (!AssetLoader.main.planets.ContainsKey(input)) ? null : AssetLoader.main.planets[input];
	}

	public static Texture2D ToTexture(this string input)
	{
		return (!AssetLoader.main.textures.ContainsKey(input)) ? null : AssetLoader.main.textures[input];
	}

	public static HeightMap ToHeightmap(this string input)
	{
		return (!AssetLoader.main.heightmaps.ContainsKey(input)) ? null : AssetLoader.main.heightmaps[input];
	}
}
