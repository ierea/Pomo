using Godot;
using System;

public class PomoTimer : Control
{
    enum Phase
    {
        Work,
        ShortBreak,
        LongBreak
    }

    [Export] private NodePath AudioStreamPlayerNodePath;
    [Export] private NodePath TimeLabelNodePath;
    [Export] private NodePath PauseButtonNodePath;
    [Export] private NodePath PauseButtonTextureRectNodePath;
    [Export] private NodePath ResetButtonNodePath;
    [Export] private NodePath UpperTimerTextureRectNodePath;
    [Export] private NodePath LowerTimerTextureRectNodePath;

    [Export] private NodePath PinWindowButtonTextureRectNodePath;

    [Export] private NodePath OptionsPopupNodePath;

    [Export] private NodePath OptionsSfxVolumeSliderNodePath;

    [Export] private NodePath OptionsUpperTimerColorPickerButtonNodePath;
    [Export] private NodePath OptionsLowerTimerColorPickerButtonNodePath;
    [Export] private NodePath OptionsUpperTimerColorPickerTextureRectNodePath;
    [Export] private NodePath OptionsLowerTimerColorPickerTextureRectNodePath;

    [Export] private NodePath OptionsWorkTimerDurationLineEditNodePath;
    [Export] private NodePath OptionsShortBreakTimerDurationLineEditNodePath;
    [Export] private NodePath OptionsLongBreakTimerDurationLineEditNodePath;
    [Export] private NodePath OptionsLongBreakFrequencyLineEditNodePath;

    [Export] private Texture PauseTexture;
    [Export] private Texture PlayTexture;
    [Export] private Texture PinWindowTexture;
    [Export] private Texture UnpinWindowTexture;

    [Export] private AudioStream WorkPhaseStartSfx;
    [Export] private AudioStream ShortBreakPhaseStartSfx;
    [Export] private AudioStream LongBreakPhaseStartSfx;

    [Export] private float MinSfxVolumeDb;
    [Export] private float MaxSfxVolumeDb;
    [Export] private float DefaultSfxVolume;

    [Export] private Color upperTimerColor;
    [Export] private Color lowerTimerColor;

    private const int SpeedMultiplier = 1;
    private const int MillisecondsInASecond = 1000;
    private const int SecondsInAMinute = 60;

    private const int AbsoluteMinimumMinutesPerPhase = 1;
    private const int AbsoluteMaximumMinutesPerPhase = 9999;

    private const int AbsoluteMinimumWorkPhasesPerLongBreak = 1;
    private const int AbsoluteMaximumWorkPhasesPerLongBreak = 9999;


    private AudioStreamPlayer AudioStreamPlayer;
    private Label TimeLabel;
    private Button PauseButton;
    private TextureRect PauseButtonTextureRect;
    private Button ResetButton;
    private TextureRect UpperTimerTextureRect;
    private TextureRect LowerTimerTextureRect;
    private TextureRect PinWindowButtonTextureRect;
    private OptionsPopup OptionsPopup;
    private HSlider OptionsSfxVolumeSlider;
    private ColorPickerButton OptionsTimerUpperColorPickerButton;
    private ColorPickerButton OptionsTimerLowerColorPickerButton;
    private TextureRect OptionsTimerUpperColorPickerTextureRect;
    private TextureRect OptionsTimerLowerColorPickerTextureRect;
    private LineEdit OptionsWorkTimerDurationLineEdit;
    private LineEdit OptionsShortBreakTimerDurationLineEdit;
    private LineEdit OptionsLongBreakTimerDurationLineEdit;
    private LineEdit OptionsLongBreakFrequencyLineEdit;

    private float currentSfxVolume;

    private int workPhasesPerLongBreak = 5;
    private int workMinutes = 25;
    private int shortBreakMinutes = 5;
    private int longBreakMinutes = 30;

    private Phase currentPhase;
    private int currentMinutesRemaining;
    private int currentSecondsRemaining;
    private float currentMillisecondsRemaining;
    private int currentPhaseTotalMinutes;
    private bool timerActive;
    private bool freshSessionAndShouldPlaySfxOnPlay;
    private int workPhasesSinceLongBreak;


