using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//Sebastian Lague on Youtube, kudos to his 2D CharController tuto!
public class Characters : MonoBehaviour
{
    [Header("Common character vars")]
    public int maxHealth = 4;
    public int currentHealth = 4;

    #region Basic Moves Inspector Variables
    [Header("Basic moves options")]
    public float speed = 10;
    #endregion

    #region Character Collision System
    [Header("Collision Detection Options")]
    protected const float shellRadius = 0.01f;
    #endregion

    #region external components
    [HideInInspector]
    public CharactersSharedVariables sharedVariables;
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

        groundNormal = Vector2.one;
        velocity = Vector2.zero;
    }

    // Update is called once per frame
    void Update()
    {
        targetVelocity = Vector2.zero;


        ComputeVelocity();
        Debug.Log("ComputeVelocity Called");
    }

    protected virtual void ComputeVelocity()
    {

    }

    void FixedUpdate()
    {
        //velocity += gravityModifier * Physics2D.gravity * Time.deltaTime;
        velocity.x = targetVelocity.x;

        grounded = false;

        Vector2 deltaPosition = velocity * Time.deltaTime;
        Debug.Log("Delta Time = " + Time.deltaTime + "Velocity used for deltaPosition = " + velocity + "Resulting in = " + deltaPosition.x);


        Vector2 moveAlongGround = new Vector2(groundNormal.y, -groundNormal.x);

        Vector2 move = moveAlongGround * deltaPosition.x;

        Movement(move, false);

        move = Vector2.up * deltaPosition.y;

        Movement(move, true);

        Debug.Log("Ground Normal = " + groundNormal + "Target velocity = " + targetVelocity.x + " Current Velocity = " + velocity.x + " Delta Position = " + deltaPosition.x + " moveAlongGround = " + moveAlongGround.x + " Move = " + move.x);
    }


    void Movement(Vector2 Move, bool yMovement)
    {
        Debug.Log("Move at Start is = " + Move.x);
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

        //TODO : This is experimental as fuck


        rb2d.position = rb2d.position + Move.normalized * distance;
        Debug.Log(transform.name + " just moved with this target velocity => " + targetVelocity);
        Debug.Log("END rb2d position =" + rb2d.position + " Move normalized = " + Move.normalized + " Distance = " + distance);
    }

    // Use this for initialization
    void Awake()
    {
        //External stuff retrieving
        sharedVariables = GameObject.Find("CharactersManager").GetComponent<CharactersSharedVariables>();

        if (sharedVariables == null)
            Debug.LogError("The scene is missing the CharacterSharedVariables class, please check your current GameObjects");
    }


}
