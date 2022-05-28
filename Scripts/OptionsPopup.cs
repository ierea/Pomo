using Godot;
using System;

public class OptionsPopup : Control
{
    [Export] private NodePath AnimationPlayerNodePath;
    [Export] private NodePath PopupPanelNodePath;

    [Export] private string ShowAnimationName;
    [Export] private string HideAnimationName;

    private AnimationPlayer AnimationPlayer;
    private Control PopupPanel;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Visible = false;

        AnimationPlayer = GetNode<AnimationPlayer>(AnimationPlayerNodePath);

        PopupPanel = GetNode<Control>(PopupPanelNodePath);
    }

    public void ShowOptionsPopup()
    {
        PopupPanel.RectPivotOffset = PopupPanel.RectSize / 2.0f;
        AnimationPlayer.Play(ShowAnimationName);
    }

    public void HideOptionsPopup()
    {
        PopupPanel.RectPivotOffset = PopupPanel.RectSize / 2.0f;
        AnimationPlayer.Play(HideAnimationName);
    }
}
