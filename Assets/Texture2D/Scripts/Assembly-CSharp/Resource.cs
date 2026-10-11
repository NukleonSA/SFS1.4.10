public class Resource
{
	public enum Type
	{
		Fuel = 0,
		SolidFuel = 1,
		Power = 2
	}

	public static string GetResourceName(Type resourceType)
	{
		return (new string[3] { "燃料", "固体燃料", "电力" })[(int)resourceType];
	}

	public static string GetResourceUnit(Type resourceType)
	{
		return (new string[3]
		{
			"t",
			"t",
			string.Empty
		})[(int)resourceType];
	}

	public static bool GrupsGlobally(Type resourceType)
	{
		return (new bool[3] { false, false, true })[(int)resourceType];
	}
}
