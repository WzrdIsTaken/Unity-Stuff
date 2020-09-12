/// <summary>
/// My solution for saving data via PlayerPrefs. 
/// I think the SettingsMaster script is pretty decent (found at the bottom) but the SettingsManager is still kind of bot in places.
/// SettingsMaster works without SettingsManager, but if you're doing that you might as well just use PlayerPrefs or hardcode the values.
/// </summary>

/*
The frontend for saving data via PlayerPrefs.
Last updated on 12/09/20 and used in 'Raze but Multiplayer'.
Note: To use this script you must be using SettingsMaster.

To use this script:
    - Attach it to a GameObject.
    - Change / assign the relevant values (audiomixer, dropdowns, names of gameobjects to find in LoadControlSettings() and LoadVolumeSettings()).
    - For the UI objects:
        - Key buttons: Assign ChangeKey() as the event and make sure that the name of the GameObject is the same as the key for the PlayerPref it represents (eg: the 'Jump'
                       button should have the name 'jump' if the key for the PlayerPref is 'jump'). 
        - Volume sliders: Assign SetVolume() as the event and make sure that the name of the exposed param + 'Slider' is the same as the relevant UI slider.
        - Graphics dropdowns: The relevant function with a dynamic int.
*/

using TMPro;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

// The frontend for storing data via PlayerPrefs.
public class SettingsManager : MonoBehaviour
{
    [SerializeField] AudioMixer mixer;
    [SerializeField] TMP_Dropdown resolutionDropdown, fullScreenDropdown, graphicsQualityDropDown;

    Resolution[] resolutions;
    Dictionary<string, KeyCode> keys = new Dictionary<string, KeyCode>();
    GameObject currentKey;
    const float INVALID_SHOW_TIME = 0.2f;

    readonly Color32 normal = new Color32(190, 190, 190, 255);
    readonly Color32 selected = new Color32(230, 230, 230, 255);
    readonly Color32 invalid = new Color32(150, 0, 0, 255);

    void OnEnable ()
    {
        LoadControlsSettings();
    }

    public void LoadControlsSettings ()
    {
        keys.Clear();
        string[] keysArr = new string[] { "jump", "crouch", "left", "right", "shoot", "reload", "quickSwitchLeft", "quickSwitchRight", "pistolSwitch", "lightSwitch", "shotgunSwitch", "rifleSwitch", "sniperSwitch", "heavySwitch",
                                          "explosiveSwitch", "scoreboard", "settings" };
        foreach (string item in keysArr)
        {
            string value = SettingsMaster.GetValue<string>(item);
            keys.Add(item, (KeyCode)Enum.Parse(typeof(KeyCode), value));
            GameObject.Find(item + "ButtonText").GetComponent<TMP_Text>().text = value;
        }
    }

    public void LoadVolumeSettings ()
    {
        string[] paramArr = new string[] { "masterVolume", "musicVolume", "soundFxVolume" };
        foreach (string item in paramArr) GameObject.Find(item + "Slider").GetComponent<Slider>().value = SettingsMaster.GetValue<float>(item);
    }

    public void LoadGraphicsSettings ()
    {
        // Resolution
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        List<string> resolutionOptions = new List<string>();
        int currentResolutionIndex = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            resolutionOptions.Add(resolutions[i].width + " x " + resolutions[i].height);
            if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height) currentResolutionIndex = i;
        }
        resolutionDropdown.AddOptions(resolutionOptions);
        resolutionDropdown.value = SettingsMaster.GetValue<int>("resolution");
        resolutionDropdown.RefreshShownValue();

        // Fullscreen
        fullScreenDropdown.value = SettingsMaster.GetValue<int>("fullScreen");
        fullScreenDropdown.RefreshShownValue();

        // Graphics quality
        graphicsQualityDropDown.value = SettingsMaster.GetValue<int>("graphicsQuality");
        graphicsQualityDropDown.RefreshShownValue();
    }

    #region Controls

    void OnGUI ()
    {
        if (currentKey == null) return;
        Event e = Event.current;

        if (e.isKey) UpdateKey(e.keyCode);
        if (e.shift)
        {
            if (Input.GetKey(KeyCode.LeftShift)) UpdateKey(KeyCode.LeftShift);
            else UpdateKey(KeyCode.RightShift);
        }
        if (e.isMouse)
        {
            for (int i = 0; i < 6; i++)
            {
                if (Input.GetMouseButton(i)) UpdateKey((KeyCode)Enum.Parse(typeof(KeyCode), "Mouse" + i));
            }
        }
    }

    void UpdateKey (KeyCode key)
    {
        if (key == KeyCode.None) return;
        if (keys.ContainsValue(key))
        {
            StartCoroutine(InvalidKey(keys.First(x => x.Value == key).Key)); // Passes in the name of the invalid key GameObject to the InvalidKey method, found via the value (the KeyCode 'key'). 
            currentKey.GetComponent<Image>().color = normal;
            currentKey = null;
            return;
        }

        string keyName = currentKey.name;
        keys[keyName] = key;
        SettingsMaster.SetValue(keyName, key.ToString());
        currentKey.GetComponentInChildren<TMP_Text>().text = key.ToString();
        currentKey.GetComponent<Image>().color = normal;
        currentKey = null;
    }

    public void ChangeKey (GameObject clicked)
    {
        if (currentKey != null) currentKey.GetComponent<Image>().color = normal;

        currentKey = clicked;
        currentKey.GetComponent<Image>().color = selected;
    }

    IEnumerator InvalidKey (string invalidKey)
    {
        Image keyColour = GameObject.Find(invalidKey).GetComponent<Image>();
        keyColour.color = invalid;

        yield return new WaitForSecondsRealtime(INVALID_SHOW_TIME);
        keyColour.color = normal;
    }

    #endregion

    #region Audio

    public void SetVolume (Slider slider)
    {
        string exposedParam = slider.name.Replace("Slider", string.Empty);
        float volume = slider.value; // Linear

        float vol = Mathf.Log10(volume) * 20; // Convert the linear audio value (0-1) to logarithmic. 
        if (volume == 0) vol = -80f;
        mixer.SetFloat(exposedParam, vol);
        SettingsMaster.SetValue(exposedParam, volume);
    }

    #endregion

    #region Graphics

    public void SetQuality (int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        SettingsMaster.SetValue("graphicsQuality", qualityIndex);
    }

    public void SetFullScreenMode (int isFullScreen)
    {
        Screen.fullScreen = Convert.ToBoolean(isFullScreen);
        SettingsMaster.SetValue("fullScreen", isFullScreen); 
    }

    public void SetResolution (int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        SettingsMaster.SetValue("resolution", resolutionIndex);
    }

    #endregion
}

