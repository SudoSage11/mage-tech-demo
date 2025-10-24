#nullable disable

using Godot;

[Tool]
public partial class FireControlPoint : ControlPoint
{
	public override void _Ready()
	{
		base._Ready();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
	}

	public override float CalculateMeshPointWeight(Vector3 meshPointPos)
	{
		Vector3 controlPos = Position;
		float distanceToControl = meshPointPos.DistanceTo(controlPos);

		// Check if the point is within the radius to ignore points outside the radius.
		float weight = 1f - (distanceToControl / 0.5f);
		return weight;
	}
}
