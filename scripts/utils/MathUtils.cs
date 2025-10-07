#nullable disable

using System;

public static class MathUtils
{
  public static float RoundToNearestGivenFloat(this float value, float nearestFloat)
  {
    if (nearestFloat == 0)
    {
      // Handle division by zero or a desired behavior for rounding to zero
      return 0;
    }

    // Scale the value by dividing by the target float
    float scaledValue = value / nearestFloat;

    // Round the scaled value to the nearest whole number
    float roundedScaledValue = MathF.Round(scaledValue, MidpointRounding.AwayFromZero);

    // Scale the rounded value back by multiplying by the target float
    float result = roundedScaledValue * nearestFloat;

    return result;
  }
}