/*

The backend of saving data via PlayerPrefs. 
Last updated on 11/09/20 and used in 'Raze But Mulitplayer'. 
Note: If you use this, you have to use .NET 4.x (https://bit.ly/2Rjvm5G).

To use this script:
    - Attach it to a GameObject.
    - Change the keys and default values in SetDefaultValues().   
    - To set a PlayerPref -> SettingsMaster.SetValue(key, value); 
    - To get a PlayerPrefs -> SettingsMaster.GetValue<type>(key);

using UnityEngine;
using System;
using System.Collections.Generic;

// The backend for storing data via PlayerPrefs
public class SettingsMaster : MonoBehaviour
{
    Resolution[] resolutions;
    Dictionary<string, dynamic> defaultValues;

    static SettingsMaster settingsMaster;

    void Awake ()
    {
        if (settingsMaster)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        settingsMaster = this;      
    }

    void Start ()
    {
        resolutions = Screen.resolutions;
        SetDefaultValues();
        SetGraphicsSettings();
    }

    void SetDefaultValues ()
    {
        defaultValues = new Dictionary<string, dynamic>
        {
            // Movement
            { "jump", "W" }, { "crouch", "S" }, { "left", "A" }, { "right", "D" }, 

            // Combat
            { "shoot", "Mouse0"}, { "reload", "R" }, { "ability", "LeftShift" }, { "quickSwitchLeft", "Q" }, { "quickSwitchRight", "E" }, { "pistolSwitch", "Alpha1" }, { "lightSwitch", "Alpha2" }, { "shotgunSwitch", "Alpha3" },
            { "rifleSwitch", "Alpha4" }, { "sniperSwitch", "Alpha5" }, { "heavySwitch", "Alpha6" }, { "explosiveSwitch", "Alpha7" },

            // Other
            { "scoreboard", "Tab" }, { "settings", "Escape"},

            // Graphics
            { "graphicsQuality", 3}, { "fullScreen", "true" }, { "resolutions" , CheckDefaultResolution() },

            // Sound
            { "masterVolume", 0.3f }, { "musicVolume", 0.3f }, { "soundFxVolume", 0.3f},

            // Username
            { "username", "Player " + UnityEngine.Random.Range(0, 1000).ToString("0000") }
        };
    }

    void SetGraphicsSettings ()
    {
        QualitySettings.SetQualityLevel(GetValue<int>("graphicsQuality"));
        Screen.fullScreen = Convert.ToBoolean(GetValue<string>("fullScreen"));

        Resolution defaultResolution = resolutions[GetValue<int>("resolutions")];
        Screen.SetResolution(defaultResolution.width, defaultResolution.height, Screen.fullScreen);
    }

    int CheckDefaultResolution ()
    {
        int defaultResolution = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height) defaultResolution = i;
        }
        return defaultResolution;
    }

    public static void SetValue (string key, string value)
    {
        PlayerPrefs.SetString(key, value);
        PlayerPrefs.Save();
    }

    public static void SetValue (string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
    }

    public static void SetValue (string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
    }

    public static T GetValue<T> (string key)
    {
        return (T)Convert.ChangeType(settingsMaster.ReturnValue(key, settingsMaster.defaultValues[key]), typeof(T));
    }

    string ReturnValue (string key, string defaultValue)
    {
        return PlayerPrefs.GetString(key, defaultValue);
    }

    int ReturnValue (string key, int defaultValue)
    {
        return PlayerPrefs.GetInt(key, defaultValue);
    }

    float ReturnValue (string key, float defaultValue)
    {
        return PlayerPrefs.GetFloat(key, defaultValue);
    }
}

*/
