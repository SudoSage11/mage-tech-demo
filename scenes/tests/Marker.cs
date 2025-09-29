using Godot;
using System;

public partial class Marker : Sprite3D {
	[Export(PropertyHint.Range, "0,10")]
	public float Weight {
		get {
			return weight;
		}
		set {
			weight = value;
			Modulate = new Color(1 - (weight / 5), 1 - (weight / 5), 1 - (weight / 5));
		}
	}
	private float weight = 0;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
	}
}
