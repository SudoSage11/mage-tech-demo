#nullable disable

using Godot;
using System;
using System.Collections.Generic;

public abstract partial class ControlPoint : Sprite3D
{
  private Area3D moveInteractionArea;
  private bool selected;
  private Vector3 targetPosition;

  private float movementSpeed = 20f;

  public override void _Ready()
  {
    moveInteractionArea = GetNode<Area3D>("MoveInteractionArea");
    moveInteractionArea.InputEvent += OnMoveInteractionAreaInputEvent;
  }

  public override void _Process(double delta)
  {
    if (selected)
    {
      Camera3D camera = GetViewport().GetCamera3D();

      Vector3 origin = camera.ProjectRayOrigin(GetViewport().GetMousePosition());
      Vector3 direction = camera.ProjectRayNormal(GetViewport().GetMousePosition());
      float depth = origin.DistanceTo(GlobalPosition);
      Vector3 pos = origin + direction * depth;

      Vector3 groundPos = new(pos.X, 0, pos.Z);

      if (selected)
      {
        targetPosition = groundPos;
      }

      if (GlobalPosition != targetPosition)
      {
        GlobalPosition = GlobalPosition.Lerp(targetPosition, movementSpeed * (float)delta);
      }
    }
  }

  /// <summary>
  /// Calculates what the weight of a mesh point at a given position would be.
  /// </summary>
  /// <param name="meshPointPos">The position of a mesh point that's needs a calculated weight.</param>
  /// <returns>The weight of the mesh point at that position.</returns>
  public abstract float CalculateMeshPointWeight(Vector3 meshPointPos);

  public override void _UnhandledInput(InputEvent input)
  {
    if (input.IsActionReleased("debug_move_element"))
    {
      selected = false;
      targetPosition = GlobalPosition;
    }
  }

  private void OnMoveInteractionAreaInputEvent(Node camera, InputEvent inputEvent, Vector3 eventPosition, Vector3 normal, long shapeIdx)
  {
    if (inputEvent.IsActionPressed("debug_move_element"))
    {
      selected = true;
    }
  }
}
