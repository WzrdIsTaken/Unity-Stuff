using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Originally used in Neon Dawn. 
/// Stuff to change if using for another project:
///     - Exposed param names in SetStartingValues().
///     - ULTING_PITCH necessary? Used in UltEffect().
///     - The exposed param in FadeSong().
///     - PlayRhythmModeMusic() needed?
/// Static methods meant to be accessed from other scripts. 
/// </summary>

// Changes the music. 
public class MusicManager : MonoBehaviour
{
    [SerializeField] AudioMixer mixer;
    [SerializeField] AudioClip menuSong;
    [SerializeField] List<AudioClip> toPlay;
    List<AudioClip> played = new List<AudioClip>();
    AudioSource source;
    const float PAUSED_VOLUME = 0.4f, ULTING_PITCH = 0.9f, NORMAL_VALUE= 1f;
    string currentSong;

    static MusicManager musicManager;

    void Awake ()
    {
        if (musicManager == null)
        {
            musicManager = this;
            DontDestroyOnLoad(this);

            source = GetComponent<AudioSource>();         
            PlaySong(menuSong, true);
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
        PlayMenuMusic();
    }

    void SetStartingValues()
    {
        string[] exposedParams = { "masterMixerVolume", "musicMixerVolume", "soundFxMixerVolume" };
        foreach (var item in exposedParams) mixer.SetFloat(item, Mathf.Log10(SettingsMaster.GetNum(item.Replace("Mixer", ""), true)) * 20);
        
        /* Spotted a potential bug here. If you're storing the volume in linear form then make sure you run a quick check to see if the user has set volume to 0.
        Eg:
            float vol = PlayerPrefs.GetFloat(item);
            if (vol == 0) mixer.SetFloat(item, -80f);
            else mixer.SetFloat(item, Mathf.Log10(vol) * 20);
        */
    }

    public static void PlayMenuMusic () 
    {
        if (!musicManager.CheckIfNull()) return;

        musicManager.CancelInvoke();

        if (musicManager.source.clip != musicManager.menuSong) musicManager.StartCoroutine(musicManager.FadeSong(0, 0.5f, musicManager.menuSong, true));
        else if (!musicManager.source.isPlaying) musicManager.PlaySong(musicManager.menuSong, true);
    }

    public static void PlayLevelMusic () 
    {
        if (!musicManager.CheckIfNull()) return;

        var song = musicManager.ChooseSong();
        if (song == null)
        {
            Debug.LogError("The music manager has no songs to play");
            return;
        }
        if (musicManager.source.clip == musicManager.menuSong) musicManager.StartCoroutine(musicManager.FadeSong(0, 0.5f, song, false));
        else musicManager.PlaySong(song, false);

        musicManager.Invoke("PlayLevelMusic", song.length);
    }

    public static void PlayRhythmModeMusic(string level)
    {
        if (!musicManager.CheckIfNull()) return;

        Debug.LogError("Not currently implemented. The level is " + level);
    }

    public static void PauseMusic (bool isPaused, bool isRhythmMode)
    {
        if (isPaused)
        {
            if (isRhythmMode) musicManager.source.Pause();
            else musicManager.source.volume = PAUSED_VOLUME;
        }
        else
        {
            if (isRhythmMode) musicManager.source.UnPause();
            else musicManager.source.volume = NORMAL_VALUE;
        }
    }

    public static void UltEffect (bool isUlting)
    {
        if (isUlting) musicManager.StartCoroutine(musicManager.FadePitch(ULTING_PITCH, 0.5f, true));
        else musicManager.StartCoroutine(musicManager.FadePitch(NORMAL_VALUE, 0.5f, false));
    }

    public static string GetSongName ()
    {
        return musicManager.currentSong;
    }

    void PlaySong (AudioClip song, bool shouldLoop)
    {
        currentSong = song.name;

        source.Stop();
        source.clip = song;
        source.loop = shouldLoop;
        source.Play();
    }

    AudioClip ChooseSong ()
    {
        if (toPlay.Count > 1)
        {
            var song = toPlay[Random.Range(0, toPlay.Count)];
            played.Add(song);
            toPlay.Remove(song);
            return song;
        }
        else if (toPlay.Count == 1) // Makes sure the same song doesn't get played twice in a row
        {
            var lastSong = toPlay[0];
            toPlay = played;
            played.Clear();
            played.Add(lastSong);
            return lastSong;
        }
        else return null;
    }

    // Base code: http://bitly.ws/8fHy
    IEnumerator FadeSong (float targetVolume, float duration, AudioClip nextSong, bool shouldLoopNext)
    {
        const string MUSIC_MIXER_VOLUME = "musicMixerVolume";
        var currentTime = 0f;

        mixer.GetFloat(MUSIC_MIXER_VOLUME, out var currentVol);
        var startVol = currentVol;
        currentVol = Mathf.Pow(10, currentVol / 20);
        var targetValue = Mathf.Clamp(targetVolume, 0.0001f, 1);

        while (currentTime < duration) // Fade out 
        {
            currentTime += Time.deltaTime;
            var newVol = Mathf.Lerp(currentVol, targetValue, currentTime / duration);
            mixer.SetFloat(MUSIC_MIXER_VOLUME, Mathf.Log10(newVol) * 20);
            yield return null;
        }

        currentTime = 0f;
        PlaySong(nextSong, shouldLoopNext);

        while (currentTime < duration) // Fade in
        {
            currentTime += Time.deltaTime;
            var newVol = Mathf.Lerp(startVol, currentVol, currentTime / duration);
            mixer.SetFloat(MUSIC_MIXER_VOLUME, Mathf.Log10(newVol) * 20);
            yield return null;
        }
    }

    IEnumerator FadePitch (float targetPitch, float duration, bool fadeOut)
    {
        var currentTime = 0f;
        var startPitch = source.pitch;

        if (fadeOut)
        {
            while (currentTime < duration)
            {
                currentTime += Time.deltaTime;
                source.pitch = Mathf.Lerp(startPitch, targetPitch, currentTime / duration);
                yield return null;
            }
        }

        else
        {
            while (currentTime < duration)
            {
                currentTime += Time.deltaTime;
                source.pitch = Mathf.Lerp(startPitch, targetPitch, currentTime / duration);
                yield return null;
            }
        }
    }

    bool CheckIfNull ()
    {
        if (musicManager == null)
        {
            Debug.LogError("Unavailable MusicManager component");
            return false;
        }
        else if (musicManager.source == null)
        {
            Debug.LogError("The MusicManager GameObject has no audio source");
            return false;
        }
        else return true;
    }
}
