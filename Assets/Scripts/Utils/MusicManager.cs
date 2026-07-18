using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Audio Settings")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private float normalVolume = 0.5f;
    [SerializeField] private float tutorialVolume = 0.05f;
    [SerializeField] private float fadeSpeed = 3f;

    [Header("Custom Loop Settings")]
    [SerializeField] private bool useCustomLoop = false;
    
    [SerializeField] private float loopStartSeconds = 0f;
    
    [SerializeField] private float loopEndSeconds = 60f;

    private float targetVolume;
    private bool isMuted = false;
    private bool inTutorial = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();
            
        targetVolume = normalVolume;
        
        if (musicSource != null)
        {
            musicSource.loop = true;
        }
    }

    private void Update()
    {
        if (musicSource == null) return;
        
        float desiredVolume = isMuted ? 0f : (inTutorial ? tutorialVolume : normalVolume);
        musicSource.volume = Mathf.Lerp(musicSource.volume, desiredVolume, Time.deltaTime * fadeSpeed);
        
        if (useCustomLoop && musicSource.isPlaying)
        {
            if (musicSource.time >= loopEndSeconds)
            {
                musicSource.time = loopStartSeconds; 
            }
        }
    }

    public void SetTutorialMode(bool isTutorial)
    {
        inTutorial = isTutorial;
    }

    public void ToggleMute(bool mute)
    {
        isMuted = mute;
    }

    public bool IsMuted()
    {
        return isMuted;
    }
    
    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }
    
    
}