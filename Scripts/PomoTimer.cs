using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// The main timer app logic handler.
/// </summary>
public class PomoTimer : Control
{
	/// <summary>
	/// Timer phase, following the Pomodoro technique.
	/// </summary>
	enum Phase
	{
		Work,
		ShortBreak,
		LongBreak
	}

	#region Node Paths
	[Export] private NodePath AudioStreamPlayerNodePath;
	[Export] private NodePath TimeLabelNodePath;
	[Export] private NodePath PauseButtonNodePath;
	[Export] private NodePath PauseButtonTextureRectNodePath;
	[Export] private NodePath ResetButtonNodePath;
	[Export] private NodePath UpperTimerTextureRectNodePath;
	[Export] private NodePath LowerTimerTextureRectNodePath;

	[Export] private NodePath PinWindowButtonNodePath;
	[Export] private NodePath PinWindowButtonTextureRectNodePath;

	[Export] private NodePath OptionsPopupNodePath;

	[Export] private NodePath OptionsResetSfxVolumeButtonNodePath;
	[Export] private NodePath OptionsSfxVolumeSliderNodePath;

	[Export] private NodePath OptionsResetUpperTimerColorButtonNodePath;
	[Export] private NodePath OptionsUpperTimerColorPickerButtonNodePath;
	[Export] private NodePath OptionsUpperTimerColorPickerTextureRectNodePath;

	[Export] private NodePath OptionsResetLowerTimerColorButtonNodePath;
	[Export] private NodePath OptionsLowerTimerColorPickerButtonNodePath;
	[Export] private NodePath OptionsLowerTimerColorPickerTextureRectNodePath;

	[Export] private NodePath OptionsResetWorkTimerDurationButtonNodePath;
	[Export] private NodePath OptionsWorkTimerDurationLineEditNodePath;

	[Export] private NodePath OptionsResetShortBreakTimerDurationButtonNodePath;
	[Export] private NodePath OptionsShortBreakTimerDurationLineEditNodePath;

	[Export] private NodePath OptionsResetLongBreakTimerDurationButtonNodePath;
	[Export] private NodePath OptionsLongBreakTimerDurationLineEditNodePath;

	[Export] private NodePath OptionsResetLongBreakFrequencyButtonNodePath;
	[Export] private NodePath OptionsLongBreakFrequencyLineEditNodePath;

	[Export] private List<NodePath> FadeableInterfaceCanvasItemNodePaths;
	#endregion

	#region Resources
	[Export] private Texture PauseTexture;
	[Export] private Texture PlayTexture;
	[Export] private Texture PinWindowTexture;
	[Export] private Texture UnpinWindowTexture;

	[Export] private AudioStream WorkPhaseStartSfx;
	[Export] private AudioStream ShortBreakPhaseStartSfx;
	[Export] private AudioStream LongBreakPhaseStartSfx;
	#endregion

	#region File Names
	[Export] private string UserPreferencesFileName;
	#endregion

	#region Input Action Names
	[Export] private string PauseTimerActionName;
	[Export] private string PlayTimerActionName;
	[Export] private string ResetTimerActionName;
	[Export] private string OpenOptionsActionName;
	[Export] private string CloseOptionsActionName;
	[Export] private string PinWindowActionName;
	[Export] private string UnpinWindowActionName;
	#endregion

	#region Constants
	private const int SpeedMultiplier = 1;
	private const int MillisecondsInASecond = 1000;
	private const int SecondsInAMinute = 60;

	private const int AbsoluteMinimumMinutesPerPhase = 1;
	private const int AbsoluteMaximumMinutesPerPhase = 9999;

	private const int AbsoluteMinimumWorkPhasesPerLongBreak = 1;
	private const int AbsoluteMaximumWorkPhasesPerLongBreak = 9999;

	private const float SecondsBeforeInterfaceFade = 2.0f;
	private const float InterfaceFadeDuration = 0.5f;
	#endregion

	#region Nodes
	private AudioStreamPlayer AudioStreamPlayer;
	private Label TimeLabel;
	private Button PauseButton;
	private TextureRect PauseButtonTextureRect;
	private Button ResetButton;
	private TextureRect UpperTimerTextureRect;
	private TextureRect LowerTimerTextureRect;
	private Button PinWindowButton;
	private TextureRect PinWindowButtonTextureRect;

	private OptionsPopup OptionsPopup;

	private Button OptionsResetSfxVolumeButton;
	private HSlider OptionsSfxVolumeSlider;

	private Button OptionsResetTimerUpperColorButton;
	private ColorPickerButton OptionsTimerUpperColorPickerButton;
	private TextureRect OptionsTimerUpperColorPickerTextureRect;

	private Button OptionsResetTimerLowerColorButton;
	private ColorPickerButton OptionsTimerLowerColorPickerButton;
	private TextureRect OptionsTimerLowerColorPickerTextureRect;

	private Button OptionsResetWorkTimerDurationButton;
	private LineEdit OptionsWorkTimerDurationLineEdit;

