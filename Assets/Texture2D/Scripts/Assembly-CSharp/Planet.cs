using System;
using System.Collections.Generic;
using UnityEngine;

public class Planet : MonoBehaviour
{
	public Planet parentBody;

	public Planet[] satellites;

	public double mass;

	public double SOI;

	public double meanMotion;

	public double orbitalVelocity;

	public double maxTerrainHeight;

	public Material terrainMaterial;

	public Material atmosphereMaterial;

	public Transform mapHolder;

	public LineRenderer orbitLine;

	public int orbitalDepth;

	public int satelliteIndex;

	public PlanetData data;

	public void SetupData(PlanetData planetData)
	{
		data = planetData;
		mass = data.BASE_DATA.gravity * Math.Pow(data.BASE_DATA.radius, 2.0);
		if (planetData.hasTerrain)
		{
			CreateTerrainMaterial(planetData.TERRAIN_DATA.TERRAIN_TEXTURE_DATA, Ref.main.terrainShader);
			maxTerrainHeight = data.TERRAIN_DATA.GetMaxTerrainHeight() + 100.0;
		}
		if (planetData.hasAtmosphere)
		{
			CreateAtmosphereMaterial(planetData.ATMOSPHERE_DATA, Ref.main.atmosphereShader);
		}
	}

	private void CreateAtmosphereMaterial(PlanetData.AtmosphereModule input, Shader atmosphereShader)
	{
		if (!(atmosphereShader == null))
		{
			Material material = new Material(atmosphereShader);
			input.GRADIENT.gradientTexture.ToTexture().wrapMode = TextureWrapMode.Clamp;
			material.SetTexture("_GradientTex", input.GRADIENT.gradientTexture.ToTexture());
			material.SetFloat("_GradientMultiplier", (float)((data.BASE_DATA.radius + input.GRADIENT.gradientHeight) / input.GRADIENT.gradientHeight) - 1f);
			material.SetTexture("_CouldTex", input.CLOUDS.cloudTexture.ToTexture());
			material.SetFloat("_CloudStartY", (float)((data.BASE_DATA.radius + (double)input.CLOUDS.startHeight + input.GRADIENT.gradientHeight) / input.GRADIENT.gradientHeight) - 1f);
			material.SetFloat("_CloudSizeY", (float)(data.BASE_DATA.radius + input.GRADIENT.gradientHeight) / (float)input.CLOUDS.height);
			material.SetFloat("_CloudSizeX", input.CLOUDS.repeatX);
			material.SetFloat("_Alpha", input.CLOUDS.alpha);
			atmosphereMaterial = material;
		}
	}

	private void CreateTerrainMaterial(PlanetData.TerrainModule.TerrainTexture input, Shader terrainShader)
	{
		Material material = new Material(terrainShader);
		material.SetTexture("_PlanetTexture", input.planetTexture.ToTexture());
		material.SetTexture("_TextureA", input.surfaceTextureA.ToTexture());
		material.SetTexture("_TextureB", input.surfaceTextureB.ToTexture());
		material.SetTexture("_TextureTerrain", input.terrainTexture.ToTexture());
		material.SetVector("_RepeatA", ToRepeat(input.surfaceTextureSizeA.ToVector2()));
		material.SetVector("_RepeatB", ToRepeat(input.surfaceTextureSizeB.ToVector2()));
		material.SetVector("_RepeatTerrain", ToRepeat(input.terrainTextureSize.ToVector2()));
		material.SetFloat("_SurfaceSize", (float)data.BASE_DATA.radius / input.surfaceLayerSize);
		material.SetFloat("_Min", input.minFade);
		material.SetFloat("_Max", input.maxFade);
		material.SetFloat("_ShadowIntensity", input.shadowIntensity);
		material.SetFloat("_ShadowSize", (float)data.BASE_DATA.radius / input.shadowHeight);
		material.SetFloat("_PlanetTextureCutout", input.planetTextureCutout);
		material.SetColor("_Fog", Color.clear);
		terrainMaterial = material;
	}

	private Vector2 ToRepeat(Vector2 size)
	{
		int num = (int)((float)data.BASE_DATA.radius * (float)Math.PI * 2f / size.x);
		return new Vector2(num, (float)data.BASE_DATA.radius / size.y);
	}

	public void SetupInteractions(AssetLoader main)
	{
		if (data.hasOrbitData && main.planets.Keys != null && main.planets.ContainsKey(data.ORBIT_DATA.parent))
		{
			parentBody = main.planets[data.ORBIT_DATA.parent];
			double period = Kepler.GetPeriod(0.0, data.ORBIT_DATA.orbitHeight, parentBody.mass);
			meanMotion = Math.PI * -2.0 / period;
			orbitalVelocity = data.ORBIT_DATA.orbitHeight * meanMotion;
			SOI = data.ORBIT_DATA.orbitHeight * Math.Pow(mass / parentBody.mass, 0.4) * data.ORBIT_DATA.multiplierSOI;
		}
		else
		{
			SOI = double.PositiveInfinity;
		}
		satellites = GetSatellites(main);
	}

	private Planet[] GetSatellites(AssetLoader main)
	{
		List<Planet> list = new List<Planet>();
		foreach (Planet value in main.planets.Values)
		{
			if (value.data.ORBIT_DATA != null && value.data.ORBIT_DATA.parent == data.BASE_DATA.name)
			{
				list.Add(value);
			}
		}
		list.Sort(SortByOrbitalDistance);
		return list.ToArray();
	}

	private static int SortByOrbitalDistance(Planet a, Planet b)
	{
		return (a.data.ORBIT_DATA.orbitHeight > b.data.ORBIT_DATA.orbitHeight) ? 1 : (-1);
	}

	public Double3 GetPosOut(double time)
	{
		double num = time * meanMotion;
		return new Double3(Math.Cos(num) * data.ORBIT_DATA.orbitHeight, Math.Sin(num) * data.ORBIT_DATA.orbitHeight);
	}

	public Double3 GetVelOut(double time)
	{
		double num = time * meanMotion;
		return new Double3(0.0 - Math.Sin(num), Math.Cos(num)) * orbitalVelocity;
	}

	public Double3 GetSolarSystemPosition(double time)
	{
		Double3 result = default(Double3);
		Planet planet = this;
		while (planet.data.hasOrbitData)
		{
			result += planet.GetPosOut(time);
			planet = planet.parentBody;
		}
		return result;
	}

	public int GetDepth()
	{
		int num = 0;
		Planet planet = this;
		while (planet.data.hasOrbitData)
		{
			num++;
			planet = planet.parentBody;
		}
		return num;
	}

	public int GetSatelliteIndex()
	{
		if (!data.hasOrbitData)
		{
			return 0;
		}
		return new List<Planet>(parentBody.satellites).IndexOf(this);
	}
}
