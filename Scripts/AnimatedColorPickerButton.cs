using Godot;
using System;

public class AnimatedColorPickerButton : ColorPickerButton
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

    private void OnAnimatedColorPickerButtonButtonDown()
    {
        AnimationPlayer.Play(ButtonDownAnimationName);
    }

    private void OnAnimatedColorPickerButtonButtonUp()
    {
        AnimationPlayer.Play(HoveredAnimationName);
    }

    private void OnAnimatedColorPickerButtonMouseEntered()
    {
        AnimationPlayer.Play(HoveredAnimationName);
    }

    private void OnAnimatedColorPickerButtonMouseExited()
    {
        AnimationPlayer.Play(NormalAnimationName);
    }
}