	private Button OptionsResetShortBreakTimerDurationButton;
	private LineEdit OptionsShortBreakTimerDurationLineEdit;

	private Button OptionsResetLongBreakTimerDurationButton;
	private LineEdit OptionsLongBreakTimerDurationLineEdit;

	private Button OptionsResetLongBreakFrequencyButton;
	private LineEdit OptionsLongBreakFrequencyLineEdit;

	private List<CanvasItem> FadeableInterfaceCanvasItems = new List<CanvasItem>();

	private UserPreferences userPreferences;
	private UserPreferences defaultUserPreferences;
	#endregion

	#region App State
	private Phase currentPhase;
	private int currentMinutesRemaining;
	private int currentSecondsRemaining;
	private float currentMillisecondsRemaining;
	private int currentPhaseTotalMinutes;
	private bool timerActive;
	private bool freshSessionAndShouldPlaySfxOnPlay;
	private int workPhasesSinceLongBreak;
	
	// Interface Fading
	private float currentSecondsBeforeInterfaceFade;
	private bool interfaceShouldFade;
	private float interfaceAlphaPercentage;
	#endregion

	/// <summary>
	/// Called when the node enters the scene tree for the first time.
	/// </summary>
	public override void _Ready()
	{
		defaultUserPreferences = new UserPreferences();
		userPreferences = UserPreferences.CreateFromFile(UserPreferencesFileName);

		OS.WindowSize = userPreferences.WindowSize;

		ResetTimerValues();

		TimeLabel = GetNode<Label>(TimeLabelNodePath);
		PauseButton = GetNode<Button>(PauseButtonNodePath);
		PauseButtonTextureRect = GetNode<TextureRect>(PauseButtonTextureRectNodePath);
		ResetButton = GetNode<Button>(ResetButtonNodePath);

		UpperTimerTextureRect = GetNode<TextureRect>(UpperTimerTextureRectNodePath);
		LowerTimerTextureRect = GetNode<TextureRect>(LowerTimerTextureRectNodePath);

		PinWindowButton = GetNode<Button>(PinWindowButtonNodePath);
		if (!PlatformAllowsWindowPinning())
		{
			PinWindowButton.Visible = false;
			PinWindowButton.Disabled = true;
		}

		PinWindowButtonTextureRect = GetNode<TextureRect>(PinWindowButtonTextureRectNodePath);

		OptionsPopup = GetNode<OptionsPopup>(OptionsPopupNodePath);

		OptionsResetTimerUpperColorButton = GetNode<Button>(OptionsResetUpperTimerColorButtonNodePath);
		OptionsTimerUpperColorPickerButton = GetNode<ColorPickerButton>(OptionsUpperTimerColorPickerButtonNodePath);
		OptionsTimerUpperColorPickerTextureRect = GetNode<TextureRect>(OptionsUpperTimerColorPickerTextureRectNodePath);

		OptionsResetTimerLowerColorButton = GetNode<Button>(OptionsResetLowerTimerColorButtonNodePath);
		OptionsTimerLowerColorPickerButton = GetNode<ColorPickerButton>(OptionsLowerTimerColorPickerButtonNodePath);
		OptionsTimerLowerColorPickerTextureRect = GetNode<TextureRect>(OptionsLowerTimerColorPickerTextureRectNodePath);

		OptionsResetWorkTimerDurationButton = GetNode<Button>(OptionsResetWorkTimerDurationButtonNodePath);
		OptionsWorkTimerDurationLineEdit = GetNode<LineEdit>(OptionsWorkTimerDurationLineEditNodePath);
		OptionsWorkTimerDurationLineEdit.Text = userPreferences.WorkMinutes.ToString();

		OptionsResetShortBreakTimerDurationButton = GetNode<Button>(OptionsResetShortBreakTimerDurationButtonNodePath);
		OptionsShortBreakTimerDurationLineEdit = GetNode<LineEdit>(OptionsShortBreakTimerDurationLineEditNodePath);
		OptionsShortBreakTimerDurationLineEdit.Text = userPreferences.ShortBreakMinutes.ToString();

		OptionsResetLongBreakTimerDurationButton = GetNode<Button>(OptionsResetLongBreakTimerDurationButtonNodePath);
		OptionsLongBreakTimerDurationLineEdit = GetNode<LineEdit>(OptionsLongBreakTimerDurationLineEditNodePath);
		OptionsLongBreakTimerDurationLineEdit.Text = userPreferences.LongBreakMinutes.ToString();

		OptionsResetLongBreakFrequencyButton = GetNode<Button>(OptionsResetLongBreakFrequencyButtonNodePath);
		OptionsLongBreakFrequencyLineEdit = GetNode<LineEdit>(OptionsLongBreakFrequencyLineEditNodePath);
		OptionsLongBreakFrequencyLineEdit.Text = userPreferences.LongBreakFrequency.ToString();

		foreach (var fadeableInterfaceCanvasItemNodePath in FadeableInterfaceCanvasItemNodePaths)
		{
			FadeableInterfaceCanvasItems.Add(GetNode<CanvasItem>(fadeableInterfaceCanvasItemNodePath));
		}

		AudioStreamPlayer = GetNode<AudioStreamPlayer>(AudioStreamPlayerNodePath);
		OptionsResetSfxVolumeButton = GetNode<Button>(OptionsResetSfxVolumeButtonNodePath);
		OptionsSfxVolumeSlider = GetNode<HSlider>(OptionsSfxVolumeSliderNodePath);
		OptionsSfxVolumeSlider.Value = userPreferences.SfxVolume;
		SetSfxVolume(userPreferences.SfxVolume);

		UpdateTimerText();
		UpdateTimerRectSizes();
		UpdateTimerRectColors();
		UpdateColorPickerTextureRects();
	}

