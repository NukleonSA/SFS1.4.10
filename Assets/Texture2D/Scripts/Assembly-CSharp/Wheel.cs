using System;
using Sirenix.OdinInspector;
using UnityEngine;

public class Wheel : MonoBehaviour
{
	public float power;

	public float traction;

	public float maxAngularVelocity;

	public float wheelSize;

	[BoxGroup]
	public float angularVelocity;

	[BoxGroup]
	public WheelModule wheelModule;

	private void OnCollisionStay2D(Collision2D collision)
	{
		float num = angularVelocity * ((float)Math.PI / 180f) * wheelSize;
		Vector2 vector = Quaternion.Euler(0f, 0f, 270f) * collision.contacts[0].normal;
		Vector2 vector2 = collision.contacts[0].relativeVelocity - vector * num;
		float magnitude = vector2.magnitude;
		float num2 = 1f;
		num2 = num2 * 0.1f * (float)Ref.controller.loadedPlanet.data.BASE_DATA.gravity;
		float num3 = traction / wheelModule.part.vessel.partsManager.rb2d.mass * Time.fixedDeltaTime * 10f;
		if (num3 > 1f)
		{
			num2 /= num3;
		}
		wheelModule.part.vessel.partsManager.rb2d.AddForceAtPosition(vector2 * traction * num2, base.transform.position);
		if (collision.rigidbody != null)
		{
			collision.rigidbody.AddForceAtPosition(-vector2 * traction * num2, collision.contacts[0].point);
		}
		float num4 = magnitude * traction * num2;
		Vector2 vector3 = collision.contacts[0].relativeVelocity - vector * ((angularVelocity + num4) * ((float)Math.PI / 180f) * wheelSize);
		Vector2 vector4 = collision.contacts[0].relativeVelocity - vector * ((angularVelocity - num4) * ((float)Math.PI / 180f) * wheelSize);
		float sqrMagnitude = vector3.sqrMagnitude;
		float sqrMagnitude2 = vector4.sqrMagnitude;
		if (sqrMagnitude > sqrMagnitude2)
		{
			angularVelocity -= magnitude * traction * num2;
		}
		else
		{
			angularVelocity += magnitude * traction * num2;
		}
	}

	private void Update()
	{
		float num = ((!(wheelModule.part.vessel == Ref.mainVessel) || !Ref.mainVessel.controlAuthority || !wheelModule.on.boolValue) ? 0f : (0f - Ref.inputController.horizontalAxis));
		wheelModule.throttle.floatValue = num * num;
		if (num == 0f)
		{
			num = Mathf.Clamp((0f - angularVelocity) / Time.deltaTime / power, (!wheelModule.on.boolValue) ? (-0.05f) : (-0.25f), (!wheelModule.on.boolValue) ? 0.05f : 0.25f);
		}
		if (num != 0f)
		{
			angularVelocity = Mathf.Clamp(angularVelocity + num * power * Time.deltaTime, 0f - maxAngularVelocity, maxAngularVelocity);
		}
		if (float.IsNaN(angularVelocity))
		{
			angularVelocity = 0f;
		}
		if (angularVelocity != 0f)
		{
			base.transform.eulerAngles = new Vector3(0f, 0f, base.transform.eulerAngles.z + angularVelocity * Time.deltaTime);
		}
	}
}
