using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace NewBuildSystem
{
	public class PickPartGrid : MonoBehaviour
	{
		public Utility.Vector2Specifier gridSize;

		[BoxGroup]
		public SpriteRenderer background;

		[BoxGroup]
		public Vector2 borderSize;

		public Transform categoryPrefab;

		public Transform categoryHolder;

		[BoxGroup("2", false, false, 0)]
		public int selectedListId;

		[BoxGroup("2", false, false, 0)]
		public Utility.PickListSpecifier pickList;

		public TextAsset pickListAsset;

		public Orientation orientation;

		public List<GameObject> icons;

		public List<Transform> buttons;

		public Transform PointCastButtons(Vector2 mousePos)
		{
			for (int i = 0; i < buttons.Count; i++)
			{
				if (buttons[i].gameObject.activeInHierarchy && Utility.IsInsideRange(mousePos.x, buttons[i].position.x, buttons[i].GetChild(0).position.x, true) && Utility.IsInsideRange(mousePos.y, buttons[i].position.y, buttons[i].GetChild(0).position.y, true))
				{
					return buttons[i];
				}
			}
			return null;
		}

		public HoldingPart TryTakePart(Vector2 mousePos, Vector2 mousePos0z)
		{
			if (!IsInsidePickArea(mousePos))
			{
				return null;
			}
			Vector2 vector = mousePos - (Vector2)base.transform.position;
			int num = (int)vector.x;
			int num2 = (int)(0f - vector.y);
			int num3 = num * (int)gridSize.Value.y + num2;
			if (num3 > pickList.Value[selectedListId].parts.Length - 1)
			{
				return null;
			}
			Vector2 vector2 = new Vector2(0f, 2f);
			Ref.inputController.PlayClickSound(0.2f);
			return new HoldingPart(vector2 - pickList.Value[selectedListId].parts[num3].centerOfRotation * orientation, new PlacedPart(null, Vector2.zero, orientation.DeepCopy(), false, 0, pickList.Value[selectedListId].parts[num3], new List<string>()));
		}

		public bool IsInsidePickArea(Vector2 mousePos)
		{
			Vector2 vector = mousePos - (Vector2)base.transform.position;
			return Utility.IsInsideRange(vector.x, 0f, gridSize.Value.x, true) && Utility.IsInsideRange(vector.y, 0f, 0f - gridSize.Value.y, true);
		}

		public bool IsInsideDropArea(Vector2 mousePos)
		{
			Vector2 vector = mousePos - (Vector2)base.transform.position;
			return Utility.IsInsideRange(vector.x, -2f, gridSize.Value.x, true) && Utility.IsInsideRange(vector.y, 2f, 0f - gridSize.Value.y - 1f, true);
		}

		public void Initialize()
		{
			SetBackground();
			CreateCategorys();
			LoadIcons();
		}

		[Button]
		private void SetBackground()
		{
			background.size = new Vector2(gridSize.Value.x, gridSize.Value.y) + borderSize;
			background.transform.localPosition = new Vector2(gridSize.Value.x, 0f - gridSize.Value.y) / 2f;
		}

		[Button("Export Part List")]
		private void Store()
		{
		}

		private void CreateCategorys()
		{
			for (int i = 0; i < pickList.Value.Length; i++)
			{
				Transform transform = Object.Instantiate(categoryPrefab, categoryHolder);
				transform.localPosition = new Vector3(0f, (float)i * -0.68f);
				TextMesh obj = transform.GetComponentsInChildren<TextMesh>()[0];
				string categoryName = pickList.Value[i].categoryName;
				transform.GetComponentsInChildren<TextMesh>()[1].text = categoryName;
				obj.text = categoryName;
				int a = i;
				transform.GetComponent<CustomEvent>().customEvent.AddListener(() =>
				{
					selectedListId = a;
					LoadIcons();
				});
				buttons.Add(transform);
			}
		}

		public void LoadIcons()
		{
			DeleteIcons();
			for (int i = 0; (float)i < gridSize.Value.x; i++)
			{
				for (int j = 0; (float)j < gridSize.Value.y; j++)
				{
					int num = i * (int)gridSize.Value.y + j;
					if (num <= pickList.Value[selectedListId].parts.Length - 1)
					{
						PartData partData = pickList.Value[selectedListId].parts[num];
						Vector2 vector = new Vector2((float)i + 0.5f, (float)(-j) - 0.5f) - partData.centerOfRotation * orientation * partData.pickGridScale;
						Vector2 vector2 = Vector2.one * partData.pickGridScale;
						Transform transform = PartGrid.LoadIcon(partData, orientation, 0, vector, vector2, base.transform, 50, Color.white, false);
						Utility.ApplySwitchesToIcon(transform, partData, Utility.GetKeys(partData, Utility.Location.pick, orientation, new bool[0], new bool[0]));
						icons.Add(transform.gameObject);
						transform.localScale *= partData.pickGridScale;
					}
				}
			}
		}

		private void DeleteIcons()
		{
			while (icons.Count > 0)
			{
				Object.Destroy(icons[0]);
				icons.RemoveAt(0);
			}
		}
	}
}
