using Godot;
using System;

public partial class ProcGenTest : Node3D {
	private PackedScene pdkMarker = ResourceLoader.Load<PackedScene>("res://scenes/tests/marker.tscn");
	private NoiseTexture3D noiseTex = ResourceLoader.Load<NoiseTexture3D>("res://assets/noise_3d.tres");

	private float frequency = 0.5f;
	[Export]
	float Frequency {
		get => frequency;
		set {
			frequency = value;
			RebuildMarkers();
		}
	}

	private float amplitude = 2f;
	[Export]
	float Amplitude {
		get => amplitude;
		set {
			amplitude = value;
			RebuildMarkers();
		}
	}

	const float gridSize = 5f;

	private Vector3 transform = new((gridSize - 1) / 2f, 0f, (gridSize - 1) / 2f);

	private Node3D markerGroup;
	private Label fpsLabel;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		markerGroup = GetNode<Node3D>("Markers");
		fpsLabel = GetNode<Label>("Control/MarginContainer/PanelContainer/VBoxContainer/HBoxContainer/FpsValue");

		RebuildMarkers();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
		fpsLabel.Text = Math.Floor(Engine.GetFramesPerSecond()).ToString();
	}

	private void RebuildMarkers() {
		// Clear children from the marker group
		foreach (var child in markerGroup.GetChildren()) {
			markerGroup.RemoveChild(child);
			child.QueueFree();
		}

		Noise noise = noiseTex.Noise;

		// Set up grid
		for (int x = 0; x < gridSize; x++) {
			for (int y = 0; y < gridSize; y++) {
				for (int z = 0; z < gridSize; z++) {
					Marker marker = pdkMarker.Instantiate<Marker>();
					marker.Position = new Vector3(x, y, z) - transform;

					// Calculate weight distrobution
					float weight = marker.Position.Y;
					weight += noise.GetNoise3Dv(marker.Position * frequency) * amplitude;

					marker.Weight = weight;

					markerGroup.AddChild(marker);
					marker.Owner = GetTree().EditedSceneRoot;
				}
			}
		}
	}
}
