#nullable disable

using Godot;
using System;
using System.Runtime.ConstrainedExecution;

public partial class DebugCamera : Camera3D
{
	[Export]
	float mouseSensitivity = 0.005f;

	[Export]
	float controllerSensitivity = 0.01f;

	[Export]
	float moveSpeed = 10f;

	bool lookEnabled = false;

	public override void _UnhandledInput(InputEvent input)
	{
		// Look
		if (input.IsActionPressed("cam_enable_look"))
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
			lookEnabled = true;
		}
		else if (input.IsActionReleased("cam_enable_look"))
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
			lookEnabled = false;
		}

		// Reset
		if (input.IsActionPressed("cam_reset"))
		{
			Position = new Vector3(2.5f, 1.5f, 4.5f);
			Rotation = new Vector3(-0.4f, 0.67f, 0f);
		}
	}

	public override void _Process(double delta)
	{
		Vector3 dir = Vector3.Zero;

		if (Input.IsActionPressed("move_forward"))
		{
			dir -= GlobalBasis.Z; // Move forward
		}
		if (Input.IsActionPressed("move_backward"))
		{
			dir += GlobalBasis.Z; // Move backward
		}
		if (Input.IsActionPressed("move_left"))
		{
			dir -= GlobalBasis.X; // Move left
		}
		if (Input.IsActionPressed("move_right"))
		{
			dir += GlobalBasis.X; // Move right
		}
		if (Input.IsActionPressed("move_up"))
		{
			dir += GlobalBasis.Y; // Move left
		}
		if (Input.IsActionPressed("move_down"))
		{
			dir -= GlobalBasis.Y; // Move right
		}

		if (dir != Vector3.Zero)
		{
			dir = dir.Normalized();
			GlobalTranslate(dir * moveSpeed * (float)delta);
		}

		// Controller look input
		Vector2 lookDirection = Input.GetVector("look_left", "look_right", "look_up", "look_down");

		if (lookDirection != Vector2.Zero)
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;

			var rotationX = new Quaternion(Vector3.Up, -lookDirection.X * controllerSensitivity);
			var rotationY = new Quaternion(Vector3.Right, -lookDirection.Y * controllerSensitivity);

			GlobalBasis = new Basis(rotationX) * GlobalBasis * new Basis(rotationY);
		}
	}

	public override void _Input(InputEvent input)
	{
		// Mouse look input 
		if (input is InputEventMouseMotion motionInput)
		{
			if (lookEnabled)
			{
				var rotationX = new Quaternion(Vector3.Up, -motionInput.Relative.X * mouseSensitivity);
				var rotationY = new Quaternion(Vector3.Right, -motionInput.Relative.Y * mouseSensitivity);

				GlobalBasis = new Basis(rotationX) * GlobalBasis * new Basis(rotationY);
			}
			else
			{
				Input.MouseMode = Input.MouseModeEnum.Visible;
			}
		}
	}
}
