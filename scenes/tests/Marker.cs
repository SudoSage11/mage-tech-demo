using Godot;
using System;

public partial class Marker : Sprite3D {
	[Export(PropertyHint.Range, "0,1")]
	public float Weight {
		get {
			return weight;
		}
		set {
			weight = value;
			Modulate = new Color(weight / 10f, weight / 10f, weight / 10f);
		}
	}
	private float weight;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
	}
}
