using NewBuildSystem;
using UnityEngine;
using UnityEngine.UI;

public class RocketPost : MonoBehaviour
{
	public string ID = string.Empty;

	public string rocketJSON;

	public Text titleText;

	public Text idText;

	public Text scoreText;

	public GameObject upVoteHighlight;

	public GameObject downVoteHightlight;

	public GameObject deleteButton;

	public Image previewImage;

	public MoveModule anim;

	public void HidePage()
	{
		base.gameObject.SetActive(false);
	}

	public void PlayAnimation()
	{
		anim.SetTargetTime(1f);
		anim.SetTime(0f);
	}

	public void LoadPage(string title, string rocketId, int score, bool hasUpVoted, bool hasDownVoted, bool canDelete)
	{
		if (!base.gameObject.activeSelf)
		{
			base.gameObject.SetActive(true);
		}
		titleText.text = title;
		idText.text = rocketId;
		scoreText.text = GetScoreString(score);
		if (upVoteHighlight.gameObject.activeSelf != hasUpVoted)
		{
			upVoteHighlight.gameObject.SetActive(hasUpVoted);
		}
		if (downVoteHightlight.gameObject.activeSelf != hasDownVoted)
		{
			downVoteHightlight.gameObject.SetActive(hasDownVoted);
		}
		if (deleteButton.gameObject.activeSelf != canDelete)
		{
			deleteButton.gameObject.SetActive(canDelete);
		}
	}

	public void LoadImage(string json)
	{
		Texture2D texture2D = Build.CreatePreviewIcon(json);
		previewImage.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f));
	}

	public void Show()
	{
		base.gameObject.SetActive(true);
	}

	public void SetID(string id)
	{
		idText.text = id;
	}

	public void SetTitle(string title)
	{
		titleText.text = title;
	}

	public void SetScore(int score)
	{
		scoreText.text = GetScoreString(score);
	}

	public void SetUpVoted(bool has)
	{
		upVoteHighlight.gameObject.SetActive(has);
	}

	public void SetDownVoted(bool has)
	{
		downVoteHightlight.gameObject.SetActive(has);
	}

	public void SetDeletable(bool able)
	{
		deleteButton.gameObject.SetActive(able);
	}

	private string GetScoreString(int score)
	{
		if (score < 1000)
		{
			return score.ToString();
		}
		if (score < 10000)
		{
			return (score / 1000).ToString() + "." + score / 100 % 10 + "k";
		}
		return score / 1000 + "k";
	}
}
