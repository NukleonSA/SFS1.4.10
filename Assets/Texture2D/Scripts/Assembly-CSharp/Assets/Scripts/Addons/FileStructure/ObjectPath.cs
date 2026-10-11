using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Addons.FileStructure
{
	public class ObjectPath
	{
		public string path = string.Empty;

		public ObjectPath(bool interhitApplication = true, string extend = "")
		{
			if (interhitApplication)
			{
				path = BasePath();
			}
			if (extend != string.Empty && path != string.Empty)
			{
				if (!extend.StartsWith("\\") || !extend.StartsWith("/") || !extend.StartsWith(Path.DirectorySeparatorChar.ToString()))
				{
					extend = Path.DirectorySeparatorChar + extend;
				}
				path += extend;
			}
		}

		public ObjectPath(bool interhitApplication = true, params string[] extend)
		{
			if (interhitApplication)
			{
				path = BasePath();
			}
			foreach (string two in extend)
			{
				path = Combine(path, two);
			}
		}

		public static string BasePath()
		{
			return (!Application.isMobilePlatform) ? Application.dataPath : Application.persistentDataPath;
		}

		public void Extend(string extension)
		{
			path = Combine(path, extension);
		}

		public string[] IndexedPath()
		{
			return path.Replace("\\", Path.DirectorySeparatorChar.ToString()).Replace("/", Path.DirectorySeparatorChar.ToString()).Split(Path.DirectorySeparatorChar);
		}

		public bool IsFile()
		{
			return File.Exists(path);
		}

		public bool IsDir()
		{
			return Directory.Exists(path);
		}

		public bool IsTheoreticalFile()
		{
			string[] array = IndexedPath();
			string text = array[array.Count() - 1];
			return text.Contains(".");
		}

		public bool IsTheoreticalDir()
		{
			return !IsTheoreticalFile();
		}

		public ObjectFile[] FilesAtLocation()
		{
			string text = path;
			if (!IsFile() && !IsTheoreticalFile() && (IsDir() || IsTheoreticalDir()))
			{
				text = Path.GetDirectoryName(text);
			}
			List<ObjectFile> list = new List<ObjectFile>();
			string[] files = Directory.GetFiles(path);
			foreach (string text2 in files)
			{
				ObjectFile objectFile = new ObjectFile();
				objectFile.path = text2;
				list.Add(objectFile);
			}
			list = OrderByDate(list);
			return list.ToArray();
		}

		public static List<ObjectFile> OrderByDate(List<ObjectFile> list)
		{
			List<object[]> list2 = new List<object[]>();
			foreach (ObjectFile item in list)
			{
				long ticks = File.GetCreationTime(item.path).Ticks;
				list2.Add(new object[2] { ticks, item });
			}
			list2 = list2.OrderBy((object[] x) => (long)x[0]).ToList();
			List<ObjectFile> list3 = new List<ObjectFile>();
			for (int num = 0; num < list2.Count; num++)
			{
				list3.Add((ObjectFile)list2[num][1]);
			}
			return list3;
		}

		public ObjectPath[] DirsAtLocation()
		{
			string text = path;
			if (!IsFile() && !IsTheoreticalFile() && (IsDir() || IsTheoreticalDir()))
			{
				text = Path.GetDirectoryName(text);
			}
			List<ObjectPath> list = new List<ObjectPath>();
			string[] directories = Directory.GetDirectories(path);
			foreach (string extend in directories)
			{
				ObjectPath objectPath = new ObjectPath(false, extend);
				objectPath.path = extend;
				list.Add(objectPath);
			}
			return list.ToArray();
		}

		public string GetPathExtention()
		{
			return (Path.GetExtension(path) ?? ".NULL").Replace(".", string.Empty);
		}

		public bool PathExists()
		{
			return IsDir() || IsFile();
		}

		public void CreatePath()
		{
			Directory.CreateDirectory(path);
		}

		public ObjectFile ToFile(string fileName, bool dirCheck)
		{
			ObjectFile objectFile = ToFile(dirCheck);
			objectFile.Extend(fileName);
			return objectFile;
		}

		public ObjectFile ToFile(bool dirCheck = false)
		{
			if ((IsDir() || (IsTheoreticalDir() && !IsFile())) && dirCheck)
			{
				throw new Exception("Target is a directory");
			}
			ObjectFile objectFile = new ObjectFile();
			objectFile.path = path;
			objectFile.fileCheck = dirCheck;
			return objectFile;
		}

		public string DirectoryName()
		{
			if (IsFile() || (IsTheoreticalFile() && !IsDir()))
			{
				return Path.GetDirectoryName(path);
			}
			return IndexedPath()[IndexedPath().Count() - 1];
		}

		public void ToParentDirectory()
		{
			if (IsFile() || (IsTheoreticalFile() && !IsDir()))
			{
				List<string> list = IndexedPath().ToList();
				list.RemoveRange(list.Count - 3, 2);
				path = string.Join(Path.DirectorySeparatorChar.ToString(), list.ToArray());
			}
			else
			{
				List<string> list2 = IndexedPath().ToList();
				list2.RemoveAt(list2.Count - 1);
				path = string.Join(Path.DirectorySeparatorChar.ToString(), list2.ToArray());
			}
		}

		public ObjectPath CopyObject()
		{
			ObjectPath objectPath = new ObjectPath(false, path);
			objectPath.path = path;
			return objectPath;
		}

		public override string ToString()
		{
			return path;
		}

		public static string Combine(string one, string two)
		{
			return one + Path.DirectorySeparatorChar + two;
		}
	}
}
