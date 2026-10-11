using Assets.Scripts.Save;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
	public Text resumeButtonText;

	public Text disableMusicText;

	public Text disableSoundText;

	public Text disableScreenRotationText;

	public Text disable60fpsText;

	public Text infiniteFuelText;

	public Text noDragText;

	public Text unbreakablePartsText;

	public Text noGravityText;

	public CustomEvent openTutorial;

	public CustomEvent openSalePage;

	public GameObject buyPartsExpansionButton;

	private void Start()
	{
		if (!CanResumeGame())
		{
			resumeButtonText.color = new Color(1f, 1f, 1f, 0.5f);
		}
		UpdateSettingText(false);
		if (Ref.openTutorial)
		{
			openTutorial.InvokeEvenets();
			Ref.openTutorial = false;
		}
		buyPartsExpansionButton.SetActive(false);
		SetCheatSettings();
		ExportResoures();
	}

	private void LateUpdate()
	{
		if (Ref.openSalePage)
		{
			openSalePage.InvokeEvenets();
			Ref.openSalePage = false;
		}
	}

	public void OpenResourceFolder()
	{
	}

	public void ExportResoures()
	{
		AssetLoader.ExportPlanets();
	}

	public void SetCheatSettings()
	{
		infiniteFuelText.color = new Color(1f, 1f, 1f, (!Ref.hasPartsExpansion) ? 0.3f : 1f);
		noDragText.color = new Color(1f, 1f, 1f, (!Ref.hasPartsExpansion) ? 0.3f : 1f);
		unbreakablePartsText.color = new Color(1f, 1f, 1f, (!Ref.hasPartsExpansion) ? 0.3f : 1f);
		noGravityText.color = new Color(1f, 1f, 1f, (!Ref.hasPartsExpansion) ? 0.3f : 1f);
	}

	public void BuildNewRocket()
	{
		Ref.LoadScene(Ref.SceneType.Build);
	}

	public void ResumeGame()
	{
		if (resumeButtonText.color.a != 0.5f)
		{
			Ref.LoadScene(Ref.SceneType.Game);
		}
	}

	private static bool CanResumeGame()
	{
		GameSaver.Quicksave quicksave = World.GetWorldByName().LoadPersistant();
		return quicksave != null && quicksave.mainVesselId != -1;
	}

	public void OpenLinkLiftoff()
	{
		Application.OpenURL("https://www.youtube.com/watch?v=5uorANMdB60");
	}

	public void OpenLinkMoonLanding()
	{
		Application.OpenURL("https://www.youtube.com/watch?v=5RgPDlBNaN8&feature=youtu.be");
	}

	public void OpenLink(string link)
	{
		Application.OpenURL(link);
	}

	public void Toggle60fps()
	{
		// 在 30 -> 60 -> 90 -> 120 之间循环
		Saving.SaveFps(Saving.NextFps(Saving.LoadFps()));
		UpdateSettingText(true);
	}

	public void ToggleScreenRotation()
	{
		Saving.ToggleSetting(Saving.SettingKey.disableAutoRotate);
		UpdateSettingText(true);
	}

	public void ToggleMusic()
	{
		Saving.ToggleSetting(Saving.SettingKey.disableMusic);
		UpdateSettingText(true);
	}

	public void ToggleSound()
	{
		Saving.ToggleSetting(Saving.SettingKey.disableSound);
		UpdateSettingText(true);
	}

	public void ToggleInfiniteFuel()
	{
		if (Ref.hasPartsExpansion)
		{
			Saving.ToggleSetting(Saving.SettingKey.infiniteFuel);
			UpdateSettingText(true);
		}
	}

	public void ToggleNoDrag()
	{
		if (Ref.hasPartsExpansion)
		{
			Saving.ToggleSetting(Saving.SettingKey.noDrag);
			UpdateSettingText(true);
		}
	}

	public void ToggleUnbreakableParts()
	{
		if (Ref.hasPartsExpansion)
		{
			Saving.ToggleSetting(Saving.SettingKey.unbreakableParts);
			UpdateSettingText(true);
		}
	}

	public void ToggleNoGravity()
	{
		if (Ref.hasPartsExpansion)
		{
			Saving.ToggleSetting(Saving.SettingKey.noGravity);
			UpdateSettingText(true);
		}
	}

	private void UpdateSettingText(bool playSound)
	{
		if (playSound)
		{
			Ref.inputController.PlayClickSound(0.6f);
		}
		Ref.inputController.SetEnableSound();
		Ref.inputController.SetEnableMusic();
		disableMusicText.text = "音乐:" + ((!Saving.LoadSetting(Saving.SettingKey.disableMusic)) ? "开" : "关");
		disableSoundText.text = "声音:" + ((!Saving.LoadSetting(Saving.SettingKey.disableSound)) ? "开" : "关");
		Ref.inputController.SetFps();
		disable60fpsText.text = "帧率:" + Saving.LoadFps();
		disableScreenRotationText.text = "屏幕旋转:" + ((!Saving.LoadSetting(Saving.SettingKey.disableAutoRotate)) ? "开" : "关");
		infiniteFuelText.text = "无限燃料:" + ((!Saving.LoadSetting(Saving.SettingKey.infiniteFuel)) ? "关" : "开");
		noDragText.text = "无大气阻力:" + ((!Saving.LoadSetting(Saving.SettingKey.noDrag)) ? "关" : "开");
		unbreakablePartsText.text = "坚不可摧:" + ((!Saving.LoadSetting(Saving.SettingKey.unbreakableParts)) ? "关" : "开");
		noGravityText.text = "没有重力:" + ((!Saving.LoadSetting(Saving.SettingKey.noGravity)) ? "关" : "开");
	}

	public void QuitApplication()
	{
		Application.Quit();
	}
}
