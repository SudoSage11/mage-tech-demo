using System;
using Godot;

public class VectorUtils {
  /// <summary>
	/// Create a hashed index that can be used to quickly look up a vector.
	/// </summary>
  public static int GetVectorHash(Vector3 vector) {
    double x = Math.Round(Convert.ToDouble(vector.X), 3);
    double y = Math.Round(Convert.ToDouble(vector.Y), 3);
    double z = Math.Round(Convert.ToDouble(vector.Z), 3);

    var tuple = (x, y, z);

    return tuple.GetHashCode();
  }
}