	/// <summary>
	/// Called every frame. 'delta' is the elapsed time since the previous frame.
	/// </summary>
	/// <param name="delta">Time in seconds since the previous frame.</param>
	public override void _Process(float delta)
	{
		if (timerActive)
		{
			currentMillisecondsRemaining -= delta * MillisecondsInASecond * SpeedMultiplier;

			while (currentMillisecondsRemaining < 0.0f)
			{
				currentSecondsRemaining -= 1;
				currentMillisecondsRemaining += MillisecondsInASecond;
			}

			while (currentSecondsRemaining < 0)
			{
				currentMinutesRemaining -= 1;
				currentSecondsRemaining += SecondsInAMinute;
			}

			if (currentMinutesRemaining < 0)
			{
				currentMinutesRemaining = 0;
				GoToNextTimerPhase();
			}

			UpdateTimerText();
			UpdateTimerRectSizes();
		}

		if (!OptionsPopup.Visible)
		{
			if (Input.IsActionJustPressed(PauseTimerActionName) ||
				Input.IsActionJustPressed(PlayTimerActionName))
			{
				TogglePauseTimer();
			}

			if (Input.IsActionJustPressed(ResetTimerActionName))
			{
				ResetTimer();
			}

			if (Input.IsActionJustPressed(OpenOptionsActionName))
			{
				ShowOptionsPopup();
			}

			if (Input.IsActionJustPressed(PinWindowActionName) ||
				Input.IsActionJustPressed(UnpinWindowActionName))
			{
				if (PlatformAllowsWindowPinning())
				{
					TogglePinWindow();
				}
			}
		}
		else
		{
			if (Input.IsActionJustPressed(CloseOptionsActionName))
			{
				CloseOptionsPopup();
			}
		}
		
		UpdateInterfaceFade(delta);
	}

	/// <summary>
	/// Update the main timer's text to display the current remaining time.
	/// </summary>
	private void UpdateTimerText()
	{
		TimeLabel.Text = currentMinutesRemaining + ":" + currentSecondsRemaining.ToString("0#");
	}

	/// <summary>
	/// Update the relative sizes of the timer's upper and lower rects according to the current remaining time.
	/// </summary>
	private void UpdateTimerRectSizes()
	{
		float millisecondsRemaining = currentMinutesRemaining * SecondsInAMinute * MillisecondsInASecond + currentSecondsRemaining * MillisecondsInASecond + currentMillisecondsRemaining;

		switch (currentPhase)
		{
			case Phase.Work:
				{
					int totalWorkMilliseconds = currentPhaseTotalMinutes * SecondsInAMinute * MillisecondsInASecond;

					UpperTimerTextureRect.SizeFlagsStretchRatio = 1.0f - millisecondsRemaining / totalWorkMilliseconds;
					LowerTimerTextureRect.SizeFlagsStretchRatio = millisecondsRemaining / totalWorkMilliseconds;
					break;
				}
			case Phase.ShortBreak:
				{
					int totalShortBreakMilliseconds = currentPhaseTotalMinutes * SecondsInAMinute * MillisecondsInASecond;

					UpperTimerTextureRect.SizeFlagsStretchRatio = millisecondsRemaining / totalShortBreakMilliseconds;
					LowerTimerTextureRect.SizeFlagsStretchRatio = 1.0f - millisecondsRemaining / totalShortBreakMilliseconds;
					break;
				}
			case Phase.LongBreak:
				{
					int totalLongBreakMilliseconds = currentPhaseTotalMinutes * SecondsInAMinute * MillisecondsInASecond;

					UpperTimerTextureRect.SizeFlagsStretchRatio = millisecondsRemaining / totalLongBreakMilliseconds;
					LowerTimerTextureRect.SizeFlagsStretchRatio = 1.0f - millisecondsRemaining / totalLongBreakMilliseconds;
					break;
				}
		}
	}

	/// <summary>
	/// Update the colors of the timer's rects according to the colors stored in user preferences.
	/// </summary>
	private void UpdateTimerRectColors()
	{
		UpperTimerTextureRect.Modulate = userPreferences.UpperTimerColor;
		LowerTimerTextureRect.Modulate = userPreferences.LowerTimerColor;
	}

