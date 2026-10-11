using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
	public delegate void EmptyDelegate();

	public static MenuManager main;

	[FoldoutGroup("Show Msg", 0)]
	public Text msgText;

	private IEnumerator msgCoroutine;

	[FoldoutGroup("Warning", 0)]
	public Text warningText;

	[FoldoutGroup("Warning", 0)]
	public Text confirmText;

	[FoldoutGroup("Warning", 0)]
	public Text cancelText;

	[FoldoutGroup("Warning", 0)]
	[SerializeField]
	private EmptyDelegate onConfirm;

	[FoldoutGroup("Warning", 0)]
	[SerializeField]
	private EmptyDelegate onCancel;

	[FoldoutGroup("Warning", 0)]
	public RectTransform warningTextShade;

	[FoldoutGroup("Menu Back", 0)]
	public bool enabledMenuBack;

	[FoldoutGroup("Menu Back", 0)]
	public UnityEvent onOpenMenuBack;

	[FoldoutGroup("Menu Back", 0)]
	public UnityEvent onCloseMenuBack;

	public RectTransform topLeftHolder;

	public RectTransform topRightHolder;

	public RectTransform bannerBackground;

	public Canvas canvas;

	public static bool EnabledMenuBack
	{
		get
		{
			return main.enabledMenuBack;
		}
	}

	private void Start()
	{
		main = this;
		if (!AdManager.ShowAds)
		{
		}
	}

	public static void ShowMsg(string msgText)
	{
		ShowMsg(msgText, 3.5f, true);
	}

	public void ShowMessage(string msgText)
	{
		ShowMsg(msgText, 3.5f, true);
	}

	public static void ShowMsg(string msgText, bool showMsg = true)
	{
		ShowMsg(msgText, 3.5f, showMsg);
	}

	public static void ShowMsg(string msgText, float displayTime = 3.5f, bool showMsg = true)
	{
		if (showMsg && !(main == null) && main.gameObject.activeInHierarchy)
		{
			if (main.msgCoroutine != null)
			{
				main.StopCoroutine(main.msgCoroutine);
			}
			main.msgCoroutine = main.MsgCoroutine(msgText, displayTime);
			main.StartCoroutine(main.msgCoroutine);
		}
	}

	private IEnumerator MsgCoroutine(string msg, float displayTime)
	{
		msgText.text = msg;
		while (displayTime > 0f)
		{
			displayTime -= Time.deltaTime;
			Color newColor = new Color(1f, 1f, 1f, displayTime);
			if (newColor != msgText.color)
			{
				msgText.color = newColor;
			}
			yield return new WaitForEndOfFrame();
		}
	}

	public static void ShowWarning(string warning, Vector2 warningWindowSize, string confirm, EmptyDelegate onConfirm, string cancel, EmptyDelegate onCancel, int buttonSize)
	{
		main.warningText.transform.parent.gameObject.SetActive(true);
		OpenMenuBack(false);
		main.warningText.text = warning;
		main.warningTextShade.sizeDelta = warningWindowSize;
		main.confirmText.text = confirm;
		main.cancelText.text = cancel;
		main.onConfirm = onConfirm;
		main.onCancel = onCancel;
		RectTransform component = main.confirmText.transform.parent.GetComponent<RectTransform>();
		RectTransform component2 = main.cancelText.transform.parent.GetComponent<RectTransform>();
		Vector2 vector = new Vector2(buttonSize, component.rect.height);
		Vector2 sizeDelta = (component2.sizeDelta = vector);
		component.sizeDelta = sizeDelta;
		RectTransform component3 = component.GetChild(0).GetComponent<RectTransform>();
		sizeDelta = vector;
		component2.GetChild(0).GetComponent<RectTransform>().sizeDelta = sizeDelta;
		component3.sizeDelta = sizeDelta;
		BoxCollider2D component4 = component.GetComponent<BoxCollider2D>();
		sizeDelta = new Vector2(vector.x, 74f);
		component2.GetComponent<BoxCollider2D>().size = sizeDelta;
		component4.size = sizeDelta;
		component.GetComponent<BoxCollider2D>().offset = new Vector2(buttonSize / 2, component.GetComponent<BoxCollider2D>().offset.y);
		component2.GetComponent<BoxCollider2D>().offset = new Vector2(buttonSize / -2, component2.GetComponent<BoxCollider2D>().offset.y);
	}

	public void ConfirmWarning()
	{
		Invoke("ConfirmAction", 0.1f / Time.timeScale);
	}

	private void ConfirmAction()
	{
		onConfirm();
		CloseWarning();
	}

	public void CancelWarning()
	{
		Invoke("CancelAction", 0.1f / Time.timeScale);
	}

	private void CancelAction()
	{
		if (onCancel != null)
		{
			onCancel();
		}
		CloseWarning();
	}

	private void CloseWarning()
	{
		warningText.transform.parent.gameObject.SetActive(false);
		CloseMenuBack();
	}

	public static void OpenMenuBack(bool pause)
	{
		main.enabledMenuBack = true;
		main.onOpenMenuBack.Invoke();
		Time.timeScale = ((!pause) ? 1 : 0);
	}

	public static void CloseMenuBack()
	{
		main.enabledMenuBack = false;
		main.onCloseMenuBack.Invoke();
		Time.timeScale = 1f;
	}
}
