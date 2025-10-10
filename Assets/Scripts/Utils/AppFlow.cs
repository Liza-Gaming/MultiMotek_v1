using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public enum LaunchMode { None, Campaign, Standalone }

public static class AppFlow
{
    public static LaunchMode Mode { get; private set; } = LaunchMode.None;
    
    private static bool _armStandaloneInitForNextScene;

    public static bool ConsumeStandaloneInitFlag()
    {
        bool armed = _armStandaloneInitForNextScene;
        _armStandaloneInitForNextScene = false;
        return armed;
    }

    public static void StartStandalone(string sceneName, MonoBehaviour runner)
    {
        runner.StartCoroutine(StartStandaloneRoutine(sceneName));
    }

    private static IEnumerator StartStandaloneRoutine(string sceneName)
    {
        Mode = LaunchMode.Standalone;
        _armStandaloneInitForNextScene = true;

        PersistentRoot.DestroyAll();
        yield return null;

        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    public static void ContinueToNext(string sceneName)
    {
        Mode = LaunchMode.Campaign;
        _armStandaloneInitForNextScene = false;
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}