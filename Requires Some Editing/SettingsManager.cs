using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Collections;
using UnityEngine.Audio;

/// <summary>
/// Originally used in Neon Dawn. 
/// Stuff to change if using for another project:
///     - The exposed param names (MASTER_VOLUME, etc).
///     - The control / exposed param names in LoadSettings().
///     - Need to change the game object names for GameObject.Find in LoadSettings()?
///     - Names of the sliders in SliderInUse().
/// Meant to be used with a SettingsMaster like script which stores the settings values in PlayerPrefs. I've copy / pasted that at the bottom of this script, the only 
/// things that needs to change there is the PlayerPref keys / default values. Just make sure they match with the SettingsManager script.
/// </summary>

// The front end for changing settings | Base Code: https://bit.ly/2TrHAK1 
public class SettingsManager : MonoBehaviour
{
    [SerializeField] AudioMixer mixer;
    Resolution[] resolutions;
    Dictionary<string, KeyCode> keys = new Dictionary<string, KeyCode>();
    GameObject currentKey;
    const float INVALID_SHOW_TIME = 0.2f;
    const string MASTER_VOLUME = "masterMixerVolume", MUSIC_VOLUME = "musicMixerVolume", SOUNDFX_VOLUME = "soundFxMixerVolume";
    bool masterSlider, musicSlider, soundFxSlider, dontUpdateVolume;

    Color32 normal = new Color32(190, 190, 190, 255);
    Color32 selected = new Color32(230, 230, 230, 255);
    Color32 invalid = new Color32(150, 0, 0, 255);

    public void LoadSettings ()
    {
        dontUpdateVolume = true;
        keys.Clear();

        // Controls
        var list = new List<string> { "forward", "backward", "left", "right", "crouch", "slide", "jump", "grapple", "ultimate", "primaryFire", "secondaryFire", "reload", "pause" };
        string textObjectName = "ButtonText";
        foreach (var item in list)
        {
            var value = SettingsMaster.GetValue(item);
            keys.Add(item, (KeyCode)Enum.Parse(typeof(KeyCode), value));
            GameObject.Find(item + textObjectName).GetComponent<Text>().text = value;
        }

        // Volume
        list.Clear();
        list.Add("masterVolume"); list.Add("musicVolume"); list.Add("soundFxVolume");
        textObjectName = "Slider";
        foreach (var item in list) GameObject.Find(item + textObjectName).GetComponent<Slider>().value = SettingsMaster.GetNum(item, true);

        GameObject.Find("masterVolume" + textObjectName).GetComponent<Slider>().value = PlayerPrefs.GetFloat("masterVolume");

        // Graphics
        var resolutionDropdown = GameObject.Find("ResolutionOptions").GetComponent<Dropdown>();
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        var resolutionOptions = new List<string>();
        var currentResolutionIndex = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            resolutionOptions.Add(resolutions[i].width + " x " + resolutions[i].height);
            if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height) currentResolutionIndex = i;
        }
        resolutionDropdown.AddOptions(resolutionOptions);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        GameObject.Find("FullScreenToggle").GetComponent<Toggle>().isOn = Convert.ToBoolean(SettingsMaster.GetValue("fullScreen"));

        var graphicsDropDown = GameObject.Find("GraphicsOptions").GetComponent<Dropdown>();
        graphicsDropDown.value = SettingsMaster.GetNum("graphicsQuality");
        graphicsDropDown.RefreshShownValue();

        dontUpdateVolume = false;
    }

    // Controls
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

        var keyName = currentKey.name;
        keys[keyName] = key;
        SettingsMaster.SetValue(keyName, key.ToString());
        currentKey.GetComponentInChildren<Text>().text = key.ToString();
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
        var keyColour = GameObject.Find(invalidKey).GetComponent<Image>();
        keyColour.color = invalid;

        yield return new WaitForSecondsRealtime(INVALID_SHOW_TIME);
        keyColour.color = normal;
    }

    // Sound
    public void SliderInUse (GameObject slider)
    {
        switch (slider.name)
        {
            case "masterVolume":
                masterSlider = true; musicSlider = false; soundFxSlider = false;
                break;
            case "musicVolume":
                masterSlider = false; musicSlider = true; soundFxSlider = false;
                break;
            case "soundFxVolume":
                masterSlider = false; musicSlider = false; soundFxSlider = true;
                break;
        }
    }

    public void SetVolume (float volume)
    {
        if (dontUpdateVolume) return;

        if (masterSlider) UpdateVolume(volume, MASTER_VOLUME);
        if (musicSlider) UpdateVolume(volume, MUSIC_VOLUME);
        if (soundFxSlider) UpdateVolume(volume, SOUNDFX_VOLUME);
    }

    void UpdateVolume (float volume, string exposedParam)
    {
        var vol = Mathf.Log10(volume) * 20;
        if (volume == 0) vol = -80f;
        mixer.SetFloat(exposedParam, vol);
        SettingsMaster.SetValue(exposedParam.Replace("Mixer", ""), volume);
    }

    // Graphics
    public void SetQuality (int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        SettingsMaster.SetValue("graphicsQuality", qualityIndex);
    }

    public void ToggleFullScreen (bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        SettingsMaster.SetValue("fullScreen", isFullscreen.ToString().ToLower());
    }

    public void SetResolution (int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }
}

