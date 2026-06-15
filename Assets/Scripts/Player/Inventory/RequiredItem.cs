using UnityEngine;
using UnityEngine.UI;

public class RequiredItem : MonoBehaviour
{
    [SerializeField] Image[] itemImage;
    [SerializeField] GameObject[] bg;
    [SerializeField] private AudioClip pickupSound;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // מציאת ה-AudioSource שעל השחקן (ההורה)
            AudioSource playerAudio = other.GetComponent<AudioSource>();
            
            if (pickupSound != null && playerAudio != null)
            {
                playerAudio.PlayOneShot(pickupSound, volume);
            }
            
            PlayerManager collector = other.GetComponent<PlayerManager>();
            if (collector != null)
            {
                collector.hasRequiredItem = true;
                for (int i = 0; i < itemImage.Length; i++)
                {
                    itemImage[i].color = Color.white;
                }
                for (int i = 0; i < bg.Length; i++)
                {
                    bg[i].SetActive(false);
                }
                Destroy(gameObject);
            }
        }
    }
}