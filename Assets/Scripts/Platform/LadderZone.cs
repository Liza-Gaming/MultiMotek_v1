using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LadderZone : MonoBehaviour
{
    public float climbSpeed = 3.5f;
    
    public bool snapXToCenter = true;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.25f);
        var c = GetComponent<Collider2D>();
        if (c) Gizmos.DrawCube(c.bounds.center, c.bounds.size);
    }
}