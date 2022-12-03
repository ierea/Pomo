using Godot;
using System;

/// <summary>
/// Handles animations for the options popup.
/// </summary>
public class OptionsPopup : Control
{
	[Export] private NodePath AnimationPlayerNodePath;
	[Export] private NodePath PopupPanelNodePath;

	[Export] private string ShowAnimationName;
	[Export] private string HideAnimationName;

	private AnimationPlayer AnimationPlayer;
	private Control PopupPanel;

	/// <summary>
	/// Called when the node enters the scene tree for the first time.
	/// </summary>
	public override void _Ready()
	{
		Visible = false;
		AnimationPlayer = GetNode<AnimationPlayer>(AnimationPlayerNodePath);
		PopupPanel = GetNode<Control>(PopupPanelNodePath);
	}

	/// <summary>
	/// Show the options popup with animation.
	/// </summary>
	public void ShowOptionsPopup()
	{
		PopupPanel.RectPivotOffset = PopupPanel.RectSize / 2.0f;
		AnimationPlayer.Play(ShowAnimationName);
	}

	/// <summary>
	/// Hide the options popup with animation.
	/// </summary>
	public void HideOptionsPopup()
	{
		PopupPanel.RectPivotOffset = PopupPanel.RectSize / 2.0f;
		AnimationPlayer.Play(HideAnimationName);
	}
}
