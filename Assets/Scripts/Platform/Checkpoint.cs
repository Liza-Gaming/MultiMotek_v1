using UnityEngine;
using System.Collections;
public class Checkpoint : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Set this as the new checkpoint
            PlayerManager respawn = other.GetComponent<PlayerManager>();
            if (respawn != null)
                respawn.SetCheckpoint(transform);
        }
    }
}
