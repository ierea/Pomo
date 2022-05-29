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

    [Export] private Texture PauseTexture;
    [Export] private Texture PlayTexture;
    [Export] private Texture PinWindowTexture;
    [Export] private Texture UnpinWindowTexture;

    [Export] private AudioStream WorkPhaseStartSfx;
    [Export] private AudioStream ShortBreakPhaseStartSfx;
    [Export] private AudioStream LongBreakPhaseStartSfx;

    [Export] private string UserPreferencesFileName;

    [Export] private string PauseTimerActionName;
    [Export] private string PlayTimerActionName;
    [Export] private string ResetTimerActionName;
    [Export] private string OpenOptionsActionName;
    [Export] private string CloseOptionsActionName;
    [Export] private string PinWindowActionName;
    [Export] private string UnpinWindowActionName;

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

    private UserPreferences userPreferences;
    private UserPreferences defaultUserPreferences;

    private Phase currentPhase;
    private int currentMinutesRemaining;
    private int currentSecondsRemaining;
    private float currentMillisecondsRemaining;
    private int currentPhaseTotalMinutes;
    private bool timerActive;
    private bool freshSessionAndShouldPlaySfxOnPlay;
    private int workPhasesSinceLongBreak;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        defaultUserPreferences = new UserPreferences();
        userPreferences = UserPreferences.CreateFromFile(UserPreferencesFileName);
        ResetTimerValues();

        TimeLabel = GetNode<Label>(TimeLabelNodePath);
        PauseButton = GetNode<Button>(PauseButtonNodePath);
        PauseButtonTextureRect = GetNode<TextureRect>(PauseButtonTextureRectNodePath);
        ResetButton = GetNode<Button>(ResetButtonNodePath);

        UpperTimerTextureRect = GetNode<TextureRect>(UpperTimerTextureRectNodePath);
        LowerTimerTextureRect = GetNode<TextureRect>(LowerTimerTextureRectNodePath);

        PinWindowButton = GetNode<Button>(PinWindowButtonNodePath);
        if (OS.GetName() == "HTML5")
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

        if (!OptionsPopup.Visible)
        {
            if (Input.IsActionJustPressed(PauseTimerActionName) || Input.IsActionJustPressed(PlayTimerActionName))
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

            if (Input.IsActionJustPressed(PinWindowActionName) || Input.IsActionJustPressed(UnpinWindowActionName))
            {
                TogglePinWindow();
            }
        }
        else
        {
            if (Input.IsActionJustPressed(CloseOptionsActionName))
            {
                CloseOptionsPopup();
            }
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
        UpperTimerTextureRect.Modulate = userPreferences.UpperTimerColor;
        LowerTimerTextureRect.Modulate = userPreferences.LowerTimerColor;
    }

    private void UpdateColorPickerTextureRects()
    {
        OptionsTimerUpperColorPickerButton.Color = userPreferences.UpperTimerColor;
        OptionsTimerLowerColorPickerButton.Color = userPreferences.LowerTimerColor;

        OptionsTimerUpperColorPickerTextureRect.Modulate = userPreferences.UpperTimerColor;
        OptionsTimerLowerColorPickerTextureRect.Modulate = userPreferences.LowerTimerColor;
    }

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
        currentMinutesRemaining = userPreferences.WorkMinutes;
        currentPhaseTotalMinutes = currentMinutesRemaining;
        currentSecondsRemaining = 0;
        currentMillisecondsRemaining = MillisecondsInASecond;
        currentPhase = Phase.Work;
        workPhasesSinceLongBreak = 0;
    }

    private void OnPauseButtonPressed()
    {
        TogglePauseTimer();
    }

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

    private void OnResetButtonPressed()
    {
        ResetTimer();
    }

    // Hide reset buttons if values are default
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

    private void OnOptionsButtonPressed()
    {
        ShowOptionsPopup();
    }

    private void ShowOptionsPopup()
    {
        ValidateResetButtons();
        OptionsPopup.ShowOptionsPopup();
    }

    private void CloseOptionsPopup()
    {
        OptionsPopup.HideOptionsPopup();
    }

    private void SaveUserPreferencesToFile()
    {
        userPreferences.SaveToFile(UserPreferencesFileName);
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
        userPreferences.SfxVolume = newVolume;
        AudioStreamPlayer.VolumeDb = GD.Linear2Db(userPreferences.SfxVolume / 100.0f);
        SaveUserPreferencesToFile();
        ValidateResetButtons();
    }

    private void OnResetVolumeButtonPressed()
    {
        float defaultSfxVolume = defaultUserPreferences.SfxVolume;
        OptionsSfxVolumeSlider.Value = defaultSfxVolume;
        SetSfxVolume(defaultSfxVolume);

        ValidateResetButtons();
    }

    private void OnUpperTimerColorPickerButtonColorChanged(Color newColor)
    {
        SetUpperTimerColor(newColor);

        ValidateResetButtons();
    }

    private void SetUpperTimerColor(Color newColor)
    {
        userPreferences.UpperTimerColor = newColor;
        UpdateTimerRectColors();
        UpdateColorPickerTextureRects();
        SaveUserPreferencesToFile();
        ValidateResetButtons();
    }

    private void OnResetUpperColorButtonPressed()
    {
        SetUpperTimerColor(defaultUserPreferences.UpperTimerColor);
    }

    private void OnLowerTimerColorPickerButtonColorChanged(Color newColor)
    {
        SetLowerTimerColor(newColor);
    }

    private void SetLowerTimerColor(Color newColor)
    {
        userPreferences.LowerTimerColor = newColor;
        UpdateTimerRectColors();
        UpdateColorPickerTextureRects();
        SaveUserPreferencesToFile();
        ValidateResetButtons();
    }

    private void OnResetLowerColorButtonPressed()
    {
        SetLowerTimerColor(defaultUserPreferences.LowerTimerColor);
    }

    private void OnWorkTimerDurationLineEditTextEntered(string newText)
    {
        SubmitTimerDuration(OptionsWorkTimerDurationLineEdit, newText, Phase.Work);
    }

    private void OnWorkTimerDurationLineEditFocusEntered()
    {
        OptionsWorkTimerDurationLineEdit.CaretPosition = OptionsWorkTimerDurationLineEdit.Text.Length;
    }

    private void OnWorkTimerDurationLineEditFocusExited()
    {
        SubmitTimerDuration(OptionsWorkTimerDurationLineEdit, OptionsWorkTimerDurationLineEdit.Text, Phase.Work);
    }

    private void OnResetWorkTimerDurationButtonPressed()
    {
        SubmitTimerDuration(OptionsWorkTimerDurationLineEdit, defaultUserPreferences.WorkMinutes.ToString(), Phase.Work);
    }

    private void OnShortBreakTimerDurationLineEditTextEntered(string newText)
    {
        SubmitTimerDuration(OptionsShortBreakTimerDurationLineEdit, newText, Phase.ShortBreak);
    }

    private void OnShortBreakTimerDurationLineEditFocusEntered()
    {
        OptionsShortBreakTimerDurationLineEdit.CaretPosition = OptionsShortBreakTimerDurationLineEdit.Text.Length;
    }

    private void OnShortBreakTimerDurationLineEditFocusExited()
    {
        SubmitTimerDuration(OptionsShortBreakTimerDurationLineEdit, OptionsShortBreakTimerDurationLineEdit.Text, Phase.ShortBreak);
    }

    private void OnResetShortBreakTimerDurationButtonPressed()
    {
        SubmitTimerDuration(OptionsShortBreakTimerDurationLineEdit, defaultUserPreferences.ShortBreakMinutes.ToString(), Phase.ShortBreak);
    }

    private void OnLongBreakTimerDurationLineEditTextEntered(string newText)
    {
        SubmitTimerDuration(OptionsLongBreakTimerDurationLineEdit, newText, Phase.LongBreak);
    }

    private void OnLongBreakTimerDurationLineEditFocusEntered()
    {
        OptionsLongBreakTimerDurationLineEdit.CaretPosition = OptionsLongBreakTimerDurationLineEdit.Text.Length;
    }

    private void OnLongBreakTimerDurationLineEditFocusExited()
    {
        SubmitTimerDuration(OptionsLongBreakTimerDurationLineEdit, OptionsLongBreakTimerDurationLineEdit.Text, Phase.LongBreak);
    }

    private void OnResetLongBreakTimerDurationButtonPressed()
    {
        SubmitTimerDuration(OptionsLongBreakTimerDurationLineEdit, defaultUserPreferences.LongBreakMinutes.ToString(), Phase.LongBreak);
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

    private void OnLongBreakFrequencyLineEditTextEntered(string newText)
    {
        SubmitLongBreakFrequency(newText);
    }

    private void OnLongBreakFrequencyLineEditFocusEntered()
    {
        OptionsLongBreakFrequencyLineEdit.CaretPosition = OptionsLongBreakFrequencyLineEdit.Text.Length;
    }

    private void OnLongBreakFrequencyLineEditFocusExited()
    {
        SubmitLongBreakFrequency(OptionsLongBreakFrequencyLineEdit.Text);
    }

    private void OnResetLongBreakFrequencyButtonPressed()
    {
        SubmitLongBreakFrequency(defaultUserPreferences.LongBreakFrequency.ToString());
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

    private void OnPinWindowButtonPressed()
    {
        TogglePinWindow();
    }

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
}
