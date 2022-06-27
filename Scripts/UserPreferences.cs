using Godot;
using System;
using YamlDotNet.Serialization;

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
