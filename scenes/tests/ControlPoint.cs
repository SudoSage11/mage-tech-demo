#nullable disable

using Godot;
using System;
using System.Collections.Generic;

public abstract partial class ControlPoint : Sprite3D
{
  /// <summary>
  /// Calculates what the weight of a mesh point at a given position would be.
  /// </summary>
  /// <param name="meshPointPos">The position of a mesh point that's needs a calculated weight.</param>
  /// <returns>The weight of the mesh point at that position.</returns>
  public abstract float CalculateMeshPointWeight(Vector3 meshPointPos);
}
