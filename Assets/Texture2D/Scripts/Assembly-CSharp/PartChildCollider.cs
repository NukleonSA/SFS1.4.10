using UnityEngine;

public class PartChildCollider : MonoBehaviour
{
	public float breakVelocity = 5f;

	public Part part;

	private void OnValidate()
	{
		Transform parent = base.transform.parent;
		if (!(parent != null))
		{
			return;
		}
		while (parent.GetComponent<Part>() == null)
		{
			if (parent.parent != null)
			{
				parent = parent.parent;
				continue;
			}
			MonoBehaviour.print(parent.name);
			break;
		}
		Part component = parent.GetComponent<Part>();
		part = ((!(component != null)) ? part : component);
	}

	private void OnCollisionEnter2D(Collision2D collision)
	{
		if (!Ref.unbreakableParts && part.vessel.partsManager.partToPartDamage && !(collision.relativeVelocity.sqrMagnitude < breakVelocity * breakVelocity))
		{
			part.DestroyPart(true, false);
		}
	}
}
