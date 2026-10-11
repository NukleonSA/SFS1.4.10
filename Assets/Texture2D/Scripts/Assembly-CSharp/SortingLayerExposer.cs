using UnityEngine;

public class SortingLayerExposer : MonoBehaviour
{
	public string SortingLayerName = "Default";

	public int SortingOrder;

	private void Awake()
	{
		base.gameObject.GetComponent<MeshRenderer>().sortingLayerName = SortingLayerName;
		base.gameObject.GetComponent<MeshRenderer>().sortingOrder = SortingOrder;
	}
}
