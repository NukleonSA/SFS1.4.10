using System;

namespace Exposure.EAPI
{
	public class ExposureModule : Attribute
	{
		[Serializable]
		public class SerializableElement
		{
			public string key;

			public object value;
		}
	}
}
