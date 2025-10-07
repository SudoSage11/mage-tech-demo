#nullable disable

using System;
using Godot;

public class VectorUtils
{
  /// <summary>
	/// Create a hashed index that can be used to quickly look up a vector.
	/// </summary>
  public static int GetVectorHash(Vector3 vector)
  {
    double x = Math.Round(Convert.ToDouble(vector.X), 3);
    double y = Math.Round(Convert.ToDouble(vector.Y), 3);
    double z = Math.Round(Convert.ToDouble(vector.Z), 3);

    var tuple = (x, y, z);

    return tuple.GetHashCode();
  }

  /// <summary>
  /// A magical function I didn't come up with to encode a Vector3 in a Vector2.
  /// 
  /// NOTE: Only encodes normalized Vectors.
  /// 
  /// TODO: Learn exactly how this works
  /// </summary>
  public static Vector2 EncodeOctahedron(Vector3 vec3)
  {
    // Ensure normalized input
    vec3 = vec3.Normalized();

    // Project onto octahedron
    vec3 /= Mathf.Abs(vec3.X) + Mathf.Abs(vec3.Y) + Mathf.Abs(vec3.Z);

    Vector2 vec2 = new(vec3.X, vec3.Y);

    // Fold lower hemisphere
    if (vec3.Z < 0.0f)
    {
      vec2 = new Vector2(
          (1.0f - Mathf.Abs(vec2.Y)) * Mathf.Sign(vec2.X),
          (1.0f - Mathf.Abs(vec2.X)) * Mathf.Sign(vec2.Y)
      );
    }

    // Map from [-1,1] to [0,1]
    return (vec2 * 0.5f) + new Vector2(0.5f, 0.5f);
  }
}