using Godot;
using System;

/// <summary>
/// Handles animations for animated color picker buttons.
/// </summary>
public class AnimatedColorPickerButton : ColorPickerButton
{
    [Export] private NodePath AnimationPlayerNodePath;
    [Export] private NodePath IconTextureRectNodePath;

    [Export] private string NormalAnimationName;
    [Export] private string HoveredAnimationName;
    [Export] private string ButtonDownAnimationName;

    private AnimationPlayer AnimationPlayer;
    private TextureRect IconTextureRect;

    /// <summary>
    /// Called when the node enters the scene tree for the first time.
    /// </summary>
    public override void _Ready()
    {
        AnimationPlayer = GetNode<AnimationPlayer>(AnimationPlayerNodePath);
        IconTextureRect = GetNode<TextureRect>(IconTextureRectNodePath);

        IconTextureRect.RectPivotOffset = IconTextureRect.RectSize / 2.0f;
    }

    /// <summary>
    /// Signal Receiver Method.<br/>
    /// For use with the animated color picker button when it is pressed down.<br/>
    /// Plays the button down animation.
    /// </summary>
    private void OnAnimatedColorPickerButtonButtonDown()
    {
        AnimationPlayer.Play(ButtonDownAnimationName);
    }

    /// <summary>
    /// Signal Receiver Method.<br/>
    /// For use with the animated color picker button when it is released.<br/>
    /// Plays the button up animation.
    /// </summary>
    private void OnAnimatedColorPickerButtonButtonUp()
    {
        AnimationPlayer.Play(HoveredAnimationName);
    }

    /// <summary>
    /// Signal Receiver Method.<br/>
    /// For use with the animated color picker button when it is hovered.<br/>
    /// Plays the hovered animation.
    /// </summary>
    private void OnAnimatedColorPickerButtonMouseEntered()
    {
        AnimationPlayer.Play(HoveredAnimationName);
    }

    /// <summary>
    /// Signal Receiver Method.<br/>
    /// For use with the animated color picker button when it is no longer hovered.<br/>
    /// Plays the normal state animation.
    /// </summary>
    private void OnAnimatedColorPickerButtonMouseExited()
    {
        AnimationPlayer.Play(NormalAnimationName);
    }
}
