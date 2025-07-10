using UnityEngine;
//https://github.com/nickbota/Unity-Platformer-Episode-6/blob/main/2D%20Tutorial/Assets/Scripts/CameraController.cs
public class CameraMover : MonoBehaviour
{

    //Follow player
    [SerializeField] private Transform player;
    [SerializeField] private float aheadDistance;
    [SerializeField] private float cameraSpeed;

    private float lookAhead;

    [Header("Camera Bounds")]
    [SerializeField] private float minX;
    [SerializeField] private float maxX;

    private void Update()
    {
        // Calculate the camera's target X position (following the player)
        float targetX = player.position.x + lookAhead;

        // Clamp to bounds so camera stops moving at edges
        targetX = Mathf.Clamp(targetX, minX, maxX);

        // Set camera position
        transform.position = new Vector3(targetX, player.position.y+1, transform.position.z);

        // Usual lookAhead smoothing
        lookAhead = Mathf.Lerp(lookAhead, (aheadDistance * player.localScale.x), Time.deltaTime * cameraSpeed);
    }
}