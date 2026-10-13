using System.IO;

namespace Assets.Scripts.Addons.FileStructure
{
	public static class ObjectFileUtility
	{
		public static string Escape(this string input)
		{
			char[] invalidPathChars = Path.GetInvalidPathChars();
			for (int i = 0; i < invalidPathChars.Length; i++)
			{
				char c = invalidPathChars[i];
				input = input.Replace(c.ToString(), string.Empty);
			}
			return input;
		}
	}
}
