using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

/// <summary>
/// Originally used in Neon Dawn. 
/// Stuff to change if using for another project:
///     - Change the name of the data file in Awake().
/// Meant to be used with a SavedData like script. I've copy / pasted that at the bottom of this script, the only things that needs
/// to change there is what is saved. The public static methods are meant to update the data in the SavedData class. 
/// </summary>

// Manages and stores the users progression infomation | Base Code: https://bit.ly/2yttGAF
public class ProgressManager : MonoBehaviour
{
    public int levelsUnlocked, levelsComplete;
    public bool hasUnlockedRhythmMode;
    string path;

    static ProgressManager progressManager;

    void Awake ()
    {
        if (progressManager == null)
        {
            progressManager = this;
            DontDestroyOnLoad(this);

            // Default values
            levelsUnlocked = 1;
            levelsComplete = 0;
            hasUnlockedRhythmMode = false;

            // Load saved values
            path = Path.Combine(Application.persistentDataPath, "NeonDawn.data");
            var data = LoadData();
            levelsUnlocked = data.levelsUnlocked;
            levelsComplete = data.levelsComplete;
            hasUnlockedRhythmMode = data.hasUnlockedRhythmMode;
        }
        else
        {
            Destroy(this);
            return;
        }
    }

    void SaveData ()
    {
        using (var stream = new FileStream(path, FileMode.Create))
        {
            var formatter = new BinaryFormatter();
            var data = new SavedData(progressManager);
            formatter.Serialize(stream, data);
            stream.Close();
        }
    }

    SavedData LoadData ()
    {
        var formatter = new BinaryFormatter(); 

        if (File.Exists(path))
        {
            using (var stream = new FileStream(path, FileMode.Open))
            {
                var data = formatter.Deserialize(stream) as SavedData;
                stream.Close();

                return data;
            }
        }
        else
        {
            using (var stream = new FileStream(path, FileMode.Create))
            {
                var data = new SavedData(progressManager);
                formatter.Serialize(stream, data);
                stream.Close();

                Debug.Log("No file found at path '" + path + "', creating new data file.");
                return LoadData();
            }
        } 
    }

    public static void UpdateUnlockedLevels (int unlocked, int complete)
    {
        progressManager.levelsUnlocked = unlocked;
        progressManager.levelsComplete = complete;
        progressManager.SaveData();
    }

    public static void UpdateRhythmModeUnlock ()
    {
        progressManager.hasUnlockedRhythmMode = true;
        progressManager.SaveData();
    }

    public static bool CheckRhythmUnlock ()
    {
        return progressManager.hasUnlockedRhythmMode;
    }

    public static int CheckLevelsUnlocked ()
    {
        return progressManager.levelsUnlocked;
    }

    public static int CheckLevelsComplete ()
    {
        return progressManager.levelsComplete;
    }

    void OnApplicationQuit ()
    {
        progressManager.SaveData();
    }
}

/*

using System;

// The backend for saving the users progress | Base Code: https://bit.ly/2yttGAF
[Serializable]
public class SavedData
{
    public int levelsUnlocked, levelsComplete;
    public bool hasUnlockedRhythmMode;

    public SavedData (ProgressManager progressManager)
    {
        levelsUnlocked = progressManager.levelsUnlocked;
        levelsComplete = progressManager.levelsComplete;
        hasUnlockedRhythmMode = progressManager.hasUnlockedRhythmMode;
    }
}

*/
