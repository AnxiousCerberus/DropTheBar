using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//Sebastian Lague on Youtube, kudos to his 2D CharController tuto!
public class Characters : MonoBehaviour
{
    [Header("---!DEV MODE!---")]
    public bool ImmediateTestMode = false;

    [Header("Common character vars")]
    public int maxHealth = 4;
    public int currentHealth = 4;

    #region Basic Moves Inspector Variables
    [Header("Basic moves options")]
    public bool GravityOn = true;
    public float speed = 10;
    public float jumpHeight = 4;
    public float timeToJumpApex = .4f;
    #endregion

    #region Character Collision System
    [Header("Collision Detection Options")]
    public float skinWidth = .015f;

    [HideInInspector]
    public RaycastOrigins raycastOrigins;

    [SerializeField]
    int horizontalRayCount = 4;
    [SerializeField]
    int verticalRayCount = 4;

    float horizontalRaySpacing;
    float verticalRaySpacing;

    public LayerMask collisionMask;
    public CollisionInfo collisions;

    [Header("Slope options")]
    [SerializeField]
    float maxClimbAngle = 89f;
    [SerializeField]
    float maxDescendAngle = 75f;
    #endregion

    #region Moves Vars
    [HideInInspector]
    public bool jumping;
    [HideInInspector]
    public float CurrentYSpeedMaxClamp = 0f;

    [HideInInspector]
    public float calculatedJumpForce;
    [HideInInspector]
    public float calculatedGravity;
    #endregion

    #region Internal Components
    [HideInInspector]
    public Collider2D thisCollider;
    [HideInInspector]
    public Rigidbody2D thisRigidbody;
    [HideInInspector]
    public SpriteRenderer thisSprite;
    [HideInInspector]
    public Animator animator;
    #endregion

    #region external components
    [HideInInspector]
    public CharactersSharedVariables sharedVariables;

    #endregion

    #region Collisions Structs
    public struct RaycastOrigins
    {
        public Vector2 topLeft, topRight;
        public Vector2 bottomLeft, bottomRight;
    }

    public struct CollisionInfo
    {
        public bool above, below;
        public bool left, right;

        public bool climbingSlope;
        public bool descendingSlope;
        public float slopeAngle, slopeAnglePreviousTick;
        public Vector2 moveDirPreviousTick;

        public float highestContact;
        public int highestContactNumber;

        public void Reset()
        {
            above = below = false;
            left = right = false;

            climbingSlope = false;
            descendingSlope = false;

            slopeAnglePreviousTick = slopeAngle;
            highestContact = 0;
            highestContactNumber = 0;
            slopeAngle = 0;
        }
    }
    #endregion

    public float minGroundNormalY = .65f;
    public float gravityModifier = 1f;

    protected Vector2 targetVelocity;
    protected bool grounded;
    protected Vector2 groundNormal;
    protected Rigidbody2D rb2d;
    protected Vector2 velocity;
    protected ContactFilter2D contactFilter;
    protected RaycastHit2D[] hitBuffer = new RaycastHit2D[16];
    protected List<RaycastHit2D> hitBufferList = new List<RaycastHit2D>(16);

    protected const float minMoveDistance = 0.001f;
    protected const float shellRadius = 0.01f;

    void OnEnable()
    {
        rb2d = GetComponent<Rigidbody2D>();
    }

    // Use this for initialization
    void Start()
    {
        contactFilter.useTriggers = false;
        contactFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        contactFilter.useLayerMask = true;
    }

    // Update is called once per frame
    void Update()
    {
        targetVelocity = Vector2.zero;

        if (currentHealth > maxHealth)
            currentHealth = maxHealth;

        ComputeVelocity();
        Debug.Log("ComputeVelocity Called");
    }

    protected virtual void ComputeVelocity()
    {

    }

    void FixedUpdate()
    {
        velocity += gravityModifier * Physics2D.gravity * Time.deltaTime;
        velocity.x = targetVelocity.x;

        grounded = false;

        Vector2 deltaPosition = velocity * Time.deltaTime;

        Vector2 moveAlongGround = new Vector2(groundNormal.y, -groundNormal.x);

        Vector2 move = moveAlongGround * deltaPosition.x;

        Movement(move, false);

        move = Vector2.up * deltaPosition.y;

        Movement(move, true);
    }


    void Movement(Vector2 Move, bool yMovement)
    {
        float distance = Move.magnitude;

        if (distance > minMoveDistance)
        {
            int count = rb2d.Cast(Move, contactFilter, hitBuffer, distance + shellRadius);

            if (count > 0)
                Debug.Log("Preparing for impact...");

            hitBufferList.Clear();
            for (int i = 0; i < count; i++)
            {
                hitBufferList.Add(hitBuffer[i]);
            }

            //Collision points... ?
            for (int i = 0; i < hitBufferList.Count; i++)
            {
                Debug.DrawLine(transform.position, hitBufferList[i].point, Color.red);
                Vector2 currentNormal = hitBufferList[i].normal;
                if (currentNormal.y > minGroundNormalY)
                {
                    grounded = true;
                    if (yMovement)
                    {
                        groundNormal = currentNormal;
                        currentNormal.x = 0;
                    }
                }

                float projection = Vector2.Dot(velocity, currentNormal);
                if (projection < 0)
                {
                    velocity = velocity - projection * currentNormal;
                }

                float modifiedDistance = hitBufferList[i].distance - shellRadius;
                distance = modifiedDistance < distance ? modifiedDistance : distance;
            }

        }

        rb2d.position = rb2d.position + Move.normalized * distance;
        Debug.Log(transform.name + " just moved with this target velocity => " + targetVelocity);
    }

    // Use this for initialization
    void Awake()
    {
        //External stuff retrieving
        sharedVariables = GameObject.Find("CharactersManager").GetComponent<CharactersSharedVariables>();
        thisCollider = this.gameObject.GetComponent<Collider2D>();
        thisRigidbody = gameObject.GetComponent<Rigidbody2D>();
        thisSprite = gameObject.GetComponentInChildren<SpriteRenderer>();
        animator = gameObject.GetComponentInChildren<Animator>();

        if (sharedVariables == null)
            Debug.LogError("The scene is missing the CharacterSharedVariables class, please check your current GameObjects");
    }


}
