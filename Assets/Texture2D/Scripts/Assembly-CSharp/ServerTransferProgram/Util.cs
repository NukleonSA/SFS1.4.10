using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;

namespace ServerTransferProgram
{
	public static class Util
	{
		public static T Value<T>(this T[] arr)
		{
			if (Application.isMobilePlatform && Ref.hasPartsExpansion)
			{
				return arr[1];
			}
			if (Application.isMobilePlatform && !Ref.hasPartsExpansion)
			{
				return arr[0];
			}
			if (Application.isConsolePlatform)
			{
				return arr[3];
			}
			return arr[2];
		}

		public static byte[] ToByteArray<T>(T obj)
		{
			if (obj == null)
			{
				return null;
			}
			BinaryFormatter binaryFormatter = new BinaryFormatter();
			using (MemoryStream memoryStream = new MemoryStream())
			{
				binaryFormatter.Serialize(memoryStream, obj);
				return memoryStream.ToArray();
			}
		}

		public static T FromByteArray<T>(byte[] data)
		{
			if (data == null)
			{
				return default(T);
			}
			BinaryFormatter binaryFormatter = new BinaryFormatter();
			using (MemoryStream serializationStream = new MemoryStream(data))
			{
				object obj = binaryFormatter.Deserialize(serializationStream);
				return (T)obj;
			}
		}

		public static string Base64Encode(string plainText)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(plainText);
			return Convert.ToBase64String(bytes);
		}

		public static string Base64Decode(string base64EncodedData)
		{
			byte[] bytes = Convert.FromBase64String(base64EncodedData);
			return Encoding.UTF8.GetString(bytes);
		}
	}
}
