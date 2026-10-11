using System;
using System.Collections.Generic;
using Assets.Scripts.Parsers;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class PlanetData
{
	[Serializable]
	public class BasicModule
	{
		[Header("Basic data")]
		public string name;

		public double radius = -1.0;

		public double gravity = -1.0;

		public double timewarpHeight = -1.0;

		[Header("Map Data")]
		public SerializableColor mapColor = new SerializableColor(Color.gray);

		public int mapResolution;

		public double hideNameMultiplier;
	}

	[Serializable]
	public class SerializableColor
	{
		[CustomValueDrawer("a")]
		public float r = -1f;

		[HideInInspector]
		public float g = -1f;

		[HideInInspector]
		public float b = -1f;

		public SerializableColor(Color input)
		{
			r = input.r;
			g = input.g;
			b = input.b;
		}

		public Color GetColor()
		{
			return new Color(r, g, b, 1f);
		}
	}

	[Serializable]
	public struct MyVector2
	{
		[HorizontalGroup(0f, 0, 0, 0)]
		[LabelWidth(20f)]
		public float x;

		[HorizontalGroup(0f, 0, 0, 0)]
		[LabelWidth(20f)]
		public float y;

		public Vector2 ToVector2()
		{
			return new Vector2(x, y);
		}
	}

	[Serializable]
	public class TerrainModule
	{
		[Serializable]
		public class DetailLevel
		{
			public double loadDistance;

			public float verticeSize;
		}

		[Serializable]
		public class TerrainTexture
		{
			[Space]
			public string planetTexture = "None";

			public float planetTextureCutout = -1f;

			[Space]
			public string surfaceTextureA = "None";

			[InlineProperty]
			public MyVector2 surfaceTextureSizeA = new MyVector2
			{
				x = -1f,
				y = -1f
			};

			[Space]
			public string surfaceTextureB = "None";

			[InlineProperty]
			public MyVector2 surfaceTextureSizeB = new MyVector2
			{
				x = -1f,
				y = -1f
			};

			[Space]
			public string terrainTexture = "None";

			[InlineProperty]
			public MyVector2 terrainTextureSize = new MyVector2
			{
				x = -1f,
				y = -1f
			};

			[Space]
			public float surfaceLayerSize = -1f;

			[Space]
			public float minFade = -1f;

			public float maxFade = -1f;

			[Space]
			public float shadowIntensity = -1f;

			public float shadowHeight = -1f;
		}

		[Space]
		public TerrainTexture TERRAIN_TEXTURE_DATA;

		[LabelText("Terrain Formula")]
		public string[] terrainFromula;

		[LabelText("Texture Formula")]
		public string[] textureFormula;

		[TableList]
		[Space]
		public DetailLevel[] DETAIL_LEVELS;

		public PlanetManager.TerrainPoints GetTerrainPoints(double from, double deltaAngle, int pointCountRequest, double LOD, bool offset, Planet planet)
		{
			double num = from * Math.PI * 2.0;
			Double3 @double = ((!offset) ? Double3.zero : new Double3((int)(Math.Cos(num) * planet.data.BASE_DATA.radius / 1000.0) * 1000, (int)(Math.Sin(num) * planet.data.BASE_DATA.radius / 1000.0) * 1000));
			PlanetManager.TerrainPoints terrainPoints = new PlanetManager.TerrainPoints(pointCountRequest + 1);
			float[] terrainSamples = ShapeScript.GetTerrainSamples(from, deltaAngle / Math.PI / 2.0, pointCountRequest + 1, planet.data.TERRAIN_DATA.terrainFromula);
			float[] terrainSamples2 = ShapeScript.GetTerrainSamples(from, deltaAngle / Math.PI / 2.0, pointCountRequest, planet.data.TERRAIN_DATA.textureFormula);
			for (int i = 0; i < pointCountRequest; i++)
			{
				double num2 = num + (double)i * deltaAngle;
				terrainPoints.points[i + 1] = (new Double3(Math.Cos(num2) * ((double)terrainSamples[i] + planet.data.BASE_DATA.radius), Math.Sin(num2) * ((double)terrainSamples[i] + planet.data.BASE_DATA.radius)) - @double).toVector3;
				float y = terrainSamples2[i];
				terrainPoints.uvOthers[i + 1] = new Vector2((terrainSamples[i] - terrainSamples[i + 1]) * (float)(LOD * 1E-06), y);
			}
			terrainPoints.points[0] = -@double.toVector3;
			return terrainPoints;
		}

		public static double GetTerrainSampleAtAngle(double angle, Planet planet)
		{
			return (!planet.data.hasTerrain) ? 0f : ShapeScript.GetTerrainSamples(angle / 360.0, 0.0, 1, planet.data.TERRAIN_DATA.terrainFromula)[0];
		}

		public double GetMaxTerrainHeight()
		{
			return Mathf.Max(ShapeScript.GetTerrainSamples(0.0, 0.001, 1000, terrainFromula));
		}
	}

	[Serializable]
	public class AtmosphereModule
	{
		[Serializable]
		public class Physics
		{
			public double height = -1.0;

			public double density = -1.0;

			public double curve = -1.0;
		}

		[Serializable]
		public class Gradient
		{
			public int positionZ = -1;

			public double gradientHeight = -1.0;

			public string gradientTexture = "None";
		}

		[Serializable]
		public class Clouds
		{
			public string cloudTexture = "None";

			public int startHeight = -1;

			public int height = -1;

			public float repeatX;

			public float alpha;

			public float cloudVelocity;
		}

		[Serializable]
		public class SerializableGradient
		{
			[Serializable]
			public class Key
			{
				[CustomValueDrawer("DrawKey")]
				public float r = -1f;

				[HideInInspector]
				public float g = -1f;

				[HideInInspector]
				public float b = -1f;

				[HideInInspector]
				public float a = -1f;

				public float distance = -1f;

				public Color GetColor()
				{
					return new Color(r, g, b, a);
				}
			}

			public Key[] keys;

			public Color Evaluate(float distance)
			{
				if (keys == null || keys.Length == 0)
				{
					return Color.clear;
				}
				if (distance >= keys[keys.Length - 1].distance)
				{
					return keys[keys.Length - 1].GetColor();
				}
				if (distance <= keys[0].distance)
				{
					return keys[0].GetColor();
				}
				int i;
				for (i = 0; distance > keys[i + 1].distance; i++)
				{
				}
				return Color.Lerp(keys[i].GetColor(), keys[i + 1].GetColor(), Mathf.InverseLerp(keys[i].distance, keys[i + 1].distance, distance));
			}
		}

		public Physics PHYSICS;

		public Gradient GRADIENT;

		public Clouds CLOUDS;

		public SerializableGradient FOG;
	}

	[Serializable]
	public class ColorGradingGradient
	{
		[Serializable]
		public class Key
		{
			public float height;

			public float shadowIntensity = 1.65f;

			public float hueShift;

			public float saturation = 1f;

			public float contrast = 1.1f;

			public float red = 1f;

			public float green = 1f;

			public float blue = 1f;

			public Key(float shadowIntensity, float hueShift, float saturation, float contrast, float red, float green, float blue)
			{
				this.shadowIntensity = shadowIntensity;
				this.hueShift = hueShift;
				this.saturation = saturation;
				this.contrast = contrast;
				this.red = red;
				this.green = green;
				this.blue = blue;
			}

			[Button]
			private void Default()
			{
				shadowIntensity = 1.65f;
				hueShift = 0f;
				saturation = 1f;
				contrast = 1.1f;
				red = 1f;
				green = 1f;
				blue = 1f;
			}

			public static Key Lerp(Key a, Key b, float time)
			{
				return new Key(Mathf.Lerp(a.shadowIntensity, b.shadowIntensity, time), Mathf.Lerp(a.hueShift, b.hueShift, time), Mathf.Lerp(a.saturation, b.saturation, time), Mathf.Lerp(a.contrast, b.contrast, time), Mathf.Lerp(a.red, b.red, time), Mathf.Lerp(a.green, b.green, time), Mathf.Lerp(a.blue, b.blue, time));
			}
		}

		public Key[] keys;

		public Key Evaluate(float height)
		{
			if (height >= keys[keys.Length - 1].height)
			{
				return keys[keys.Length - 1];
			}
			if (height <= keys[0].height)
			{
				return keys[0];
			}
			int i;
			for (i = 0; height > keys[i + 1].height; i++)
			{
			}
			return Key.Lerp(keys[i], keys[i + 1], Mathf.InverseLerp(keys[i].height, keys[i + 1].height, height));
		}
	}

	[Serializable]
	public class FrontLayer
	{
		public string frontLayerTexture;

		public float cutout;

		public int extraRadius;

		public int transitionStart;

		public int transitionDistance;
	}

	[Serializable]
	public class OrbitModule
	{
		public string parent;

		public double orbitHeight;

		public double multiplierSOI = 1.0;

		public int orbitLineResolution = 200;
	}

	[Space]
	public BasicModule BASE_DATA;

	[Title("", null, TitleAlignments.Left, true, true)]
	[Space]
	public bool hasAtmosphere;

	[ShowIf("hasAtmosphere", true)]
	public AtmosphereModule ATMOSPHERE_DATA;

	[Title("", null, TitleAlignments.Left, true, true)]
	[Space]
	public bool hasTerrain;

	[ShowIf("hasTerrain", true)]
	public TerrainModule TERRAIN_DATA;

	[Title("", null, TitleAlignments.Left, true, true)]
	[Space]
	public bool hasFrontLayer;

	[ShowIf("hasFrontLayer", true)]
	public FrontLayer FRONT_LAYER;

	[Title("", null, TitleAlignments.Left, true, true)]
	[Space]
	public bool hasPostProcessing;

	[ShowIf("hasPostProcessing", true)]
	public ColorGradingGradient POST_PROCESSING;

	[Title("", null, TitleAlignments.Left, true, true)]
	[Space]
	public bool hasOrbitData;

	[ShowIf("hasOrbitData", true)]
	public OrbitModule ORBIT_DATA;

	public static PlanetData FromJson(string jsonData)
	{
		List<string> list = new List<string>(jsonData.Split("●"[0]));
		PlanetData planetData = new PlanetData();
		planetData.BASE_DATA = JsonUtility.FromJson<BasicModule>(list[list.IndexOf("BASE_DATA") + 1]);
		if (list.Contains("ATMOSPHERE_DATA"))
		{
			planetData.ATMOSPHERE_DATA = JsonUtility.FromJson<AtmosphereModule>(list[list.IndexOf("ATMOSPHERE_DATA") + 1]);
		}
		planetData.hasAtmosphere = list.Contains("ATMOSPHERE_DATA");
		if (list.Contains("FRONT_LAYER"))
		{
			planetData.FRONT_LAYER = JsonUtility.FromJson<FrontLayer>(list[list.IndexOf("FRONT_LAYER") + 1]);
		}
		planetData.hasFrontLayer = list.Contains("FRONT_LAYER");
		if (list.Contains("POST_PROCESSING"))
		{
			planetData.POST_PROCESSING = JsonUtility.FromJson<ColorGradingGradient>(list[list.IndexOf("POST_PROCESSING") + 1]);
		}
		planetData.hasPostProcessing = list.Contains("POST_PROCESSING");
		if (list.Contains("TERRAIN_DATA"))
		{
			planetData.TERRAIN_DATA = JsonUtility.FromJson<TerrainModule>(list[list.IndexOf("TERRAIN_DATA") + 1]);
		}
		planetData.hasTerrain = list.Contains("TERRAIN_DATA");
		if (list.Contains("ORBIT_DATA"))
		{
			planetData.ORBIT_DATA = JsonUtility.FromJson<OrbitModule>(list[list.IndexOf("ORBIT_DATA") + 1]);
		}
		planetData.hasOrbitData = list.Contains("ORBIT_DATA");
		return planetData;
	}

	public string ToJson()
	{
		string empty = string.Empty;
		empty = empty + "●BASE_DATA●\n" + JsonUtility.ToJson(BASE_DATA, true) + "\n\n";
		if (hasAtmosphere)
		{
			empty = empty + "●ATMOSPHERE_DATA●\n" + JsonUtility.ToJson(ATMOSPHERE_DATA, true) + "\n\n";
		}
		if (hasFrontLayer)
		{
			empty = empty + "●FRONT_LAYER●\n" + JsonUtility.ToJson(FRONT_LAYER, true) + "\n\n";
		}
		if (hasTerrain)
		{
			empty = empty + "●TERRAIN_DATA●\n" + JsonUtility.ToJson(TERRAIN_DATA, true) + "\n\n";
		}
		if (hasOrbitData)
		{
			empty = empty + "●ORBIT_DATA●\n" + JsonUtility.ToJson(ORBIT_DATA, true) + "\n\n";
		}
		if (hasPostProcessing)
		{
			empty = empty + "●POST_PROCESSING●\n" + JsonUtility.ToJson(POST_PROCESSING, true) + "\n\n";
		}
		return empty;
	}
}
