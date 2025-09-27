using Godot;
using MageTechDemo.scripts;
using System;
using System.Collections.Generic;
using System.Diagnostics;

[Tool]
public partial class ProcGenTest : Node3D {
	private PackedScene pkdMarker = ResourceLoader.Load<PackedScene>("res://scenes/tests/marker.tscn");
	private NoiseTexture3D noiseTex = ResourceLoader.Load<NoiseTexture3D>("res://assets/noise_3d.tres");
	private Material lineMaterial = ResourceLoader.Load<Material>("res://assets/line_material.tres");
	private Material meshMaterial = ResourceLoader.Load<Material>("res://assets/mesh_material.tres");

	private float frequency = 0.5f;
	/// <summary>
	/// How quickly the noise varies over space.
	/// </summary>
	[Export]
	float Frequency {
		get => frequency;
		set {
			frequency = value;
			if (readyComplete) {
				RebuildMarkers();
			}
		}
	}

	private float amplitude = 2f;
	/// <summary>
	/// The strength of the noise.
	/// </summary>
	[Export]
	float Amplitude {
		get => amplitude;
		set {
			amplitude = value;
			if (readyComplete) {
				RebuildMarkers();
			}
		}
	}

	private float gridSpacing = 1f;
	/// <summary>
	/// How far apart each point on the test grid is from each other.
	/// </summary>
	[Export(PropertyHint.Range, "0,2,0.2")]
	float GridSpacing {
		get => gridSpacing;
		set {
			gridSpacing = value;
			if (readyComplete) {
				RebuildMarkers();
			}
		}
	}

	private float drawPoint = 3f;
	/// <summary>
	/// How far apart each point on the test grid is from each other.
	/// </summary>
	[Export(PropertyHint.Range, "0,20,1")]
	float DrawPoint {
		get => gridSpacing;
		set {
			gridSpacing = value;
			DrawImmediateMesh();
		}
	}

	private float gridSize = 10f;
	/// <summary>
	/// How many points to draw on each axis of the test grid.
	/// </summary>
	[Export(PropertyHint.Range, "0,20,1")]
	float GridSize {
		get => gridSize;
		set {
			gridSize = value;
			if (readyComplete) {
				RebuildMarkers();
			}
		}
	}

	private Vector3 GridOffset { get => new((GridSize - 1) / 2f, (GridSize - 1) / 2f, (GridSize - 1) / 2f); }

	private Node3D markerGroup;
	private Label fpsLabel;
	private MeshInstance3D arrayMeshInstance;
	private MeshInstance3D immediateMeshInstance;


	bool readyComplete = false;

	double timeSinceLastUpdate = 0f;

	Dictionary<int, Marker> markers = [];

	ImmediateMesh triMesh = new();
	ImmediateMesh lineMesh = new();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		markerGroup = GetNode<Node3D>("Markers");
		fpsLabel = GetNode<Label>("Control/MarginContainer/PanelContainer/VBoxContainer/HBoxContainer/FpsValue");
		immediateMeshInstance = GetNode<MeshInstance3D>("ImmediateMeshInstance");
		arrayMeshInstance = GetNode<MeshInstance3D>("ArrayMeshInstance");

		readyComplete = true;

		RebuildMarkers();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
		if (!Engine.IsEditorHint()) {
			fpsLabel.Text = Math.Floor(Engine.GetFramesPerSecond()).ToString();
		}

		if (timeSinceLastUpdate >= 0.1) {
			//DrawImmediateMesh();
			timeSinceLastUpdate = 0f;
		}

