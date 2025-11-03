using UnityEngine;

public class ItemHover : MonoBehaviour
{
    
    [SerializeField] private float hoverHeight = 0.25f;
    
    [SerializeField] private float hoverSpeed = 1.5f;
    
    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        
        float sinValue = Mathf.Sin(Time.time * hoverSpeed);

        float yOffset = sinValue * (hoverHeight / 2);


        transform.position = new Vector3(
            startPosition.x,
            startPosition.y + yOffset,
            startPosition.z
        );
    }
}