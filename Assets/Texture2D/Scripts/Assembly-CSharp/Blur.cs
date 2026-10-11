using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;

public class Blur : MonoBehaviour
{
	public Texture2D shade;

	public Texture2D blur;

	public float blurStreght;

	public Texture2D flameIn;

	public Texture2D flameOut;

	[Button("Generate Shade Texture")]
	public void Generate()
	{
		Texture2D texture2D = new Texture2D(shade.width, shade.height, TextureFormat.Alpha8, false);
		texture2D.filterMode = FilterMode.Point;
		for (int i = 0; i < shade.width; i++)
		{
			for (int j = 0; j < shade.height; j++)
			{
				if (blur.GetPixel(i, j).a > 0f)
				{
					int num = (int)(blur.GetPixel(i, j).a * 12f);
					float num2 = 0f;
					for (int k = 0; k < num * 2 + 1; k++)
					{
						num2 += 1f - shade.GetPixel(Mathf.Clamp(i - num + k, 0, shade.width), j).r;
					}
					num2 = Mathf.Lerp(1f - shade.GetPixel(i, j).r, num2 / (float)(num * 2 + 1), blurStreght);
					texture2D.SetPixel(i, j, new Color(1f, 1f, 1f, num2));
				}
				else
				{
					texture2D.SetPixel(i, j, new Color(0f, 0f, 0f, 1f - shade.GetPixel(i, j).r));
				}
			}
		}
		texture2D.Apply();
		File.WriteAllBytes(Application.dataPath + "/Textures/Combined.png", texture2D.EncodeToPNG());
	}

	[Button("Generate Shade Texture")]
	public void GenerateFlame()
	{
		Texture2D texture2D = new Texture2D(flameIn.width, flameIn.height, TextureFormat.RGBA32, false);
		texture2D.filterMode = FilterMode.Point;
		for (int i = 0; i < texture2D.width; i++)
		{
			for (int j = 0; j < texture2D.height; j++)
			{
				if (flameIn.GetPixel(i, j).a == 0f)
				{
					texture2D.SetPixel(i, j, Color.clear);
					continue;
				}
				Color pixel = flameIn.GetPixel(i, j);
				float num = Mathf.Max(pixel.r, pixel.g, pixel.b);
				texture2D.SetPixel(i, j, new Color(pixel.r / num, pixel.g / num, pixel.b / num, num));
			}
		}
		texture2D.Apply();
		File.WriteAllBytes(Application.dataPath + "/Textures/" + flameOut.name + ".png", texture2D.EncodeToPNG());
	}
}