		timeSinceLastUpdate += delta;
	}

	// Rebuilds the marker sprites and provides weight values based on the frequency and amplitude
	private void RebuildMarkers() {
		markers.Clear();

		// Clear children from the marker group
		if (markerGroup.GetChildCount() > 0) {
			foreach (var child in markerGroup.GetChildren()) {
				markerGroup.RemoveChild(child);
				child.QueueFree();
			}
		}

		Noise noise = noiseTex.Noise;

		// Set up grid
		for (float x = 0f; x < (GridSize * GridSpacing); x += GridSpacing) {
			for (float z = 0f; z < (GridSize * GridSpacing); z += GridSpacing) {
				for (float y = 0f; y < (GridSize * GridSpacing); y += GridSpacing) {
					Marker marker = pkdMarker.Instantiate<Marker>();
					Vector3 newPos = new Vector3(x, y, z) - GridOffset;
					marker.Position = newPos;

					// Calculate weight distrobution
					//float weight = -marker.Position.Y;
					//weight += noise.GetNoise3Dv(marker.Position * frequency) * amplitude;

					float weight = 5 - (Vector3.Zero.DistanceTo(newPos) * 0.5f);

					marker.Weight = weight;

					//markerGroup.AddChild(marker);
					//marker.Owner = GetTree().EditedSceneRoot;

					markers.Add(GetVectorHash(newPos), marker);
				}
			}
		}

		DrawImmediateMesh();
	}

	private void DrawImmediateMesh() {
		triMesh.ClearSurfaces();
		lineMesh.ClearSurfaces();

		GD.Print($"Drawing mesh for {markers.Count} points");

		triMesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
		lineMesh.SurfaceBegin(Mesh.PrimitiveType.Lines);

		// Render mesh from points
		foreach (Marker marker in markers.Values) {
			Vector3 cubeOrigin = marker.Position;

			List<Vector3> cubeCorners = [];
			List<float> cubeWeights = [];

			for (int v = 0; v < 8; v++) {
				Vector3 offsetPos = cubeOrigin + GenerationTable.originOffsets[v];

				cubeCorners.Add(offsetPos);

				if (markers.TryGetValue(GetVectorHash(offsetPos), out Marker offsetMarker)) {
					cubeWeights.Add(offsetMarker.Weight);
				}
				else {
					cubeWeights.Add(-1f);
				}
			}

			Cube cube = new([.. cubeCorners], [.. cubeWeights], 3);

			var triangles = cube.GetTriangles();

			if (triangles.Count == 0) {
				continue;
			}

			foreach (Triangle tri in triangles) {
				// Calculate the normal of the whole face, and apple to each vertex
				Vector3 vertA = tri.A;
				Vector3 vertB = tri.B;
				Vector3 vertC = tri.C;

				Vector3 normal = (vertB - vertA) * (vertC - vertA);
				normal = normal.Normalized();

				float avgX = (vertA.X + vertB.X + vertC.X) / 3;
				float avgY = (vertA.Y + vertB.Y + vertC.Y) / 3;
				float avgZ = (vertA.Z + vertB.Z + vertC.Z) / 3;

				Vector3 triAvg = new(avgX, avgY, avgZ);

				triMesh.SurfaceSetNormal(triAvg.Normalized());
				triMesh.SurfaceAddVertex(tri.A);

				triMesh.SurfaceSetNormal(triAvg.Normalized());
				triMesh.SurfaceAddVertex(tri.B);

				triMesh.SurfaceSetNormal(triAvg.Normalized());
				triMesh.SurfaceAddVertex(tri.C);

				lineMesh.SurfaceSetColor(Colors.LimeGreen);
				lineMesh.SurfaceAddVertex(triAvg);

				lineMesh.SurfaceSetColor(Colors.LimeGreen);
				lineMesh.SurfaceAddVertex(triAvg + triAvg.Normalized());
			}
		}

		triMesh.SurfaceEnd();
		lineMesh.SurfaceEnd();

		MeshInstance3D meshInstance = new();
		meshInstance.Mesh = triMesh;
		meshInstance.MaterialOverride = meshMaterial;
		AddChild(meshInstance);

		// MeshInstance3D lineMeshInstance = new();
		// lineMeshInstance.Mesh = lineMesh;
		// lineMeshInstance.MaterialOverride = lineMaterial;
		// AddChild(lineMeshInstance);
	}

	/// <summary>
	/// Create a hashed index that can be used to quickly look up a vector.
	/// </summary>
	public int GetVectorHash(Vector3 vector) {
		double x = Math.Round(Convert.ToDouble(vector.X), 3);
		double y = Math.Round(Convert.ToDouble(vector.Y), 3);
		double z = Math.Round(Convert.ToDouble(vector.Z), 3);

		var tuple = (x, y, z);

		return tuple.GetHashCode();
	}
}

public static class NodeExtensions {
	public static void ClearChildren(this Node node) {
		foreach (Node child in node.GetChildren()) {
			// Remove from tree first
			if (child.IsInsideTree()) {
				child.GetParent().RemoveChild(child);
			}

			// Delete and free
			child.QueueFree();
		}
	}
}