using Godot;
using System;

public class Main : Node
{
    [Export] private Vector2 MinimumWindowSize;
    
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        OS.MinWindowSize = MinimumWindowSize;
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
