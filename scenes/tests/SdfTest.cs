using Godot;
using System.Linq;

[Tool]
public partial class SdfTest : Node3D
{
	private Node3D? controlPoints;
	private MeshInstance3D? meshInstance;
	private ShaderMaterial? shader;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		controlPoints = GetNode<Node3D>("ControlPoints");
		meshInstance = GetNode<MeshInstance3D>("MeshInstance3D");

		shader = meshInstance.MaterialOverride as ShaderMaterial;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Godot.Collections.Array positions = [];

		// Add all positions for the control points to the shader
		foreach (ControlPoint point in controlPoints?.GetChildren().Cast<ControlPoint>() ?? [])
		{
			positions.Add(point.GlobalPosition);
		}

		shader?.SetShaderParameter("node_positions", positions);
	}
}
