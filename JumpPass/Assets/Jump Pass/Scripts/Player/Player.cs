using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(AudioSource))]
public class Player : MonoBehaviour
{
    public AudioClip jumpSfx;           //Jump sound effect;
    public Animator animator;           //Animator component;
    public Trajectory trajectory;       //Trajectory class;
    public bool OneTimePlatfotm;        //One time platform, decides if player can jump on one platform twice;

    private Vector3 StartPos;
    private Vector3 dir;
    private Vector3 finalForceDir;
    private Vector3 screenPos;
    private Vector2 defaultSize, sizeDelta;
    private Vector2 platformTop, playerBot;
    private float distance;
    private float sizeFactor;
    private float time;
    private float collisionOffset;
    private int pointsCount;
    private GameObject holder;
    private GameObject curPlatform, passedPlatform;
    private bool grounded;
    private bool isDead;
    private bool prepare;
    private GameManager GM;
    private Canvas UICanvas;
    private CanvasGroup canvasGroup;
    private RectTransform[] AllPoints;
    private Vector3[] trajectoryPoints;
    private Vector2[] pointSize;
    private AudioSource audioSource;
    private Rigidbody2D rb2D;
    private BoxCollider2D boxCollider;
    private Transform thisT;

    void Awake()
    {
        gameObject.tag = "Player";      //Setting player tag;
    }

    void Start()
    {
        //Cache components;
        boxCollider = GetComponent<BoxCollider2D>();
        rb2D = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        GM = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();
        UICanvas = GameObject.FindObjectOfType<Canvas>();
        holder = new GameObject("TrajectoryHolder", typeof(RectTransform), typeof(CanvasGroup));
        holder.transform.SetParent(UICanvas.transform, false);
        canvasGroup = holder.GetComponent<CanvasGroup>();
        thisT = transform;

        //Set default values;
        defaultSize = trajectory.pointTransform.sizeDelta;
        canvasGroup.alpha = 0;
        pointsCount = Mathf.FloorToInt(trajectory.trajectoryDuration / trajectory.pointsDensity)-1;
        AllPoints = new RectTransform[pointsCount];
        trajectoryPoints = new Vector3[pointsCount + 1];
        pointSize = new Vector2[pointsCount + 1];

        //Instatiate trajectory points;
        for (int i = 0; i < pointsCount; i++)
        {
            AllPoints[i] = (RectTransform)Instantiate(trajectory.pointTransform, Vector3.zero, Quaternion.identity);
            AllPoints[i].SetParent(holder.transform, false);
        }
    }

    void Update()
    {
        //If not grounded, do nothing;
        if (!grounded)
            return;
        //Apply input controls;
        ApplyControls();
        //If Animator object is null, do nothing,
        if (!animator)
            return;
        //Else, set Animator parameters;
        animator.SetBool("Prepare", prepare);
        animator.SetBool("Grounded", grounded);
    }

