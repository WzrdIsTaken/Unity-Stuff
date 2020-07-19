using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Originally used in Neon Dawn. 
/// Stuff to change if using for another project:
///     - The first scene name in OnFadeComplete().
/// Just make sure you have an image with can be filled and an animation.
/// </summary>

// Fades between scenes | Base Code: https://bit.ly/3clz9rP 
public class LevelChanger : MonoBehaviour
{
    [SerializeField] Image progressWheel;
    Animator animator;
    string levelToLoad;
    bool isFastLoad;

    static LevelChanger levelChanger;

    void Awake ()
    {
        if (levelChanger == null)
        {
            levelChanger = this;
            DontDestroyOnLoad(this);

            animator = GetComponent<Animator>();
            progressWheel.enabled = false;
        }
        else
        {
            Destroy(this);
            return;
        }
    }

    public static void ChangeScene (string scene, bool isFastLoad)
    {
        if (!levelChanger.CheckIfNull()) return;

        levelChanger.levelToLoad = scene;
        levelChanger.isFastLoad = isFastLoad;
        levelChanger.animator.SetTrigger("FadeOut");
    }

    public void OnFadeComplete ()
    {
        if (SceneManager.GetActiveScene().name == "FirstLoad")
        {
            SceneManager.LoadScene(levelToLoad);
            animator.SetTrigger("FadeIn");
        } 
        else StartCoroutine(LoadSceneAsync(levelToLoad, isFastLoad));
    }

    IEnumerator LoadSceneAsync (string sceneName, bool isFastLoad)
    {
        var operation = SceneManager.LoadSceneAsync(sceneName);

        if (!isFastLoad) // Stops the loading wheel flashing up for a frame if the load is really fast (eg, between the main menu scene and the level select scene).
        {
            progressWheel.enabled = true;
            progressWheel.fillAmount = 0f;

            while (!operation.isDone)
            {
                progressWheel.fillAmount = Mathf.Clamp01(operation.progress / .9f);
                yield return null;
            }

            progressWheel.enabled = false;
        }

        animator.SetTrigger("FadeIn");
    }

    bool CheckIfNull ()
    {
        if (levelChanger == null)
        {
            Debug.LogError("Unavailable LevelChanger component");
            return false;
        }
        else return true;
    }
}
