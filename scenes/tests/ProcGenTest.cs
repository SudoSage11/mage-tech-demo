#nullable disable

using Config;
using Godot;
using MageTechDemo.scripts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public partial class ProcGenTest : Node3D
{
	private Label fpsLabel;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		fpsLabel = GetNode<Label>("Control/MarginContainer/PanelContainer/VBoxContainer/HBoxContainer/FpsValue");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (!Engine.IsEditorHint())
		{
			fpsLabel.Text = Math.Floor(Engine.GetFramesPerSecond()).ToString();
		}
	}

	private void OnSpawnPressed()
	{
		GD.Print("Pressed");
	}

	private void OnCheckDrawGeneratedMeshesToggled(bool active)
	{
		ConfigSettings.Instance.DrawGeneratedMeshes = active;
	}

	private void OnCheckDrawDebugPointsToggled(bool active)
	{
		ConfigSettings.Instance.DrawDebugPoints = active;
	}

	private void OnCheckDrawDebugLinesToggled(bool active)
	{
		ConfigSettings.Instance.DrawDebugLines = active;
	}
}