    void ApplyControls()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StartPos = Input.mousePosition;         //Cache start touch position;
            prepare = true;                         //Set jump prepare to true;
        }
        else if (Input.GetMouseButtonUp(0) && prepare)  //If touch ended and prepare is true;
        {       
            Jump();                                     //Execute jump;
            prepare = false;                            //Set prepare to false;
        }
        else if (prepare && Input.GetMouseButtonDown(1))//If prepare is true and second touch was done,
        {
            canvasGroup.alpha = 0;                      //Hide trahectory;
            prepare = false;                            //Set prepare to false;
        }
        else if (prepare)                               //If prepare is true, execute jump prepare.
            PrepareJump();
    }

    void PrepareJump()
    {
        dir = StartPos - Input.mousePosition;                                                   //Calculate direction based on start touch position and current touch;
        distance = dir.magnitude;                                                               //Distance between start and currnt touch;
        finalForceDir = new Vector3(dir.x, dir.y, 0) * distance / 7500;                         //Final force;
        DrawTrajectory();               //Draw jump trajectory;                                                                   
        canvasGroup.alpha = 0.8F;       //Set jump trajectory alpha;
    }

    void Jump()
    {
        thisT.parent = null;                                //Detach player from any platform;
        rb2D.isKinematic = false;                           //Enable rigidbody;
        rb2D.velocity = Vector3.zero;                       //Reset velocity just before we adding force to avoid bugs;
        rb2D.AddForce(finalForceDir, ForceMode2D.Impulse);  //Add jump force;
        distance = 0;                                       //Reset distance between touches;
        canvasGroup.alpha = 0;                              //Set trajectory alpha to zero, becouse we wont see it anymore;

        //Check if final force is enough for a minimum jump and if so,
        if (finalForceDir.y > 0.5F)
        {
            Utilities.PlaySFX(audioSource, jumpSfx, 1);             //Play jump sound;
            grounded = false;                                       //Set grounded to false;
            //if (OneTimePlatfotm && passedPlatform != null)          //If OneTimePlatfotm is true, disable current platform's collider;
            //    passedPlatform.GetComponent<Collider2D>().enabled = false;
        }
    }

    //Draw trajectory function;
    public void DrawTrajectory()
    {
        //Build trajectory, this function will fill our points coordinates array;
        BuildTrajectory(rb2D.position, finalForceDir, trajectory.pointsDensity, trajectory.trajectoryDuration);

        //Position trajectory points with positions;
        for (int i = 0; i < trajectoryPoints.Length - 1; i++)
        {
            AllPoints[i].position = trajectoryPoints[i];    //Set position
            AllPoints[i].up = trajectoryPoints[i + 1] - AllPoints[i].position;                           //Set rotation;
            AllPoints[i].sizeDelta = pointSize[i + 1];                                                   //Set size;              
        }
    }

    //Calculate trajectory point;
    public Vector3 CalculateTrajectoryPoint(Vector3 start, Vector3 startVelocity, float time)
    {
        return start + startVelocity * time + Physics.gravity * time * time * 0.5f;
    }

    //Build trajectory;
    public void BuildTrajectory(Vector3 start, Vector3 startVelocity, float dencity, float duration)
    {
        for (int i = 1; ; i++)
        {
            time = dencity * i;
            if (time > duration) break;
            //Get trajectory point coordinate;
            screenPos = Camera.main.WorldToScreenPoint(CalculateTrajectoryPoint(start, startVelocity, time));
            sizeFactor = ((1 / dencity * 2) - i) * 2; //Calculate size factor;
            sizeDelta = new Vector2(defaultSize.x + sizeFactor, defaultSize.y + sizeFactor);    //Calculate size delta, base on default point size and size factor;
            trajectoryPoints[i - 1] = screenPos;    //Fill trajectory points array;
            pointSize[i - 1] = sizeDelta;           //Fill trajectory point sizes array;
        }

        
    }

    //When collision is happens
    void OnCollisionEnter2D(Collision2D col)
    {
        curPlatform = col.gameObject;           //Set currentPlatform to collision object;
        //Calculate collision offset base on player collider min bot point and current platform collider max top point;
        collisionOffset = Mathf.Abs(boxCollider.bounds.min.y - curPlatform.GetComponent<BoxCollider2D>().bounds.max.y);
        //Check if collision offset is small enough for grounded and other purposes;
        if (collisionOffset <= 0.05F)
        {
            transform.parent = curPlatform.transform;       //Set current platform as parent object;
            grounded = true;                                //Set grounded to true;

            //Check is platform is not passed and its tag is Platform;
            if (col.gameObject != passedPlatform && col.gameObject.CompareTag("Finish"))
            {
                GM.AddScore();                      //Add score;
                rb2D.velocity = Vector2.zero;       //Reset velocity;
                rb2D.isKinematic = true;            //Disable rigidbody;
                if(passedPlatform!=null)
                {
                    passedPlatform.GetComponent<Collider2D>().enabled = false;
                }
                passedPlatform = col.gameObject;    //Set platform to passed;
            }
        }
    }

    public void Revive()
    {
        if(passedPlatform!=null)
        {
            transform.parent = passedPlatform.transform;
            transform.localPosition = new Vector3(0, 0.6f, 0);
            rb2D.velocity = new Vector2(0, 0);
            //rb2D.isKinematic = true;            //Disable rigidbody;
            grounded = true;
        }
        else
        {
            transform.position =GM.playerSettings.spawnPosition.position;
            rb2D.velocity = new Vector2(0, 0);
            //rb2D.isKinematic = true;            //Disable rigidbody;
            grounded = true;
        }
    }

    public bool IsGrounded()
    {
        return grounded;
    }

    //Death function;
    public void Death()
    {
        thisT.parent = null;            //Detatch parent object;
        rb2D.velocity = Vector2.zero;   //Reset velocity;
        gameObject.SetActive(false);    //Disable player object;
    }

    public void SetAlive()
    {
        passedPlatform = null;          //Reset passed platform;
        gameObject.SetActive(true);     //Enable player object;
    }
}

[System.Serializable]
public class Trajectory
{
    public RectTransform pointTransform;    //Trajectory point rectTransform (UI image);
    public float pointsDensity = 0.15F;     //Points density, 
    public float trajectoryDuration = 1.5F; //Trajectory duration;
}