/*

using UnityEngine;
using System;

// Manages + stores settings infomation
public class SettingsMaster : MonoBehaviour
{
    Resolution[] resolutions;

    static SettingsMaster settingsMaster;

    void Awake()
    {
        if (settingsMaster == null)
        {
            settingsMaster = this;
            DontDestroyOnLoad(this);

            resolutions = Screen.resolutions; 

            #region PlayerPrefs

            PlayerPrefs.GetString("forward", "W");           PlayerPrefs.GetString("left", "A");
            PlayerPrefs.GetString("backward", "S");          PlayerPrefs.GetString("right", "D");
            PlayerPrefs.GetString("crouch", "LeftShift");    PlayerPrefs.GetString("slide", "LeftControl");
            PlayerPrefs.GetString("jump", "Space");          PlayerPrefs.GetString("grapple", "E");
            PlayerPrefs.GetString("ultimate", "Q");          PlayerPrefs.GetString("reload", "R");
            PlayerPrefs.GetString("primaryFire", "Mouse0");  PlayerPrefs.GetString("secondaryFire", "Mouse1");
            PlayerPrefs.GetString("pause", "Escape");

            PlayerPrefs.GetFloat("masterVolume", 0.3f);      PlayerPrefs.GetFloat("musicVolume", 0.3f); // Volume stored in linear form, not logarithmic.
            PlayerPrefs.GetFloat("soundFxVolume", 0.3f);

            PlayerPrefs.GetInt("graphicsQuality", 3);        PlayerPrefs.GetInt("resolution", CheckDefaultResolution());
            PlayerPrefs.GetString("fullScreen", "true");

            #endregion
        }
        else
        {
            Destroy(this);
            return;
        }
    }

    void Start () 
    {
        SetStartingValues();
    }

    void SetStartingValues ()
    {
        QualitySettings.SetQualityLevel(GetNum("graphicsQuality"));
        Screen.fullScreen = Convert.ToBoolean(GetValue("fullScreen"));
        var defaultResolution = resolutions[GetNum("resolution")];
        Screen.SetResolution(defaultResolution.width, defaultResolution.height, Screen.fullScreen);
    }

    int CheckDefaultResolution ()
    {
        var defaultResolution = 0;
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

    public static void SetValue (string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
    }

    public static void SetValue (string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
    }

    public static string GetValue (string key)
    {
        return PlayerPrefs.GetString(key);
    }

    public static float GetNum (string key, bool isFloat) 
    {
        return PlayerPrefs.GetFloat(key);
    }

    public static int GetNum (string key) 
    {
        return PlayerPrefs.GetInt(key);
    }
}

*/