using NewBuildSystem;

public static class SetTypeUtility
{
	public static void Exec(this PartData.Switch.Output.MeshSetType input, SpriteMesh mesh, SpriteMesh original, int point, float floatValue)
	{
		switch (input)
		{
		case PartData.Switch.Output.MeshSetType.Width:
			if (mesh.points[point].width != original.points[point].width + floatValue)
			{
				mesh.points[point].width = original.points[point].width + floatValue;
				mesh.Generate();
			}
			break;
		case PartData.Switch.Output.MeshSetType.PosX:
			if (mesh.points[point].position.x != original.points[point].position.x + floatValue)
			{
				mesh.points[point].position.x = original.points[point].position.x + floatValue;
				mesh.Generate();
			}
			break;
		case PartData.Switch.Output.MeshSetType.PosY:
			if (mesh.points[point].position.y != original.points[point].position.y + floatValue)
			{
				mesh.points[point].position.y = original.points[point].position.y + floatValue;
				mesh.Generate();
			}
			break;
		case PartData.Switch.Output.MeshSetType.UVy:
			if (mesh.points[point].uv != original.points[point].uv + floatValue)
			{
				mesh.points[point].uv = original.points[point].uv + floatValue;
				mesh.Generate();
			}
			break;
		}
	}
}
