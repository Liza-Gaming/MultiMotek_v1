using UnityEngine;

public class FollowPlayerLight : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, 0f);

    private Transform player;

    void Update()
    {
        if (player == null)
        {
            var tagged = GameObject.FindGameObjectWithTag(playerTag);
            if (tagged != null) player = tagged.transform;
            else return;
        }
        
        transform.position = player.position + offset;
    }
}