using Godot;
using MageTechDemo.scripts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public partial class ProcGenTest : Node3D {
	private PackedScene pkdMarker = ResourceLoader.Load<PackedScene>("res://scenes/tests/control_point.tscn");
	private ShaderMaterial meshShaderMaterial = ResourceLoader.Load<ShaderMaterial>("res://assets/materials/mesh_shader_material.tres");
	private Material debugPointMaterial = ResourceLoader.Load<Material>("res://assets/materials/debug_point_material.tres");
	private Material debugLineMaterial = ResourceLoader.Load<Material>("res://assets/materials/debug_line_material.tres");
	private Material defaultMaterial = ResourceLoader.Load<Material>("res://assets/materials/default_material.tres");

	/// <summary>
	/// How far apart each mesh point is from each other in meters.
	/// </summary>
	[Export(PropertyHint.Range, "0.05,1,0.05")]
	float MeshPointSpacing {
		get => meshPointSpacing;
		set {
			meshPointSpacing = value;
		}
	}
	private float meshPointSpacing = 0.2f;

	/// <summary>
	/// The radius away from each control point that mesh points will be generated in meters.
	/// </summary>
	[Export(PropertyHint.Range, "0,2,0.05")]
	float MeshGenerationRadius {
		get => meshGenerationRadius;
		set {
			meshGenerationRadius = value;
		}
	}
	private float meshGenerationRadius = 0.5f;

	/// <summary>
	/// The radius away from each control point that mesh points will be generated in meters.
	/// </summary>
	[Export(PropertyHint.Range, "0,2,0.05")]
	float DrawPoint {
		get => drawPoint;
		set {
			drawPoint = value;
		}
	}
	private float drawPoint = 0.5f;

	private Node3D controlPoints;
	private Label fpsLabel;
	private CheckBox checkDrawMeshPoints;
	private CheckBox checkDrawMesh;
	private CheckBox checkDrawNormals;
	private MeshInstance3D meshInstance;
	private MeshInstance3D pointMeshInstance;
	private MeshInstance3D normalMeshInstance;

	double timeSinceLastUpdate = 0f;

	/// <summary>
	/// Control points, and their location hashcodes for quick lookup.
	/// </summary>
	Dictionary<int, MeshPoint> meshPoints = [];

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		controlPoints = GetNode<Node3D>("ControlPoints");
		fpsLabel = GetNode<Label>("Control/MarginContainer/PanelContainer/VBoxContainer/HBoxContainer/FpsValue");
		checkDrawMesh = GetNode<CheckBox>("Control/MarginContainer/PanelContainer/VBoxContainer/CheckDrawMesh");
		checkDrawMeshPoints = GetNode<CheckBox>("Control/MarginContainer/PanelContainer/VBoxContainer/CheckDrawMeshPoints");
		checkDrawNormals = GetNode<CheckBox>("Control/MarginContainer/PanelContainer/VBoxContainer/CheckDrawNormals");
		meshInstance = GetNode<MeshInstance3D>("MeshInstance");
		pointMeshInstance = GetNode<MeshInstance3D>("PointMeshInstance");
		normalMeshInstance = GetNode<MeshInstance3D>("NormalMeshInstance");

		BuildMeshPoints();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
		if (!Engine.IsEditorHint()) {
			fpsLabel.Text = Math.Floor(Engine.GetFramesPerSecond()).ToString();
		}

		if (timeSinceLastUpdate >= 0.1) {
			BuildMeshPoints();
			timeSinceLastUpdate = 0f;
		}

		timeSinceLastUpdate += delta;
	}

	public static float RoundToNearestGivenFloat(float value, float nearestFloat) {
		if (nearestFloat == 0) {
			// Handle division by zero or a desired behavior for rounding to zero
			return 0;
		}

		// Scale the value by dividing by the target float
		float scaledValue = value / nearestFloat;

		// Round the scaled value to the nearest whole number
		float roundedScaledValue = MathF.Round(scaledValue, MidpointRounding.AwayFromZero);

		// Scale the rounded value back by multiplying by the target float
		float result = roundedScaledValue * nearestFloat;

		return result;
	}

	// Rebuilds the marker sprites and provides weight values based on the frequency and amplitude
	private void BuildMeshPoints() {
		meshPoints.Clear();

		if (controlPoints == null) return;

		// Loop through each control point and create mesh points around it.
		foreach (ControlPoint control in controlPoints.GetChildren().Cast<ControlPoint>()) {
			Vector3 controlPos = control.Position;

			float offset = meshGenerationRadius + (meshPointSpacing * 2);

			// Iterate through a grid around each control point
			for (float x = controlPos.X - offset; x < controlPos.X + offset; x += meshPointSpacing) {
				for (float y = controlPos.Y - offset; y < controlPos.Y + offset; y += meshPointSpacing) {
					for (float z = controlPos.Z - offset; z < controlPos.Z + offset; z += meshPointSpacing) {
						Vector3 meshPointPos = new(
							RoundToNearestGivenFloat(x, meshPointSpacing),
							RoundToNearestGivenFloat(y, meshPointSpacing),
							RoundToNearestGivenFloat(z, meshPointSpacing)
						);
						int posHash = VectorUtils.GetVectorHash(meshPointPos);

						float distanceToControl = meshPointPos.DistanceTo(controlPos);

						// Check if the point is within the radius to ignore points outside the radius.
						if (distanceToControl < meshGenerationRadius + (meshPointSpacing * 2)) {
							float weight = 1f - (distanceToControl / meshGenerationRadius);
							MeshPoint point = new(meshPointPos, weight);

							// If already in the list but the saved weight is lower, update the weight
							if (meshPoints.TryGetValue(posHash, out MeshPoint value)) {
								if (value.Weight < weight) {
									meshPoints[posHash].Weight = weight;
								}
							} else {
								meshPoints.Add(posHash, point);
							}
						}
					}
				}
			}
		}

		DrawMeshPoints();
		DrawMesh();
	}

	private void DrawMeshPoints() {
		ImmediateMesh debugMesh = pointMeshInstance.Mesh as ImmediateMesh;
		debugMesh.ClearSurfaces();

		// If not checked, just clear the mesh
		if (!checkDrawMeshPoints.ButtonPressed) return;

		debugMesh.SurfaceBegin(Mesh.PrimitiveType.Points);

		foreach (MeshPoint point in meshPoints.Values) {
			debugMesh.SurfaceSetColor(point.GetColor(drawPoint));
			debugMesh.SurfaceAddVertex(point.Position);
		}

		debugMesh.SurfaceEnd();
	}

	private void DrawMesh() {
		ImmediateMesh mesh = meshInstance.Mesh as ImmediateMesh;
		mesh.ClearSurfaces();

		ImmediateMesh normalMesh = normalMeshInstance.Mesh as ImmediateMesh;
		normalMesh.ClearSurfaces();

		// If not checked, just clear the mesh. If there aren't any points nothing is going to get drawn
		if (!checkDrawMesh.ButtonPressed || meshPoints.Count == 0) return;

		if (checkDrawNormals.ButtonPressed) {
			normalMesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
		}

		mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);

		// Render mesh from points
		foreach (MeshPoint point in meshPoints.Values) {
			Vector3 cubeOrigin = point.Position;

			List<Vector3> cubeCorners = [];
			List<float> cubeWeights = [];

			for (int v = 0; v < 8; v++) {
				Vector3 offsetPos = cubeOrigin + (GenerationTable.originOffsets[v] * meshPointSpacing);

				cubeCorners.Add(offsetPos);

				if (meshPoints.TryGetValue(VectorUtils.GetVectorHash(offsetPos), out MeshPoint offsetMarker)) {
					cubeWeights.Add(offsetMarker.Weight);
				} else {
					cubeWeights.Add(-1f);
				}
			}

			Cube cube = new([.. cubeCorners], [.. cubeWeights], drawPoint);

			var triangles = cube.GetTriangles();

			if (triangles.Count == 0) {
				continue;
			}

			foreach (Triangle tri in triangles) {
				// Calculate the normal of the whole face, and apple to each vertex
				Vector3 vertA = tri.A;
				Vector3 vertB = tri.B;
				Vector3 vertC = tri.C;

				Vector3 normal = (vertC - vertA).Cross(vertB - vertA);
				normal = normal.Normalized();

				float avgX = (vertA.X + vertB.X + vertC.X) / 3;
				float avgY = (vertA.Y + vertB.Y + vertC.Y) / 3;
				float avgZ = (vertA.Z + vertB.Z + vertC.Z) / 3;

				Vector3 triAvg = new(avgX, avgY, avgZ);


				mesh.SurfaceSetNormal(normal);
				mesh.SurfaceAddVertex(tri.A);

				mesh.SurfaceSetNormal(normal);
				mesh.SurfaceAddVertex(tri.B);

				mesh.SurfaceSetNormal(normal);
				mesh.SurfaceAddVertex(tri.C);

				if (checkDrawNormals.ButtonPressed) {
					normalMesh.SurfaceSetColor(Colors.LimeGreen);
					normalMesh.SurfaceAddVertex(triAvg);

					normalMesh.SurfaceSetColor(Colors.LimeGreen);
					normalMesh.SurfaceAddVertex(triAvg + (normal * 0.2f));
				}
			}
		}

		mesh.SurfaceEnd();

		if (checkDrawNormals.ButtonPressed) {
			normalMesh.SurfaceEnd();
		}
	}

	private void OnSpawnPressed() {
		GD.Print("Pressed");
	}

	private void OnCheckDrawMeshToggled(bool active) {
		DrawMesh();
	}

	private void OnCheckDrawMeshPointsToggled(bool active) {
		DrawMeshPoints();
	}
}