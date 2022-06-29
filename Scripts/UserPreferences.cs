using Godot;
using System;
using YamlDotNet.Serialization;

/// <summary>
/// Stores user preferences for the timer app.
/// </summary>
public class UserPreferences
{
    public int UserPreferencesVersion = 2;
    public float SfxVolume = 50.0f;
    public Color UpperTimerColor = new Color("#ff7676");
    public Color LowerTimerColor = new Color("#8dd1ff");
    public int WorkMinutes = 25;
    public int ShortBreakMinutes = 5;
    public int LongBreakMinutes = 30;
    public int LongBreakFrequency = 5;

    public Vector2 WindowSize;

    public UserPreferences()
    {
        WindowSize = OS.WindowSize;
    }

    /// <summary>
    /// Static creation of UserPreferences.<br/>
    /// Reads in an existing user preferences file if it exists, or creates a new one otherwise.
    /// </summary>
    /// <param name="fileName">The name of the user preferences file.</param>
    /// <returns>A UserPreferences that is loaded from file if it exists, or created with default values otherwise.</returns>
    public static UserPreferences CreateFromFile(string fileName)
    {
        File userPreferencesFile = new File();

        if (userPreferencesFile.FileExists("user://" + fileName))
        {
            userPreferencesFile.Open("user://" + fileName, File.ModeFlags.Read);
            string yamlInput = userPreferencesFile.GetAsText();
            userPreferencesFile.Close();

            Deserializer deserializer = new Deserializer();
            return deserializer.Deserialize<UserPreferences>(yamlInput);
        }
        else
        {
            return new UserPreferences();
        }
    }

    /// <summary>
    /// Save the user preferences to a file with the specified file name.
    /// </summary>
    /// <param name="fileName">The file name for the saved file.</param>
    public void SaveToFile(string fileName)
    {
        Serializer serializer = new Serializer();
        string yamlOutput = serializer.Serialize(this);

        File userPreferencesFile = new File();
        userPreferencesFile.Open("user://" + fileName, File.ModeFlags.Write);
        userPreferencesFile.StoreString(yamlOutput);
        userPreferencesFile.Close();
    }
}
