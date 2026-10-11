using System;
using Newtonsoft.Json;

namespace ServerTransferProgram.ServerControlLiberary.DataControllers.PacketSystem
{
	public class DefaultPacket
	{
		[NonSerialized]
		public static Random packetRandomizer = new Random();

		[NonSerialized]
		public Action onSended;

		public string ContainedType;

		public int PacketID = packetRandomizer.Next(0, 9999999);

		public int ResponseTo = -1;

		public bool Success = true;

		public string Error = string.Empty;

		public void SetError(string msg)
		{
			Success = false;
			Error = msg;
		}

		public string ToJSON()
		{
			return JsonConvert.SerializeObject(this);
		}

		public string ToSendable()
		{
			ContainedType = GetType().FullName;
			return ToJSON();
		}

		public static T FromSendable<T>(string json)
		{
			return JsonConvert.DeserializeObject<T>(json);
		}

		public static object FromSendable(string json)
		{
			DefaultPacket defaultPacket = JsonConvert.DeserializeObject<DefaultPacket>(json);
			Type type = Type.GetType(defaultPacket.ContainedType);
			if (type == null)
			{
				type = typeof(DefaultPacket);
			}
			return JsonConvert.DeserializeObject(json, type);
		}

		public Type GetContainedType()
		{
			return Type.GetType(ContainedType);
		}

		public object ToTargetPacket()
		{
			return Convert.ChangeType(this, GetContainedType());
		}

		public T ToTargetPacket<T>()
		{
			return (T)ToTargetPacket();
		}

		public T GetEncapsulatedData<T>(byte[] data)
		{
			return Util.FromByteArray<T>(data);
		}

		public byte[] EncapsulateData(object data)
		{
			return Util.ToByteArray(data);
		}

		public bool ContainsType<T>()
		{
			if (GetContainedType() == null)
			{
				return false;
			}
			return GetContainedType() == typeof(T);
		}

		public bool ContainsType(Type t)
		{
			if (GetContainedType() == null)
			{
				return false;
			}
			return GetContainedType() == t;
		}

		public bool Is<T>()
		{
			return Is(typeof(T));
		}

		public bool Is(Type t)
		{
			return IsSameOrSubclass(t, GetContainedType());
		}

		public bool IsSameOrSubclass(Type potentialBase, Type potentialDescendant)
		{
			return potentialDescendant.IsSubclassOf(potentialBase) || potentialDescendant == potentialBase;
		}
	}
}
