using UnityEngine;

public class DieZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerManager player = other.GetComponent<PlayerManager>();
            player.ExplodeAndRespawn();
            var mover = player.GetComponent<PlayerMover>();
            if (mover != null) mover.SetInputLocked(true);
        }
    }
}