    public PomoTimer()
    {
        ResetTimerValues();
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        AudioStreamPlayer = GetNode<AudioStreamPlayer>(AudioStreamPlayerNodePath);
        SetSfxVolume(DefaultSfxVolume);

        TimeLabel = GetNode<Label>(TimeLabelNodePath);
        PauseButton = GetNode<Button>(PauseButtonNodePath);
        PauseButtonTextureRect = GetNode<TextureRect>(PauseButtonTextureRectNodePath);
        ResetButton = GetNode<Button>(ResetButtonNodePath);

        UpperTimerTextureRect = GetNode<TextureRect>(UpperTimerTextureRectNodePath);
        LowerTimerTextureRect = GetNode<TextureRect>(LowerTimerTextureRectNodePath);

        PinWindowButtonTextureRect = GetNode<TextureRect>(PinWindowButtonTextureRectNodePath);

        OptionsPopup = GetNode<OptionsPopup>(OptionsPopupNodePath);

        OptionsSfxVolumeSlider = GetNode<HSlider>(OptionsSfxVolumeSliderNodePath);
        OptionsSfxVolumeSlider.Value = DefaultSfxVolume;

        OptionsTimerUpperColorPickerButton = GetNode<ColorPickerButton>(OptionsUpperTimerColorPickerButtonNodePath);
        OptionsTimerLowerColorPickerButton = GetNode<ColorPickerButton>(OptionsLowerTimerColorPickerButtonNodePath);
        OptionsTimerUpperColorPickerTextureRect = GetNode<TextureRect>(OptionsUpperTimerColorPickerTextureRectNodePath);
        OptionsTimerLowerColorPickerTextureRect = GetNode<TextureRect>(OptionsLowerTimerColorPickerTextureRectNodePath);

        OptionsWorkTimerDurationLineEdit = GetNode<LineEdit>(OptionsWorkTimerDurationLineEditNodePath);
        OptionsShortBreakTimerDurationLineEdit = GetNode<LineEdit>(OptionsShortBreakTimerDurationLineEditNodePath);
        OptionsLongBreakTimerDurationLineEdit = GetNode<LineEdit>(OptionsLongBreakTimerDurationLineEditNodePath);
        OptionsLongBreakFrequencyLineEdit = GetNode<LineEdit>(OptionsLongBreakFrequencyLineEditNodePath);

        UpdateTimerText();
        UpdateTimerRectSizes();
        UpdateTimerRectColors();
        UpdateOptions();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
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

    }

    private void UpdateTimerText()
    {
        TimeLabel.Text = currentMinutesRemaining + ":" + currentSecondsRemaining.ToString("0#");
    }

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

    private void UpdateTimerRectColors()
    {
        UpperTimerTextureRect.Modulate = upperTimerColor;
        LowerTimerTextureRect.Modulate = lowerTimerColor;
    }

    private void UpdateOptions()
    {
        OptionsTimerUpperColorPickerButton.Color = upperTimerColor;
        OptionsTimerLowerColorPickerButton.Color = lowerTimerColor;

        OptionsTimerUpperColorPickerTextureRect.Modulate = upperTimerColor;
        OptionsTimerLowerColorPickerTextureRect.Modulate = lowerTimerColor;
    }

