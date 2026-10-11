using System;
using System.Collections.Generic;
using Assets.Scripts.Addons.FileStructure;

namespace Assets.Scripts.Save
{
	[Serializable]
	public class World
	{
		private const string persistantName = "Persistant.qks";

		public ObjectFile persistantPath;

		public ObjectPath quicksavesPath;

		public World(string name)
		{
			ObjectPath objectPath = Saving.WorldSavingLocation();
			objectPath.Extend(name);
			objectPath.CreatePath();
			persistantPath = objectPath.CopyObject().ToFile("Persistant.qks", false);
			quicksavesPath = objectPath.CopyObject();
			quicksavesPath.Extend("Quicksaves");
			quicksavesPath.CreatePath();
		}

		public static World GetWorldByName()
		{
			return new World("main");
		}

		public void UpdatePersistant()
		{
			persistantPath.FlushJUTILObject(GameSaver.Quicksave.CreateQuicksave("Persistant"));
		}

		public GameSaver.Quicksave LoadPersistant()
		{
			if (!persistantPath.IsFile())
			{
				return null;
			}
			GameSaver.Quicksave obj;
			persistantPath.CastFromJUTIL<GameSaver.Quicksave>(out obj);
			return obj;
		}

		public List<string> GetQuicksavesList()
		{
			List<string> list = new List<string>();
			ObjectFile[] array = quicksavesPath.FilesAtLocation();
			foreach (ObjectPath objectPath in array)
			{
				ObjectFile objectFile = objectPath.ToFile();
				if (objectPath.IsFile() && objectPath.GetPathExtention() == "qks")
				{
					list.Add(objectFile.FileName());
				}
			}
			return list;
		}

		public void CreateQuicksave(string quicksaveName)
		{
			CreateQuicksave(quicksaveName, GameSaver.Quicksave.CreateQuicksave(string.Empty));
		}

		public void CreateQuicksave(string quicksaveName, GameSaver.Quicksave quicksave)
		{
			quicksaveName = quicksaveName.Escape();
			if (quicksaveName == string.Empty)
			{
				quicksaveName = "Unnamed Quicksave";
			}
			quicksavesPath.CreatePath();
			ObjectFile objectFile = quicksavesPath.ToFile(quicksaveName + ".qks", false);
			objectFile.PreventOverwrite();
			objectFile.FlushJUTILObject(quicksave);
		}

		public GameSaver.Quicksave LoadQuickSave(string name)
		{
			ObjectFile quicksavePath = GetQuicksavePath(name);
			if (!quicksavePath.IsFile())
			{
				return null;
			}
			GameSaver.Quicksave obj;
			quicksavePath.CastFromJUTIL<GameSaver.Quicksave>(out obj);
			return obj;
		}

		public void DeleteQuickSave(string name)
		{
			ObjectFile quicksavePath = GetQuicksavePath(name);
			quicksavePath.Delete();
		}

		private ObjectFile GetQuicksavePath(string name)
		{
			ObjectFile objectFile = quicksavesPath.ToFile();
			objectFile.Extend(name + ".qks");
			return objectFile;
		}
	}
}