	/// <summary>
	/// Update the colors displayed over the color picker buttons according to the currently stored user preferences.
	/// </summary>
	private void UpdateColorPickerTextureRects()
	{
		OptionsTimerUpperColorPickerButton.Color = userPreferences.UpperTimerColor;
		OptionsTimerLowerColorPickerButton.Color = userPreferences.LowerTimerColor;

		OptionsTimerUpperColorPickerTextureRect.Modulate = userPreferences.UpperTimerColor;
		OptionsTimerLowerColorPickerTextureRect.Modulate = userPreferences.LowerTimerColor;
	}

	/// <summary>
	/// Update the interface fading.
	/// </summary>
	/// <param name="deltaTime">Delta time.</param>
	private void UpdateInterfaceFade(float deltaTime)
	{
		if (!timerActive)
		{
			interfaceShouldFade = false;
			currentSecondsBeforeInterfaceFade = SecondsBeforeInterfaceFade;
			interfaceAlphaPercentage = 1.0f;
		}
		else
		{
			if (!interfaceShouldFade)
			{
				currentSecondsBeforeInterfaceFade -= deltaTime;

				if (currentSecondsBeforeInterfaceFade < 0.0f)
				{
					interfaceShouldFade = true;
				}
			}
			else
			{
				float newInterfaceAlphaPercentage = interfaceAlphaPercentage - (deltaTime / InterfaceFadeDuration);
				interfaceAlphaPercentage = Mathf.Max(newInterfaceAlphaPercentage, 0.0f);
			}
		}
		
		// Update interface visibility
		foreach (var fadeableInterfaceCanvasItem in FadeableInterfaceCanvasItems)
		{
			Color color = fadeableInterfaceCanvasItem.Modulate;
			color.a = interfaceAlphaPercentage;
			fadeableInterfaceCanvasItem.Modulate = color;
		}
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with the main Pomo interface GUI Input signal.<br/>
	/// Make the interface appear.
	/// </summary>
	private void OnPomoTimerReceivedGuiInput(InputEvent inputEvent)
	{
		interfaceShouldFade = false;
		currentSecondsBeforeInterfaceFade = SecondsBeforeInterfaceFade;
		interfaceAlphaPercentage = 1.0f;
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with the main Pomo interface resized signal.<br/>
	/// Make the interface appear.
	/// </summary>
	private void OnPomoTimerResized()
	{
		interfaceShouldFade = false;
		currentSecondsBeforeInterfaceFade = SecondsBeforeInterfaceFade;
		interfaceAlphaPercentage = 1.0f;
	}

	/// <summary>
	/// End the current timer phase and enter the next one.
	/// </summary>
	private void GoToNextTimerPhase()
	{
		switch (currentPhase)
		{
			case Phase.Work:
				{
					workPhasesSinceLongBreak += 1;
					if (workPhasesSinceLongBreak >= userPreferences.LongBreakFrequency)
					{
						InitializeNewTimerPhase(Phase.LongBreak);
					}
					else
					{
						InitializeNewTimerPhase(Phase.ShortBreak);
					}
					break;
				}
			case Phase.ShortBreak:
				{
					InitializeNewTimerPhase(Phase.Work);
					break;
				}
			case Phase.LongBreak:
				{
					InitializeNewTimerPhase(Phase.Work);
					break;
				}
		}
	}

	/// <summary>
	/// Initialize the timer based on the input phase.
	/// </summary>
	/// <param name="newPhase">The phase to initialize the timer for</param>
	private void InitializeNewTimerPhase(Phase newPhase)
	{
		switch (newPhase)
		{
			case Phase.Work:
				{
					currentMinutesRemaining = userPreferences.WorkMinutes;

					AudioStreamPlayer.Stream = WorkPhaseStartSfx;
					AudioStreamPlayer.Play();
					break;
				}
			case Phase.ShortBreak:
				{
					currentMinutesRemaining = userPreferences.ShortBreakMinutes;

					AudioStreamPlayer.Stream = ShortBreakPhaseStartSfx;
					AudioStreamPlayer.Play();
					break;
				}
			case Phase.LongBreak:
				{
					currentMinutesRemaining = userPreferences.LongBreakMinutes;
					workPhasesSinceLongBreak = 0;

					AudioStreamPlayer.Stream = LongBreakPhaseStartSfx;
					AudioStreamPlayer.Play();
					break;
				}
		}
		currentPhaseTotalMinutes = currentMinutesRemaining;
		currentSecondsRemaining = 0;
		currentMillisecondsRemaining = MillisecondsInASecond;
		currentPhase = newPhase;
	}

	/// <summary>
	/// Reset the timer values, display the play button texture, and update the timer's text and relative rect sizes.
	/// </summary>
	private void ResetTimer()
	{
		ResetTimerValues();

		PauseButtonTextureRect.Texture = PlayTexture;

		UpdateTimerText();
		UpdateTimerRectSizes();
	}

	/// <summary>
	/// Reset the active timer's values to the initial state.
	/// </summary>
	private void ResetTimerValues()
	{
		timerActive = false;
		freshSessionAndShouldPlaySfxOnPlay = true;
		currentMinutesRemaining = userPreferences.WorkMinutes;
		currentPhaseTotalMinutes = currentMinutesRemaining;
		currentSecondsRemaining = 0;
		currentMillisecondsRemaining = MillisecondsInASecond;
		currentPhase = Phase.Work;
		workPhasesSinceLongBreak = 0;
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with the pause button.<br/>
	/// Toggle pause for the timer.
	/// </summary>
	private void OnPauseButtonPressed()
	{
		TogglePauseTimer();
	}

	/// <summary>
	/// Toggle the timer's pause state.
	/// </summary>
	void TogglePauseTimer()
	{
		if (freshSessionAndShouldPlaySfxOnPlay)
		{
			AudioStreamPlayer.Stream = WorkPhaseStartSfx;
			AudioStreamPlayer.Play();

			freshSessionAndShouldPlaySfxOnPlay = false;
		}

		if (timerActive)
		{
			PauseButtonTextureRect.Texture = PlayTexture;
			timerActive = false;
		}
		else
		{
			PauseButtonTextureRect.Texture = PauseTexture;
			timerActive = true;
		}
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with the reset button.<br/>
	/// Reset the timer.
	/// </summary>
	private void OnResetButtonPressed()
	{
		ResetTimer();
	}

	/// <summary>
	/// Validate the visibility of all options reset buttons, hiding them if their associated preferences are at their default values, and displaying them otherwise.
	/// </summary>
	private void ValidateResetButtons()
	{
		if (Mathf.Abs(userPreferences.SfxVolume - defaultUserPreferences.SfxVolume) < Mathf.Epsilon)
		{
			OptionsResetSfxVolumeButton.Visible = false;
			OptionsResetSfxVolumeButton.Disabled = true;
		}
		else
		{
			OptionsResetSfxVolumeButton.Visible = true;
			OptionsResetSfxVolumeButton.Disabled = false;
		}

		if (userPreferences.UpperTimerColor.ToRgba32() == defaultUserPreferences.UpperTimerColor.ToRgba32())
		{
			OptionsResetTimerUpperColorButton.Visible = false;
			OptionsResetTimerUpperColorButton.Disabled = true;
		}
		else
		{
			OptionsResetTimerUpperColorButton.Visible = true;
			OptionsResetTimerUpperColorButton.Disabled = false;
		}

		if (userPreferences.LowerTimerColor.ToRgba32() == defaultUserPreferences.LowerTimerColor.ToRgba32())
		{
			OptionsResetTimerLowerColorButton.Visible = false;
			OptionsResetTimerLowerColorButton.Disabled = true;
		}
		else
		{
			OptionsResetTimerLowerColorButton.Visible = true;
			OptionsResetTimerLowerColorButton.Disabled = false;
		}

		if (userPreferences.WorkMinutes == defaultUserPreferences.WorkMinutes)
		{
			OptionsResetWorkTimerDurationButton.Visible = false;
			OptionsResetWorkTimerDurationButton.Disabled = true;
		}
		else
		{
			OptionsResetWorkTimerDurationButton.Visible = true;
			OptionsResetWorkTimerDurationButton.Disabled = false;
		}

		if (userPreferences.ShortBreakMinutes == defaultUserPreferences.ShortBreakMinutes)
		{
			OptionsResetShortBreakTimerDurationButton.Visible = false;
			OptionsResetShortBreakTimerDurationButton.Disabled = true;
		}
		else
		{
			OptionsResetShortBreakTimerDurationButton.Visible = true;
			OptionsResetShortBreakTimerDurationButton.Disabled = false;
		}

		if (userPreferences.LongBreakMinutes == defaultUserPreferences.LongBreakMinutes)
		{
			OptionsResetLongBreakTimerDurationButton.Visible = false;
			OptionsResetLongBreakTimerDurationButton.Disabled = true;
		}
		else
		{
			OptionsResetLongBreakTimerDurationButton.Visible = true;
			OptionsResetLongBreakTimerDurationButton.Disabled = false;
		}

		if (userPreferences.LongBreakFrequency == defaultUserPreferences.LongBreakFrequency)
		{
			OptionsResetLongBreakFrequencyButton.Visible = false;
			OptionsResetLongBreakFrequencyButton.Disabled = true;
		}
		else
		{
			OptionsResetLongBreakFrequencyButton.Visible = true;
			OptionsResetLongBreakFrequencyButton.Disabled = false;
		}
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with the options button when it is pressed.<br/>
	/// Show the options popup.
	/// </summary>
	private void OnOptionsButtonPressed()
	{
		ShowOptionsPopup();
	}

	/// <summary>
	/// Show the options popup.
	/// </summary>
	private void ShowOptionsPopup()
	{
		ValidateResetButtons();
		OptionsPopup.ShowOptionsPopup();
	}

	/// <summary>
	/// Close the options popup.
	/// </summary>
	private void CloseOptionsPopup()
	{
		OptionsPopup.HideOptionsPopup();
	}

	/// <summary>
	/// Save the user preferences to file.
	/// </summary>
	private void SaveUserPreferencesToFile()
	{
		userPreferences.SaveToFile(UserPreferencesFileName);
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with the options menu background overlay button when it is pressed.<br/>
	/// Close the options popup.
	/// </summary>
	private void OnOptionsPopupOverlayButtonPressed()
	{
		CloseOptionsPopup();
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with options menu confirm button when it is pressed.<br/>
	/// Close the options popup.
	/// </summary>
	private void OnConfirmOptionsButtonPressed()
	{
		CloseOptionsPopup();
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with the SFX Volume Slider when its value changes.<br/>
	/// Set the SFX volume.
	/// </summary>
	/// <param name="newVolume">The new SFX volume</param>
	private void OnSfxVolumeSliderValueChanged(float newVolume)
	{
		SetSfxVolume(newVolume);
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with the SFX Volume Slider when a drag has ended.<br/>
	/// Play a sound to demonstrate the volume set.
	/// </summary>
	/// <param name="valueChanged"></param>
	private void OnSfxVolumeSliderDragEnded(bool valueChanged)
	{
		// Play audio cue as demo
		AudioStreamPlayer.Stream = ShortBreakPhaseStartSfx;
		AudioStreamPlayer.Play();
	}

	/// <summary>
	/// Set the SFX volume.
	/// </summary>
	/// <param name="newVolume">The new SFX volume</param>
	private void SetSfxVolume(float newVolume)
	{
		userPreferences.SfxVolume = newVolume;
		AudioStreamPlayer.VolumeDb = GD.Linear2Db(userPreferences.SfxVolume / 100.0f);
		SaveUserPreferencesToFile();
		ValidateResetButtons();
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with the reset volume button when it is pressed.<br/>
	/// Reset the SFX volume.
	/// </summary>
	private void OnResetVolumeButtonPressed()
	{
		float defaultSfxVolume = defaultUserPreferences.SfxVolume;
		OptionsSfxVolumeSlider.Value = defaultSfxVolume;
		SetSfxVolume(defaultSfxVolume);
		
		// Play audio cue as demo
		AudioStreamPlayer.Stream = ShortBreakPhaseStartSfx;
		AudioStreamPlayer.Play();

		ValidateResetButtons();
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with the upper timer color picker button when its color has been changed.<br/>
	/// Set the upper timer rect color then validate reset buttons.
	/// </summary>
	private void OnUpperTimerColorPickerButtonColorChanged(Color newColor)
	{
		SetUpperTimerColor(newColor);

		ValidateResetButtons();
	}

	/// <summary>
	/// Set the color of the upper timer rect, update, and save preference to file.
	/// </summary>
	/// <param name="newColor">The color to apply to the upper timer rect</param>
	private void SetUpperTimerColor(Color newColor)
	{
		userPreferences.UpperTimerColor = newColor;
		UpdateTimerRectColors();
		UpdateColorPickerTextureRects();
		SaveUserPreferencesToFile();
		ValidateResetButtons();
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with the reset upper timer color button when it is pressed.<br/>
	/// Reset the upper timer color.
	/// </summary>
	private void OnResetUpperColorButtonPressed()
	{
		SetUpperTimerColor(defaultUserPreferences.UpperTimerColor);
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with the lower timer color picker button when its color has been changed.<br/>
	/// Set the upper timer rect color then validate reset buttons.
	/// </summary>
	private void OnLowerTimerColorPickerButtonColorChanged(Color newColor)
	{
		SetLowerTimerColor(newColor);
	}

	/// <summary>
	/// Set the color of the lower timer rect, update, and save preference to file.
	/// </summary>
	/// <param name="newColor">The color to apply to the lower timer rect</param>
	private void SetLowerTimerColor(Color newColor)
	{
		userPreferences.LowerTimerColor = newColor;
		UpdateTimerRectColors();
		UpdateColorPickerTextureRects();
		SaveUserPreferencesToFile();
		ValidateResetButtons();
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with the reset lower timer color button when it is pressed.<br/>
	/// Reset the lower timer color.
	/// </summary>
	private void OnResetLowerColorButtonPressed()
	{
		SetLowerTimerColor(defaultUserPreferences.LowerTimerColor);
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with the work timer duration LineEdit when text is entered.<br/>
	/// Validates and sets the work timer duration.
	/// </summary>
	/// <param name="newText">The input text</param>
	private void OnWorkTimerDurationLineEditTextEntered(string newText)
	{
		SubmitTimerDuration(OptionsWorkTimerDurationLineEdit, newText, Phase.Work);
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with the work timer duration LineEdit when it is focused.<br/>
	/// Moves the caret position to the end of the text.
	/// </summary>
	private void OnWorkTimerDurationLineEditFocusEntered()
	{
		OptionsWorkTimerDurationLineEdit.CaretPosition = OptionsWorkTimerDurationLineEdit.Text.Length;
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with the work timer duration LineEdit when focus is lost.<br/>
	/// Validates and sets the work timer duration.
	/// </summary>
	private void OnWorkTimerDurationLineEditFocusExited()
	{
		SubmitTimerDuration(OptionsWorkTimerDurationLineEdit, OptionsWorkTimerDurationLineEdit.Text, Phase.Work);
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with the reset work timer duration button when it is pressed.<br/>
	/// Resets the work timer duration to default values.
	/// </summary>
	private void OnResetWorkTimerDurationButtonPressed()
	{
		SubmitTimerDuration(OptionsWorkTimerDurationLineEdit, defaultUserPreferences.WorkMinutes.ToString(), Phase.Work);
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with the short break timer duration LineEdit when text is entered.<br/>
	/// Validates and sets the short break timer duration.
	/// </summary>
	/// <param name="newText">The input text</param>
	private void OnShortBreakTimerDurationLineEditTextEntered(string newText)
	{
		SubmitTimerDuration(OptionsShortBreakTimerDurationLineEdit, newText, Phase.ShortBreak);
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with the short break timer duration LineEdit when it is focused.<br/>
	/// Validates and sets the short break timer duration.
	/// </summary>
	private void OnShortBreakTimerDurationLineEditFocusEntered()
	{
		OptionsShortBreakTimerDurationLineEdit.CaretPosition = OptionsShortBreakTimerDurationLineEdit.Text.Length;
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with the short break timer duration LineEdit when focus is lost.<br/>
	/// Validates and sets the short break timer duration.
	/// </summary>
	private void OnShortBreakTimerDurationLineEditFocusExited()
	{
		SubmitTimerDuration(OptionsShortBreakTimerDurationLineEdit, OptionsShortBreakTimerDurationLineEdit.Text, Phase.ShortBreak);
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with the reset short break timer duration button when it is pressed.<br/>
	/// Resets the short break timer duration to default values.
	/// </summary>
	private void OnResetShortBreakTimerDurationButtonPressed()
	{
		SubmitTimerDuration(OptionsShortBreakTimerDurationLineEdit, defaultUserPreferences.ShortBreakMinutes.ToString(), Phase.ShortBreak);
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with the long break timer duration LineEdit when text is entered.<br/>
	/// Validates and sets the long break timer duration.
	/// </summary>
	/// <param name="newText">The input text</param>
	private void OnLongBreakTimerDurationLineEditTextEntered(string newText)
	{
		SubmitTimerDuration(OptionsLongBreakTimerDurationLineEdit, newText, Phase.LongBreak);
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with the long break timer duration LineEdit when it is focused.<br/>
	/// Validates and sets the long break timer duration.
	/// </summary>
	private void OnLongBreakTimerDurationLineEditFocusEntered()
	{
		OptionsLongBreakTimerDurationLineEdit.CaretPosition = OptionsLongBreakTimerDurationLineEdit.Text.Length;
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with the long break timer duration LineEdit when focus is lost.<br/>
	/// Validates and sets the long break timer duration.
	/// </summary>
	private void OnLongBreakTimerDurationLineEditFocusExited()
	{
		SubmitTimerDuration(OptionsLongBreakTimerDurationLineEdit, OptionsLongBreakTimerDurationLineEdit.Text, Phase.LongBreak);
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with the reset long break timer duration button when it is pressed.<br/>
	/// Resets the long break timer duration to default values.
	/// </summary>
	private void OnResetLongBreakTimerDurationButtonPressed()
	{
		SubmitTimerDuration(OptionsLongBreakTimerDurationLineEdit, defaultUserPreferences.LongBreakMinutes.ToString(), Phase.LongBreak);
	}

	/// <summary>
	/// Validate the submitted text, and set it as the timer duration for the specified phase if it is valid.
	/// </summary>
	/// <param name="OptionsTimerDurationLineEdit">The LineEdit for the submitted timer duration</param>
	/// <param name="newText">The text input for the timer duration</param>
	/// <param name="phaseType">The timer phase to apply the timer duration to</param>
	private void SubmitTimerDuration(LineEdit OptionsTimerDurationLineEdit, string newText, Phase phaseType)
	{
		int newMinutesAsInteger;
		bool newTextIsNumeric = false;

		// Try to parse the input, rounding to an integer if it's a float.
		if (float.TryParse(newText, out float newMinutesAsFloat))
		{
			newMinutesAsInteger = Mathf.RoundToInt(newMinutesAsFloat);
			newTextIsNumeric = true;
		}
		else if (int.TryParse(newText, out newMinutesAsInteger))
		{
			newTextIsNumeric = true;
		}

		// If input is valid, clamp and apply it.
		if (newTextIsNumeric)
		{
			newMinutesAsInteger = Mathf.Clamp(newMinutesAsInteger, AbsoluteMinimumMinutesPerPhase, AbsoluteMaximumMinutesPerPhase);

			switch (phaseType)
			{
				case Phase.Work:
					{
						userPreferences.WorkMinutes = newMinutesAsInteger;
						break;
					}
				case Phase.ShortBreak:
					{
						userPreferences.ShortBreakMinutes = newMinutesAsInteger;
						break;
					}
				case Phase.LongBreak:
					{
						userPreferences.LongBreakMinutes = newMinutesAsInteger;
						break;
					}
				default:
					break;
			}
		}

		// Apply the value to the text, regardless of whether it was successful.
		switch (phaseType)
		{
			case Phase.Work:
				{
					OptionsTimerDurationLineEdit.Text = userPreferences.WorkMinutes.ToString();
					break;
				}
			case Phase.ShortBreak:
				{
					OptionsTimerDurationLineEdit.Text = userPreferences.ShortBreakMinutes.ToString();
					break;
				}
			case Phase.LongBreak:
				{
					OptionsTimerDurationLineEdit.Text = userPreferences.LongBreakMinutes.ToString();
					break;
				}
			default:
				break;
		}

		SaveUserPreferencesToFile();

		ValidateResetButtons();

		// If in a fresh session, reset to apply the new values.
		if (freshSessionAndShouldPlaySfxOnPlay)
		{
			ResetTimer();
		}

		OptionsTimerDurationLineEdit.ReleaseFocus();
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with the long break frequency LineEdit when it is focused.<br/>
	/// Validates and sets the long break frequency.
	/// </summary>
	/// <param name="newText">The input text to be parsed</param>
	private void OnLongBreakFrequencyLineEditTextEntered(string newText)
	{
		SubmitLongBreakFrequency(newText);
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with the long break frequency LineEdit when it is focused.<br/>
	/// Validates and sets the long break frequency.
	/// </summary>
	private void OnLongBreakFrequencyLineEditFocusEntered()
	{
		OptionsLongBreakFrequencyLineEdit.CaretPosition = OptionsLongBreakFrequencyLineEdit.Text.Length;
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with the long break frequency LineEdit when focus is lost.<br/>
	/// Validates and sets the long break frequency.
	/// </summary>
	private void OnLongBreakFrequencyLineEditFocusExited()
	{
		SubmitLongBreakFrequency(OptionsLongBreakFrequencyLineEdit.Text);
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with the reset short break frequency button when it is pressed.<br/>
	/// Resets the short break frequency to default values.
	/// </summary>
	private void OnResetLongBreakFrequencyButtonPressed()
	{
		SubmitLongBreakFrequency(defaultUserPreferences.LongBreakFrequency.ToString());
	}

	/// <summary>
	/// Validate the submitted text, and set it as the long break frequency if it is valid.
	/// </summary>
	/// <param name="newText">The text input for the long break frequency</param>
	private void SubmitLongBreakFrequency(string newText)
	{
		int newFrequencyAsInteger;
		bool newTextIsNumeric = false;

		// Try to parse the input, rounding to an integer if it's a float.
		if (float.TryParse(newText, out float newFrequencyAsFloat))
		{
			newFrequencyAsInteger = Mathf.RoundToInt(newFrequencyAsFloat);
			newTextIsNumeric = true;
		}
		else if (int.TryParse(newText, out newFrequencyAsInteger))
		{
			newTextIsNumeric = true;
		}

		// If input is valid, clamp and apply it.
		if (newTextIsNumeric)
		{
			newFrequencyAsInteger = Mathf.Clamp(newFrequencyAsInteger, AbsoluteMinimumWorkPhasesPerLongBreak, AbsoluteMaximumWorkPhasesPerLongBreak);

			userPreferences.LongBreakFrequency = newFrequencyAsInteger;
		}

		// Apply the value to the text, regardless of whether it was successful.
		OptionsLongBreakFrequencyLineEdit.Text = userPreferences.LongBreakFrequency.ToString();

		SaveUserPreferencesToFile();

		ValidateResetButtons();

		// If in a fresh session, reset to apply the new values.
		if (freshSessionAndShouldPlaySfxOnPlay)
		{
			ResetTimer();
		}

		OptionsLongBreakFrequencyLineEdit.ReleaseFocus();
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with the pin window button when it is pressed.<br/>
	/// Toggle the pin status of the app window.
	/// </summary>
	private void OnPinWindowButtonPressed()
	{
		TogglePinWindow();
	}

	/// <summary>
	/// Toggle the pin status (whether it is always shown over other windows) of the app window.
	/// </summary>
	private void TogglePinWindow()
	{
		if (OS.IsWindowAlwaysOnTop())
		{
			PinWindowButtonTextureRect.Texture = PinWindowTexture;
			OS.SetWindowAlwaysOnTop(false);
		}
		else
		{
			PinWindowButtonTextureRect.Texture = UnpinWindowTexture;
			OS.SetWindowAlwaysOnTop(true);
		}
	}

	/// <summary>
	/// Get whether the current app platform allows for window pinning.
	/// </summary>
	/// <returns>True if the app platform allows for window pinning.</returns>
	private bool PlatformAllowsWindowPinning()
	{
		if (OS.GetName() == "Windows" ||
			OS.GetName() == "OSX" ||
			OS.GetName() == "X11")
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	/// <summary>
	/// Signal Receiver Method.<br/>
	/// For use with the resize signal of the main timer control.<br/>
	/// Saves the current window size to user preferences.
	/// </summary>
	private void OnViewportSizeChanged()
	{
		if (userPreferences != null)
		{
			userPreferences.WindowSize = OS.WindowSize;
			SaveUserPreferencesToFile();
		}
	}
}