    private void GoToNextTimerPhase()
    {
        switch (currentPhase)
        {
            case Phase.Work:
            {
                workPhasesSinceLongBreak += 1;
                if (workPhasesSinceLongBreak >= workPhasesPerLongBreak)
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

    private void InitializeNewTimerPhase(Phase newPhase)
    {
        switch (newPhase)
        {
            case Phase.Work:
            {
                currentMinutesRemaining = workMinutes;

                AudioStreamPlayer.Stream = WorkPhaseStartSfx;
                AudioStreamPlayer.Play();
                break;
            }
            case Phase.ShortBreak:
            {
                currentMinutesRemaining = shortBreakMinutes;

                AudioStreamPlayer.Stream = ShortBreakPhaseStartSfx;
                AudioStreamPlayer.Play();
                break;
            }
            case Phase.LongBreak:
            {
                currentMinutesRemaining = longBreakMinutes;
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

    private void ResetTimer()
    {
        ResetTimerValues();

        PauseButtonTextureRect.Texture = PlayTexture;

        UpdateTimerText();
        UpdateTimerRectSizes();
    }

    private void ResetTimerValues()
    {
        timerActive = false;
        freshSessionAndShouldPlaySfxOnPlay = true;
        currentMinutesRemaining = workMinutes;
        currentPhaseTotalMinutes = currentMinutesRemaining;
        currentSecondsRemaining = 0;
        currentMillisecondsRemaining = MillisecondsInASecond;
        currentPhase = Phase.Work;
        workPhasesSinceLongBreak = 0;
    }

    private void CloseOptionsPopup()
    {
        OptionsPopup.HideOptionsPopup();
    }

    private void OnPauseButtonPressed()
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

    private void OnResetButtonPressed()
    {
        ResetTimer();
    }

    private void OnOptionsButtonPressed()
    {
        OptionsPopup.ShowOptionsPopup();
    }

    private void OnOptionsPopupOverlayButtonPressed()
    {
        CloseOptionsPopup();
    }

    private void OnConfirmOptionsButtonPressed()
    {
        CloseOptionsPopup();
    }

    private void OnSfxVolumeSliderValueChanged(float newVolume)
    {
        SetSfxVolume(newVolume);
    }

    private void SetSfxVolume(float newVolume)
    {
        currentSfxVolume = Mathf.Lerp(MinSfxVolumeDb, MaxSfxVolumeDb, newVolume / 100.0f);
        AudioStreamPlayer.VolumeDb = currentSfxVolume;
    }

    private void OnUpperTimerColorPickerButtonColorChanged(Color newColor)
    {
        upperTimerColor = newColor;
        UpdateTimerRectColors();
        UpdateOptions();
    }

    private void OnLowerTimerColorPickerButtonColorChanged(Color newColor)
    {
        lowerTimerColor = newColor;
        UpdateTimerRectColors();
        UpdateOptions();
    }

    private void OnWorkTimerDurationLineEditTextEntered(string newText)
    {
        SubmitTimerDuration(OptionsWorkTimerDurationLineEdit, newText, Phase.Work);
    }

    private void OnWorkTimerDurationLineEditFocusExited()
    {
        SubmitTimerDuration(OptionsWorkTimerDurationLineEdit, OptionsWorkTimerDurationLineEdit.Text, Phase.Work);
    }

    private void OnShortBreakTimerDurationLineEditTextEntered(string newText)
    {
        SubmitTimerDuration(OptionsShortBreakTimerDurationLineEdit, newText, Phase.ShortBreak);
    }

    private void OnShortBreakTimerDurationLineEditFocusExited()
    {
        SubmitTimerDuration(OptionsShortBreakTimerDurationLineEdit, OptionsShortBreakTimerDurationLineEdit.Text, Phase.ShortBreak);
    }

    private void OnLongBreakTimerDurationLineEditTextEntered(string newText)
    {
        SubmitTimerDuration(OptionsLongBreakTimerDurationLineEdit, newText, Phase.LongBreak);
    }

    private void OnLongBreakTimerDurationLineEditFocusExited()
    {
        SubmitTimerDuration(OptionsLongBreakTimerDurationLineEdit, OptionsLongBreakTimerDurationLineEdit.Text, Phase.LongBreak);
    }

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
                    workMinutes = newMinutesAsInteger;
                    break;
                }
                case Phase.ShortBreak:
                {
                    shortBreakMinutes = newMinutesAsInteger;
                    break;
                }
                case Phase.LongBreak:
                {
                    longBreakMinutes = newMinutesAsInteger;
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
                OptionsTimerDurationLineEdit.Text = workMinutes.ToString();
                break;
            }
            case Phase.ShortBreak:
            {
                OptionsTimerDurationLineEdit.Text = shortBreakMinutes.ToString();
                break;
            }
            case Phase.LongBreak:
            {
                OptionsTimerDurationLineEdit.Text = longBreakMinutes.ToString();
                break;
            }
            default:
                break;
        }
        
        // If in a fresh session, reset to apply the new values.
        if (freshSessionAndShouldPlaySfxOnPlay)
        {
            ResetTimer();
        }
        
        OptionsTimerDurationLineEdit.ReleaseFocus();
    }

    private void OnLongBreakFrequencyLineEditTextEntered(string newText)
    {
        SubmitLongBreakFrequency(newText);
    }

    private void OnLongBreakFrequencyLineEditFocusExited()
    {
        SubmitLongBreakFrequency(OptionsLongBreakFrequencyLineEdit.Text);
    }

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

            workPhasesPerLongBreak = newFrequencyAsInteger;
        }

        // Apply the value to the text, regardless of whether it was successful.
        OptionsLongBreakFrequencyLineEdit.Text = workPhasesPerLongBreak.ToString();
        
        // If in a fresh session, reset to apply the new values.
        if (freshSessionAndShouldPlaySfxOnPlay)
        {
            ResetTimer();
        }
        
        OptionsLongBreakFrequencyLineEdit.ReleaseFocus();
    }

    private void OnPinWindowButtonPressed()
    {
        bool isCurrentlyAlwaysOnTop = OS.IsWindowAlwaysOnTop();

        if (isCurrentlyAlwaysOnTop)
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
}
