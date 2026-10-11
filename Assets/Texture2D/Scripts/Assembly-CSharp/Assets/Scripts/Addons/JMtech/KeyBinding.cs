using UnityEngine;

namespace Assets.Scripts.Addons.JMtech
{
	public class KeyBinding
	{
		public KeyCode TriggerKey;

		public KeyBinding(KeyCode defaultKey)
		{
			TriggerKey = defaultKey;
		}

		public bool GetKey()
		{
			return Input.GetKey(TriggerKey);
		}

		public bool GetKeyUp()
		{
			return Input.GetKeyUp(TriggerKey);
		}

		public bool GetKeyDown()
		{
			return Input.GetKeyDown(TriggerKey);
		}
	}
}
