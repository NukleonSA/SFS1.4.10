using UnityEngine;

[CreateAssetMenu]
public class Skin : ScriptableObject
{
	public Sprite texture;

	public Sprite shading;

	public Sprite icon;

	public Color color = Color.white;

	public float scale = 1f;

	public float tileScale = 1f;

	public SpriteMesh.DrawMode drawMode;
}
