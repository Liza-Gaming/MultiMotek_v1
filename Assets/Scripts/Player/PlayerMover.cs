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
    
    private static readonly int P_IsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int T_Jump       = Animator.StringToHash("Jump");
    private static readonly int T_Land       = Animator.StringToHash("Land");

    private Rigidbody2D rb;
    
    [SerializeField] private Animator animator;

    //private bool facingRight = true; // Tells the direction that the player is facing

    private Vector3 initialScale;

    public GameObject landingEffectPrefab;
    public Transform landingEffectPoint;

    private bool wasGroundedLastFrame = true;
    private bool inputLocked = false;
    
    [SerializeField] private LayerMask platformLayer;
    [SerializeField] private float platformRayLen = 0.25f;

    private MovingPlatform currentPlatform;
    
    
    [Header("Continuous Movement → Sugar Drain")]
    [SerializeField] private SugarMeter sugarMeter;
    [SerializeField, Tooltip("כמה דק׳-משחק רצופות עד הורדה")]
    private float continuousMoveThresholdGameMinutes = 20f;
    [SerializeField, Tooltip("כמה יחידות סוכר להוריד בכל בלוק")]
    private float sugarDrainPerBlock = 4f;
    [SerializeField, Tooltip("מהירות מינימלית (יחסית לפלטפורמה) שנחשבת תנועה")]
    private float moveSpeedEpsilon = 0.1f;
    [SerializeField, Tooltip("כמה שניות אמיתיות מותר להיות כמעט סטטית בלי לאפס רצף")]
    private float stopForgivenessRealSeconds = 0.25f;

// צוברים זמן תנועה בדקות-משחק
    private float movingGameSecondsAccum = 0f;
    private float idleForgivenessTimer = 0f;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!animator) animator = GetComponent<Animator>();
    }
    void Start()
    {
        initialScale = transform.localScale;
        if (!sugarMeter) sugarMeter = SugarMeter.Instance ? SugarMeter.Instance : FindObjectOfType<SugarMeter>();
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
            
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);
            
            if (isGrounded)
                UpdateCurrentPlatform();
            else
                currentPlatform = null;
            
            if (animator) animator.SetBool(P_IsGrounded, isGrounded);
            
            if (isGrounded && !wasGroundedLastFrame)
            {
                if (landingEffectPrefab)
                    Instantiate(landingEffectPrefab, landingEffectPoint.position, Quaternion.identity);
                
                if (animator) animator.SetTrigger(T_Land);
            }
            
            if (jumpAction.action.triggered && isGrounded)
            {
                float platformX = (currentPlatform != null) ? currentPlatform.Velocity.x : 0f;
                rb.linearVelocity = new Vector2(platformX + moveAction.action.ReadValue<Vector2>().x * _speed, jumpForce);
                
                if (animator) animator.SetTrigger(T_Jump);
            }
            
            if (!isGrounded && wasGroundedLastFrame)
            {
                if (animator) animator.SetTrigger(T_Jump);
            }
            
            wasGroundedLastFrame = isGrounded;
            
            TrackContinuousMovementSugar();

    }
    
    private void UpdateCurrentPlatform()
    {
        currentPlatform = null;
        
        RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, platformRayLen, platformLayer);
        if (hit.collider != null)
            currentPlatform = hit.collider.GetComponentInParent<MovingPlatform>();
    }

    void FixedUpdate()
    {
        if (inputLocked)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        float moveInput = moveAction.action.ReadValue<Vector2>().x;

        Vector2 v = rb.linearVelocity;
        v.x = moveInput * _speed;
        
        if (isGrounded && currentPlatform != null)
        {
            Vector2 pv = currentPlatform.Velocity;
            v.x += pv.x;
            
            if (pv.y > 0f && v.y < pv.y)
                v.y = pv.y;
        }

        rb.linearVelocity = v;
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
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * platformRayLen);
        }
    }
    
    private void TrackContinuousMovementSugar()
    {
        if (inputLocked) // בזמן נעילה לא סופרים
        {
            movingGameSecondsAccum = 0f;
            idleForgivenessTimer = 0f;
            return;
        }
        
        Vector2 platformVel = (currentPlatform != null) ? currentPlatform.Velocity : Vector2.zero;
        Vector2 relVel = rb.linearVelocity - platformVel;

        bool isMovingNow = relVel.magnitude > moveSpeedEpsilon;

        if (isMovingNow)
        {

            movingGameSecondsAccum += GameTime.RealSecondsToGameSeconds(Time.deltaTime);
            idleForgivenessTimer = 0f;

            float blockGameSeconds = continuousMoveThresholdGameMinutes * 60f;
            
            if (movingGameSecondsAccum >= blockGameSeconds)
            {
                int blocks = Mathf.FloorToInt(movingGameSecondsAccum / blockGameSeconds);
                movingGameSecondsAccum -= blocks * blockGameSeconds;

                if (sugarMeter)
                {
                    
                    sugarMeter.DecreaseSugarGame(sugarDrainPerBlock, durationGameMin: 1f);
                }

                // אם תרצי חץ למטה רגעית בזמן הורדה:
                // GetComponentInChildren<SugarChangeArrow>()?.ShowDown(1f);
            }
        }
        else
        {
            // סליחת רפרופים קצרה כדי לא לאפס מייד
            idleForgivenessTimer += Time.deltaTime;
            if (idleForgivenessTimer >= stopForgivenessRealSeconds)
            {
                movingGameSecondsAccum = 0f;
            }
        }
    }

    

}