using Godot;
using System;

/// <summary>
/// Handles basic window logic.
/// </summary>
public class Main : Node
{
    [Export] private Vector2 MinimumWindowSize;

    /// <summary>
    /// Called when the node enters the scene tree for the first time.
    /// </summary>
    public override void _Ready()
    {
        OS.MinWindowSize = MinimumWindowSize;
    }
}
