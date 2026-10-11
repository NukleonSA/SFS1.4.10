using UnityEngine;

public class FlameRandomizer : MonoBehaviour
{
	public Vector2 min;

	public Vector2 max;

	private void Update()
	{
		if (!Ref.mapView)
		{
			base.transform.localScale = new Vector3(Random.Range(min.x, max.x), Random.Range(min.y, max.y));
		}
	}
}
