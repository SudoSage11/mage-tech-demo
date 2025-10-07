#nullable disable

using Godot;

public static class NodeUtils
{
  /// <summary>
  /// Clear all children from the specified node. Will remove from tree first if possible, then <c>QueueFree()</c>.
  /// </summary>
  /// <param name="node">The node to clear children from.</param>
  public static void ClearChildren(this Node node)
  {
    foreach (Node child in node.GetChildren())
    {
      // Remove from tree first
      if (child.IsInsideTree())
      {
        child.GetParent().RemoveChild(child);
      }

      // Delete and free
      child.QueueFree();
    }
  }
}