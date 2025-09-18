using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using UnityEngine.SceneManagement;

public class Pause : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject pauseBtn;
    [SerializeField] private GameObject infoBtn;
    [SerializeField] private GameObject resumeBtn;
    [SerializeField] private GameObject pauseShield;

    [Header("Blur")]
    [SerializeField] private Volume pauseBlurVolume;
    [SerializeField] private Volume[] volumesToMute; 
    [SerializeField] private float blurInTime = 0.2f;
    [SerializeField] private float blurOutTime = 0.15f;

    [Header("Audio")]
    // [SerializeField] private bool pauseAudio = true;

    private Coroutine blurRoutine;
    private float[] prevWeights;

    void Start()
    {
        SetActiveSafe(pauseBtn,  true);
        SetActiveSafe(infoBtn,   true);
        SetActiveSafe(resumeBtn, false);
        SetActiveSafe(pauseShield, false);
        

        if (pauseBlurVolume) pauseBlurVolume.weight = IsPaused ? 1f : 0f;

        if (IsPaused)
        {
            Time.timeScale = 0f;
            RefreshVolumesToMute();
            ToggleGameplayVolumes(false);

            SetActiveSafe(pauseBtn,   false);
            SetActiveSafe(infoBtn,    false);
            SetActiveSafe(resumeBtn,  true);
            SetActiveSafe(pauseShield, true);
        }
        else
        {
            Time.timeScale = 1f;
            ToggleGameplayVolumes(true);
        }
    }
    
    
    public void PausGeame()
    {
        if (IsPaused) return;
        IsPaused = true;

        Time.timeScale = 0f;
        ToggleGameplayVolumes(false);
        
        SetActiveSafe(pauseBtn, false);
        SetActiveSafe(infoBtn, false);
        SetActiveSafe(resumeBtn, true);
        SetActiveSafe(pauseShield, true);
        
        FadePauseBlur(true);
        // if (pauseAudio) AudioListener.pause = true;
        
        var btn = resumeBtn ? resumeBtn.GetComponent<Button>() : null;
        if (btn) EventSystem.current?.SetSelectedGameObject(btn.gameObject);

        // למחשב
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Menu()
    {
        SceneManager.LoadScene("Intro");
    }
    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;

        Time.timeScale = 1f;
        ToggleGameplayVolumes(true);
        
        SetActiveSafe(pauseShield, false);
        SetActiveSafe(resumeBtn, false);
        SetActiveSafe(infoBtn, true);
        SetActiveSafe(pauseBtn, true);

        FadePauseBlur(false);
        // if (pauseAudio) AudioListener.pause = false;
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        
        if (pauseBlurVolume) DontDestroyOnLoad(pauseBlurVolume.gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (pauseBlurVolume) pauseBlurVolume.weight = 0f;
        RestoreMutedVolumes();
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        RefreshVolumesToMute();

        if (IsPaused)
        {
            Time.timeScale = 0f;
            ToggleGameplayVolumes(false);

            if (pauseBlurVolume) pauseBlurVolume.weight = 1f;
            SetActiveSafe(pauseBtn,   false);
            SetActiveSafe(infoBtn,    false);
            SetActiveSafe(resumeBtn,  true);
            SetActiveSafe(pauseShield, true);
        }
        else
        {
            if (pauseBlurVolume) pauseBlurVolume.weight = 0f;
        }
    }


    private void ToggleGameplayVolumes(bool restore)
    {
        if (volumesToMute == null || volumesToMute.Length == 0) return;

        if (!restore)
        {
            prevWeights = new float[volumesToMute.Length];
            for (int i = 0; i < volumesToMute.Length; i++)
            {
                if (!volumesToMute[i]) continue;
                prevWeights[i] = volumesToMute[i].weight;
                volumesToMute[i].weight = 0f;
            }
        }
        else
        {
            RestoreMutedVolumes();
        }
    }

    private void RestoreMutedVolumes()
    {
        if (volumesToMute == null || prevWeights == null) return;
        for (int i = 0; i < volumesToMute.Length && i < prevWeights.Length; i++)
            if (volumesToMute[i]) volumesToMute[i].weight = prevWeights[i];
    }

    private void FadePauseBlur(bool on)
    {
        if (!pauseBlurVolume) return;
        if (blurRoutine != null) StopCoroutine(blurRoutine);
        blurRoutine = StartCoroutine(AnimateWeight(pauseBlurVolume, on ? 1f : 0f, on ? blurInTime : blurOutTime));
    }

    private IEnumerator AnimateWeight(Volume v, float target, float dur)
    {
        float start = v.weight;
        float t = 0f;
        dur = Mathf.Max(0.0001f, dur);
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            v.weight = Mathf.Lerp(start, target, t);
            yield return null;
        }
        v.weight = target;
    }

    private static void SetActiveSafe(GameObject go, bool active)
    {
        if (go && go.activeSelf != active) go.SetActive(active);
    }
    
    private void RefreshVolumesToMute()
    {
        var list = new System.Collections.Generic.List<Volume>();
        var all = FindObjectsByType<Volume>(FindObjectsSortMode.None);
        foreach (var v in all)
        {
            if (!v || v == pauseBlurVolume) continue;
            list.Add(v);
        }
        volumesToMute = list.ToArray();
    }

}
