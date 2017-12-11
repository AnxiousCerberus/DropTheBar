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
        ComputeVelocity();
    }

    protected virtual void ComputeVelocity()
    {
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;
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

        CalculateGravityAndJump();
    }

    void CalculateGravityAndJump()
    {
        calculatedGravity = -(2 * jumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        calculatedJumpForce = Mathf.Abs(calculatedGravity) * timeToJumpApex;
    }

    private void LateUpdate()
    {
    #if UNITY_EDITOR
        if (ImmediateTestMode)
            CalculateGravityAndJump();
    #endif
    }

    //Main Move Method, this will effectively translate the position of the character
    public void ApplyMoveAndCollisions(Vector2 a_moveDirection)
    {
        /* This can be useful if we're using Spine again... Meanwhile, let's just use regular sprites
         * 
        //Sprite flipping depending on direction
        //Used before collisions calculations to avoid the wall pushing changing Pauline's direction
        if (SpineSkeleton != null)
        {
            if (a_moveDirection.x < 0)
                SpineSkeleton.FlipX = true;
            else if (a_moveDirection.x > 0)
                SpineSkeleton.FlipX = false;
        }
        */

        //Setting up raycasts and collisions infos for this frame, starting from a blank slate
        UpdateRaycastOrigins();
        collisions.Reset();
        collisions.moveDirPreviousTick = a_moveDirection;

        //Slope check if we're going down, the method will automatically apply the slope move if a slope is detected
        if (a_moveDirection.y < 0)
        {
            DescendSlope(ref a_moveDirection);
        }

        //Checking and applying Horizontal Collisions if the character is moving on the X axis
        if (a_moveDirection.x != 0)
            CheckAndApplyHorizontalCollisions(ref a_moveDirection);

        //Checking vertical collisions if the character is moving on the Y axis
        //It'll call the APPLY Vertical Collisions Method on its own after some other checks
        if (a_moveDirection.y != 0)
            CheckVerticalCollisions(ref a_moveDirection);

        //Not jumping anymore if we're going down
        if (a_moveDirection.y <= 0 && jumping)
        {
            jumping = false;
        }

        //Debug.Log(transform.name +  " moveDir before Translate = " + a_moveDirection);

        //THAT'S IT, LET'S MOOOOVE \o/ =D
        transform.Translate(a_moveDirection);
    }

    #region Collision System Methods
    void ApplyVerticalCollision(ref Vector2 moveDirection, ref float directionY, ref float rayLength, ref RaycastHit2D hit)
    {
        moveDirection.y = (hit.distance - skinWidth) * directionY;
        rayLength = hit.distance;

        if (collisions.climbingSlope)
        {
            moveDirection.x = moveDirection.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(moveDirection.x);
        }

        collisions.below = directionY == -1;
        collisions.above = directionY == 1;
    }

    void CheckVerticalCollisions(ref Vector2 moveDirection)
    {
        float directionY = Mathf.Sign(moveDirection.y);
        float rayLength = Mathf.Abs(moveDirection.y) + skinWidth;

        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + moveDirection.x);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);
            Debug.DrawRay(rayOrigin, Vector3.up * directionY * rayLength, Color.red);

            if (hit.transform != null)
            {
                    ApplyVerticalCollision(ref moveDirection, ref directionY, ref rayLength, ref hit);
            }
        }

        if (collisions.climbingSlope)
        {
            float directionX = Mathf.Sign(moveDirection.x);
            rayLength = Mathf.Abs(moveDirection.x) + skinWidth;
            Vector2 rayOrigin = ((directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) + Vector2.up * moveDirection.y;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            if (hit.transform != null)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (slopeAngle != collisions.slopeAngle)
                {
                    moveDirection.x = (hit.distance - skinWidth) * directionX;
                    collisions.slopeAngle = slopeAngle;
                }
            }
        }
    }

    void CheckAndApplyHorizontalCollisions(ref Vector2 moveDirection)
    {
        float directionX = Mathf.Sign(moveDirection.x);
        float rayLength = Mathf.Abs(moveDirection.x) + skinWidth;

        if (Mathf.Abs(moveDirection.x) < skinWidth)
        {
            rayLength = 2 * skinWidth;
        }

        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector3.right * directionX, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector3.right * directionX * rayLength, Color.red);

            if (hit.transform != null)
            {
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);

                if (hit.point.y > collisions.highestContact)
                    collisions.highestContact = hit.point.y;

                if (i == 0 && slopeAngle <= maxClimbAngle)
                {
                    if (collisions.descendingSlope)
                    {
                        collisions.descendingSlope = false;
                        moveDirection = collisions.moveDirPreviousTick;
                    }
                    float distanceToSlopeStart = 0f;
                    if (slopeAngle != collisions.slopeAnglePreviousTick)
                    {
                        distanceToSlopeStart = hit.distance - skinWidth;
                        moveDirection.x -= distanceToSlopeStart * directionX;
                    }
                    ClimbSlope(ref moveDirection, slopeAngle);
                    moveDirection.x += distanceToSlopeStart * directionX;
                }

                if (!collisions.climbingSlope || slopeAngle > maxClimbAngle)
                {
                    moveDirection.x = (hit.distance - skinWidth) * directionX;
                    rayLength = hit.distance;

                    if (collisions.climbingSlope)
                    {
                        moveDirection.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveDirection.x);
                    }

                    collisions.left = directionX == -1;
                    collisions.right = directionX == 1;
                }
            }
        }
    }
    #endregion

    #region Slopes
    void ClimbSlope(ref Vector2 moveDirection, float a_slopeAngle)
    {
        float moveDistance = Mathf.Abs(moveDirection.x);
        float climbMoveDirectionY = Mathf.Sin(a_slopeAngle * Mathf.Deg2Rad) * moveDistance;

        if (moveDirection.y <= climbMoveDirectionY)
        {
            moveDirection.y = climbMoveDirectionY;
            moveDirection.x = Mathf.Cos(a_slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveDirection.x);
            collisions.below = true;
            collisions.climbingSlope = true;
            collisions.slopeAngle = a_slopeAngle;
        }
    }

    void DescendSlope(ref Vector2 moveDirection)
    {
        float directionX = Mathf.Sign(moveDirection.x);
        Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);

        if (hit.transform != null)
        {
            float descendSlopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            if (descendSlopeAngle != 0f && descendSlopeAngle <= maxDescendAngle)
            {
                if (Mathf.Sign(hit.normal.x) == directionX)
                {
                    if (hit.distance - skinWidth <= Mathf.Tan(descendSlopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveDirection.x))
                    {
                        float moveDistance = Mathf.Abs(moveDirection.x);
                        float descendDirectionY = Mathf.Sin(descendSlopeAngle * Mathf.Deg2Rad) * moveDistance;
                        moveDirection.x = Mathf.Cos(descendSlopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveDirection.x);
                        moveDirection.y -= descendDirectionY;

                        collisions.slopeAngle = descendSlopeAngle;
                        Debug.DrawRay(transform.position, moveDirection, Color.red);
                        collisions.descendingSlope = true;
                        collisions.below = true;
                    }
                }
            }
        }
    }
    #endregion

    #region Other Moves


    void Brakes()
    {
        //TODO : Brakes should be easier to do with the new smoothed out moves
    }
    #endregion

    #region Main Raycasts Methods
    public void UpdateRaycastOrigins()
    {
        Bounds bounds = thisCollider.bounds;
        bounds.Expand(skinWidth * -2);

        raycastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        raycastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
        raycastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        raycastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);

        //Debug.DrawLine(raycastOrigins.bottomLeft, raycastOrigins.topRight, Color.blue);
        //Debug.DrawLine(raycastOrigins.bottomRight, raycastOrigins.topLeft, Color.blue);
    }

    public void CalculateRaySpacing()
    {
        Bounds bounds = thisCollider.bounds;
        bounds.Expand(skinWidth * -2);

        horizontalRayCount = Mathf.Clamp(horizontalRayCount, 2, int.MaxValue);
        verticalRayCount = Mathf.Clamp(verticalRayCount, 2, int.MaxValue);

        horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
        verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
    }
    #endregion

}
