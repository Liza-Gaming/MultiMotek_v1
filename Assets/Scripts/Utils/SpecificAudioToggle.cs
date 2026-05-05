using UnityEngine;
using UnityEngine.EventSystems;

public class SpecificAudioToggle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public enum AudioTargetType { SFX, Music }

    [Header("Setup")]
    [Tooltip("בחרי האם הכפתור הזה שולט באפקטים (ההורה) או במוזיקה (הילד)")]
    [SerializeField] private AudioTargetType targetType;

    [Header("UI Elements")]
    [SerializeField] private GameObject muteIcon;

    [Header("Hover Settings")]
    [SerializeField] private float hoverScaleMultiplier = 1.1f;
    [SerializeField] private float scaleSpeed = 15f;

    private AudioSource targetAudioSource;
    private Vector3 originalScale;
    private Vector3 targetScale;
    private bool isMuted = false;

    private void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;

        // מציאת השחקן מחדש בכל תחילת סצנה
        FindTargetSource();

        if (targetAudioSource != null)
        {
            isMuted = targetAudioSource.mute;
        }
        
        if (muteIcon != null)
        {
            muteIcon.SetActive(isMuted);
        }
    }

    private void FindTargetSource()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            if (targetType == AudioTargetType.SFX)
            {
                targetAudioSource = player.GetComponent<AudioSource>();
            }
            else if (targetType == AudioTargetType.Music)
            {
                
                //targetAudioSource = player.GetComponentInChildren<AudioSource>();
                
                targetAudioSource = player.transform.Find("Audio Source").GetComponent<AudioSource>();
            }
        }
        
        if (targetAudioSource == null)
        {
            Debug.LogWarning($"[SpecificAudioToggle] לא נמצא AudioSource עבור {targetType} בסצנה הזו!");
        }
    }

    private void Update()
    {
        // אם השחקן עבר סצנה והקשר אבד, ננסה למצוא אותו שוב (ליתר ביטחון)
        if (targetAudioSource == null)
        {
            FindTargetSource();
            return; 
        }

        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScaleMultiplier;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (targetAudioSource == null) return;

        isMuted = !isMuted; 
        
        if (muteIcon != null)
        {
            muteIcon.SetActive(isMuted);
        }

        targetAudioSource.mute = isMuted;
    }
}