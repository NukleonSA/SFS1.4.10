using UnityEngine;

[ExecuteInEditMode]
public class PostProcessingManager : MonoBehaviour
{
	public static PostProcessingManager main;

	[Space]
	public Material partMaterial;

	public Material posProcessingMaterial;

	[Space]
	public PlanetData.ColorGradingGradient.Key defaultAmbient;

	private void Start()
	{
		main = this;
		partMaterial.SetFloat("_Intensity", 1.45f);
	}

	private void LateUpdate()
	{
		if (Ref.currentScene == Ref.SceneType.Game)
		{
			if (Ref.mapView)
			{
				SetMap();
			}
			else
			{
				SetGame();
			}
		}
	}

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if ((Ref.mapView && Ref.currentScene == Ref.SceneType.Game) || posProcessingMaterial == null)
		{
			Graphics.Blit(source, destination);
		}
		else
		{
			Graphics.Blit(source, destination, posProcessingMaterial);
		}
	}

	private void SetGame()
	{
		PlanetData.ColorGradingGradient.Key key = ((!Ref.controller.loadedPlanet.data.hasPostProcessing) ? defaultAmbient : Ref.controller.loadedPlanet.data.POST_PROCESSING.Evaluate((float)Ref.mainVesselHeight + Ref.controller.cameraDistanceGame));
		SetColorGrading(true, key);
		partMaterial.SetFloat("_Intensity", key.shadowIntensity);
	}

	private void SetMap()
	{
		SetColorGrading(false, new PlanetData.ColorGradingGradient.Key(0f, 0f, 1f, 1f, 1f, 1f, 1f));
	}

	private void SetColorGrading(bool enabled, PlanetData.ColorGradingGradient.Key colorGrading)
	{
		posProcessingMaterial.SetFloat("_Hue", colorGrading.hueShift);
		posProcessingMaterial.SetFloat("_Saturation", colorGrading.saturation);
		posProcessingMaterial.SetFloat("_Contrast", colorGrading.contrast);
		float num = 0.3f * colorGrading.red + 0.59f * colorGrading.green + 0.11f * colorGrading.blue;
		Vector4 value = new Vector4(colorGrading.red / num, colorGrading.green / num, colorGrading.blue / num, 1f);
		posProcessingMaterial.SetVector("_Multiplier", value);
	}
}
