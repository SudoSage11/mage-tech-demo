#nullable disable

using Godot;
using System;

public partial class MeshPoint(Vector3 position, float weight)
{
	public float Weight { get; set; } = weight;

	public Vector3 Position { get; set; } = position;

	public int PosHash { get => VectorUtils.GetVectorHash(Position); }

	public Color GetColor(float maxDistance)
	{
		if (Weight < maxDistance)
		{
			return new(1, 1, 1);
		}
		else
		{
			return new(1, 0, 0);
		}
	}
}
