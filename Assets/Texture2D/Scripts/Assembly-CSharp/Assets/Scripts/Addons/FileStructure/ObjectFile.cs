using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Addons.FileStructure
{
	public class ObjectFile : ObjectPath
	{
		public bool fileCheck = true;

		public ObjectFile()
			: base(true, string.Empty)
		{
		}

		public void Delete()
		{
			CheckIfFile();
			File.Delete(path);
		}

		public bool Exists()
		{
			CheckIfFile();
			return IsFile();
		}

		public void FlushJson(string content)
		{
			File.WriteAllText(path, content);
		}

		public void PreventOverwrite()
		{
			if (!Exists())
			{
				return;
			}
			for (int i = 1; i < 1000; i++)
			{
				ObjectFile objectFile = ToFile();
				objectFile.SetFileName(FileName() + " " + i);
				if (!objectFile.Exists())
				{
					path = objectFile.path;
					break;
				}
			}
		}

		public DateTime DateCreated()
		{
			return File.GetCreationTime(path);
		}

		public string ReadContent()
		{
			CheckIfFile();
			return File.ReadAllText(path);
		}

		public T CastFromJNET<T>()
		{
			return JsonUtility.FromJson<T>(ReadContent());
		}

		public T CastFromJUTIL<T>()
		{
			return JsonUtility.FromJson<T>(ReadContent());
		}

		public void CastFromJNET<T>(out T obj)
		{
			obj = JsonUtility.FromJson<T>(ReadContent());
		}

		public void CastFromJUTIL<T>(out T obj)
		{
			obj = JsonUtility.FromJson<T>(ReadContent());
		}

		public void FlushJNETObject(object o)
		{
			FlushJson(JsonUtility.ToJson(o, true));
		}

		public void FlushJUTILObject(object o)
		{
			FlushJson(JsonUtility.ToJson(o, true));
		}

		private void CheckIfFile()
		{
			if (fileCheck && !IsFile() && (!IsTheoreticalFile() || IsDir()))
			{
				throw new Exception("Target has to be a file.");
			}
		}

		public string FileName(bool extention = false)
		{
			if (!extention)
			{
				return Path.GetFileNameWithoutExtension(path);
			}
			return Path.GetFileName(path);
		}

		public ObjectFile CopyFObject()
		{
			return ToFile();
		}

		public void SetFileName(string newName)
		{
			List<string> list = IndexedPath().ToList();
			list.RemoveAt(list.Count - 1);
			list.Add(newName + "." + GetPathExtention());
			path = string.Join(Path.DirectorySeparatorChar.ToString(), list.ToArray());
		}
	}
}
