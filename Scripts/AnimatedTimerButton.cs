using Godot;
using System;

public class AnimatedTimerButton : Button
{
    [Export] private NodePath AnimationPlayerNodePath;
    [Export] private NodePath IconTextureRectNodePath;

    [Export] private string NormalAnimationName;
    [Export] private string HoveredAnimationName;
    [Export] private string ButtonDownAnimationName;

    private AnimationPlayer AnimationPlayer;
    private TextureRect IconTextureRect;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        AnimationPlayer = GetNode<AnimationPlayer>(AnimationPlayerNodePath);
        IconTextureRect = GetNode<TextureRect>(IconTextureRectNodePath);

        IconTextureRect.RectPivotOffset = IconTextureRect.RectSize / 2.0f;
    }

    private void OnAnimatedTimerButtonButtonDown()
    {
        AnimationPlayer.Play(ButtonDownAnimationName);
    }

    private void OnAnimatedTimerButtonButtonUp()
    {
        AnimationPlayer.Play(HoveredAnimationName);
    }

    private void OnAnimatedTimerButtonMouseEntered()
    {
        AnimationPlayer.Play(HoveredAnimationName);
    }

    private void OnAnimatedTimerButtonMouseExited()
    {
        AnimationPlayer.Play(NormalAnimationName);
    }
}
