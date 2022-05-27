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

    [Export] private NodePath TimeLabelNodePath;
    [Export] private NodePath PauseButtonNodePath;
    [Export] private NodePath PauseButtonTextureRectNodePath;
    [Export] private NodePath ResetButtonNodePath;
    [Export] private NodePath UpperTimerTextureRectNodePath;
    [Export] private NodePath LowerTimerTextureRectNodePath;

    [Export] private Texture PauseTexture;
    [Export] private Texture PlayTexture;

    private const int SpeedMultiplier = 1;
    private const int MillisecondsInASecond = 1000;
    private const int SecondsInAMinute = 60;

    private Label TimeLabel;
    private Button PauseButton;
    private TextureRect PauseButtonTextureRect;
    private Button ResetButton;
    private TextureRect UpperTimerTextureRect;
    private TextureRect LowerTimerTextureRect;

    private int workPhasesPerLongBreak = 5;
    private int workMinutes = 25;
    private int shortBreakMinutes = 5;
    private int longBreakMinutes = 30;

    private Phase currentPhase;
    private int currentMinutesRemaining;
    private int currentSecondsRemaining;
    private float currentMillisecondsRemaining;
    private bool timerActive;
    private int workPhasesSinceLongBreak;

    public PomoTimer()
    {
        currentPhase = Phase.Work;
        currentMinutesRemaining = workMinutes;
        currentSecondsRemaining = 0;
        currentMillisecondsRemaining = MillisecondsInASecond;
        timerActive = false;
        workPhasesSinceLongBreak = 0;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        TimeLabel = GetNode<Label>(TimeLabelNodePath);
        PauseButton = GetNode<Button>(PauseButtonNodePath);
        PauseButtonTextureRect = GetNode<TextureRect>(PauseButtonTextureRectNodePath);
        ResetButton = GetNode<Button>(ResetButtonNodePath);
        UpperTimerTextureRect = GetNode<TextureRect>(UpperTimerTextureRectNodePath);
        LowerTimerTextureRect = GetNode<TextureRect>(LowerTimerTextureRectNodePath);

        UpdateTimerText();
        UpdateTimerTextures();
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
            UpdateTimerTextures();
        }
    }

    private void UpdateTimerText()
    {
        TimeLabel.Text = currentMinutesRemaining + ":" + currentSecondsRemaining.ToString("0#");
    }

    private void UpdateTimerTextures()
    {
        float millisecondsRemaining = currentMinutesRemaining * SecondsInAMinute * MillisecondsInASecond + currentSecondsRemaining * MillisecondsInASecond + currentMillisecondsRemaining;

        switch (currentPhase)
        {
            case Phase.Work:
            {
                int totalWorkMilliseconds = workMinutes * SecondsInAMinute * MillisecondsInASecond;

                UpperTimerTextureRect.SizeFlagsStretchRatio = 1.0f - millisecondsRemaining / totalWorkMilliseconds;
                LowerTimerTextureRect.SizeFlagsStretchRatio = millisecondsRemaining / totalWorkMilliseconds;
                break;
            }
            case Phase.ShortBreak:
            {
                int totalShortBreakMilliseconds = shortBreakMinutes * SecondsInAMinute * MillisecondsInASecond;

                UpperTimerTextureRect.SizeFlagsStretchRatio = millisecondsRemaining / totalShortBreakMilliseconds;
                LowerTimerTextureRect.SizeFlagsStretchRatio = 1.0f - millisecondsRemaining / totalShortBreakMilliseconds;
                break;
            }
            case Phase.LongBreak:
            {
                int totalLongBreakMilliseconds = longBreakMinutes * SecondsInAMinute * MillisecondsInASecond;

                UpperTimerTextureRect.SizeFlagsStretchRatio = millisecondsRemaining / totalLongBreakMilliseconds;
                LowerTimerTextureRect.SizeFlagsStretchRatio = 1.0f - millisecondsRemaining / totalLongBreakMilliseconds;
                break;
            }
        }
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
                break;
            }
            case Phase.ShortBreak:
            {
                currentMinutesRemaining = shortBreakMinutes;
                break;
            }
            case Phase.LongBreak:
            {
                currentMinutesRemaining = longBreakMinutes;
                workPhasesSinceLongBreak = 0;
                break;
            }
        }
        currentSecondsRemaining = 0;
        currentMillisecondsRemaining = MillisecondsInASecond;
        currentPhase = newPhase;
    }

    private void OnPauseButtonPressed()
    {
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
        timerActive = false;
        currentMinutesRemaining = workMinutes;
        currentSecondsRemaining = 0;
        currentMillisecondsRemaining = MillisecondsInASecond;
        currentPhase = Phase.Work;
        workPhasesSinceLongBreak = 0;

        PauseButtonTextureRect.Texture = PlayTexture;

        UpdateTimerText();
        UpdateTimerTextures();
    }
}
