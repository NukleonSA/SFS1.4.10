using Sirenix.OdinInspector;
using UnityEngine;

public class stars : MonoBehaviour
{
	public int starCount;

	public AnimationCurve sizeCurve;

	public Transform starPrefab;

	public float area;

	public float perlinSize;

	public Gradient gradient;

	public float size;

	[Button]
	private void CreateStars()
	{
		while (base.transform.childCount > 0)
		{
			Object.DestroyImmediate(base.transform.GetChild(0).gameObject);
		}
		for (int i = 0; i < starCount; i++)
		{
			Transform transform = Object.Instantiate(starPrefab, base.transform);
			transform.GetComponent<SpriteRenderer>().color = gradient.Evaluate(Random.Range(0f, 1f));
			transform.localPosition = Random.insideUnitCircle * area;
			while (Mathf.PerlinNoise(transform.localPosition.x / perlinSize + 100f, transform.localPosition.y / perlinSize + 100f) > Random.Range(0.2f, 0.8f))
			{
				transform.localPosition = Random.insideUnitCircle * area;
			}
			transform.localScale = Vector3.one * sizeCurve.Evaluate(Random.Range(0f, 1f)) * size;
		}
	}
}
