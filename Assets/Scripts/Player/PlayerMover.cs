using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/**
 *  I learned about 2D animation changing from here: https://www.youtube.com/watch?v=hkaysu1Z-N8&t=147s&ab_channel=Brackeys
 *  I learned about moving objects from here: https://www.youtube.com/watch?v=WxCsnNiJnhA&ab_channel=PPHGames
 *  To implement the player movements with the new input system (Unity 6) I watched this: https://www.youtube.com/watch?v=HmXU4dZbaMw&t=89s&ab_channel=SpudMasterStudios
 *  Minimaps: 
 *  https://www.youtube.com/watch?v=kWhOMJMihC0&t=274s&ab_channel=CodeMonkey
 *  https://www.youtube.com/watch?v=TkegkmRbrN0&t=640s&ab_channel=MuddyWolf
 */
public class PlayerMover : MonoBehaviour
{

    [SerializeField]
    [Tooltip("Control the player with defined keyboard buttons")]
    public InputAction PlayerControls;
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private float _speed = 5f;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    private bool isGrounded;

    private Rigidbody2D rb;

    //private bool facingRight = true; // Tells the direction that the player is facing

    private Vector3 initialScale;

    public GameObject landingEffectPrefab; // Landing Effect
    public Transform landingEffectPoint;

    private bool wasGroundedLastFrame = true;
    private bool inputLocked = false;

    //Vector2 moveDir = Vector2.zero; // Direction of the player by vector

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    void Start()
    {
        initialScale = transform.localScale;
    }

    void OnEnable()
    {
        moveAction.action.Enable();
        jumpAction.action.Enable();
    }

    void OnDisable()
    {
        moveAction.action.Disable();
        jumpAction.action.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        if (inputLocked) return;

        // Ground check
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);

        if (isGrounded && !wasGroundedLastFrame)
        {
            Instantiate(landingEffectPrefab, landingEffectPoint.position, Quaternion.identity);
        }

        wasGroundedLastFrame = isGrounded;

        if (jumpAction.action.triggered && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    // Moves the player in every frame when the user is clicking on the buttons
    void FixedUpdate()
    {
        if (inputLocked)
        {
            // להקפיא תנועה פיזיקלית אופקית
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        float moveInput = moveAction.action.ReadValue<Vector2>().x;
        rb.linearVelocity = new Vector2(moveInput * _speed, rb.linearVelocity.y);

        if (moveInput > 0.01f)
            transform.localScale = new Vector3(Mathf.Abs(initialScale.x), initialScale.y, initialScale.z);
        else if (moveInput < -0.01f)
            transform.localScale = new Vector3(-Mathf.Abs(initialScale.x), initialScale.y, initialScale.z);
    }
    
    public void SetInputLocked(bool locked)
    {
        inputLocked = locked;

        if (locked)
        {
            moveAction.action.Disable();
            jumpAction.action.Disable();
            
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            moveAction.action.Enable();
            jumpAction.action.Enable();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        }
    }

}