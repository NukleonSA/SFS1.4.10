using System;
using Sirenix.OdinInspector;
using UnityEngine;

public class Soundtrack : MonoBehaviour
{
	[Serializable]
	public class SoundtrackPiece
	{
		public AudioClip soundtracks;

		[Range(0f, 3f)]
		public float pitch = 1f;

		[Range(0f, 3f)]
		public float volume = 0.25f;

		public bool enabled = true;
	}

	[Space]
	[TableList]
	public SoundtrackPiece[] soundtracks;

	[Header(" Delay between soundtracks")]
	public float delay;

	[Space]
	public AudioSource audioSource;

	public bool playingSoundtrack;

	public int index;

	public void SelectRandomSoundtrack()
	{
		int num = index;
		while (num == index || !soundtracks[num].enabled)
		{
			num = UnityEngine.Random.Range(0, soundtracks.Length);
		}
		index = num;
		AudioClip audioClip = soundtracks[index].soundtracks;
		audioSource.pitch = soundtracks[index].pitch;
		audioSource.clip = audioClip;
		audioSource.Play();
		Invoke("SelectRandomSoundtrack", audioClip.length / audioSource.pitch + delay);
	}

	private void Update()
	{
		if (Ref.currentScene == Ref.SceneType.Game)
		{
			playingSoundtrack = Ref.mainVessel != null && (!Ref.controller.loadedPlanet.data.hasAtmosphere || Ref.mainVesselHeight > Ref.controller.loadedPlanet.data.ATMOSPHERE_DATA.PHYSICS.height || Ref.controller.loadedPlanet.data.BASE_DATA.name != Ref.controller.startAdress);
		}
		if (audioSource == null)
		{
			return;
		}
		if (playingSoundtrack)
		{
			if (!audioSource.isPlaying && !IsInvoking("SelectRandomSoundtrack"))
			{
				Invoke("SelectRandomSoundtrack", 1f / Time.timeScale);
			}
			if (index != -1 && audioSource.volume < soundtracks[index].volume)
			{
				audioSource.volume = Mathf.Min(audioSource.volume + Time.deltaTime * 0.1f * soundtracks[index].volume, soundtracks[index].volume);
			}
		}
		else if (audioSource.isPlaying)
		{
			if (audioSource.volume > 0f)
			{
				audioSource.volume -= Time.deltaTime * 0.1f * soundtracks[index].volume;
				return;
			}
			audioSource.Stop();
			CancelInvoke("SelectRandomSoundtrack");
		}
	}
}
