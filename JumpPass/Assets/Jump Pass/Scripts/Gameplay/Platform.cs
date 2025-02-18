using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class Platform : MonoBehaviour 
{
    public enum MoveType                        //Platform movement types;
    {
        None,
        AroundPivot,
        PointsBased
    }
    public MoveType moveType = MoveType.None;
    public float moveRadius = 1;                //Move radius (if AroundPivot move type is selected);
    public Vector2 pointA;                      //Move point A (if PointsBased move type is selected));
    public Vector2 pointB;                      //Move point B;
    public float stopTime;                      //Moving platform pause time; 
    public float moveSpeed = 15;                //Platform move speed;
    public Transform Coin;                      //Coin object (if you want to place a coin on platform, make it child of platform and assign to this variable);
                                                //Coin object should have Collider2D component with isTrigger on;
    private Transform thisT;
    private Vector2 pivot;
    private Rigidbody2D rb2D;
    private Quaternion rotation;
    private float RandomAngle;
    private Vector2 RandomCirclePosition;
    private Vector2[] Points = new Vector2[2];
    public int curPoint;
    private float delay, time;
    private bool Rotate;
    private float pointsDistance;

    void OnEnable()
    {
        pivot = transform.position;             //Set pivot coordinates point to tmatch transform pivot position;

        if (moveType == MoveType.AroundPivot)   //If platform type is AroundPivot, put platform to a random point within radius;
            SetRandonPosition();

        //Set move points;
        Points[0] = (Vector2)pivot + pointA;            
        Points[1] = (Vector2)pivot + pointB;
        gameObject.tag = "Finish";
    }

    void Start()
    {
        //Cache components
        thisT = GetComponent<Transform>();
        rb2D = GetComponent<Rigidbody2D>();
        rb2D.isKinematic = true;
    }

    void FixedUpdate()
    {
        //Switch between platform move types;
        switch (moveType)
        {
            case MoveType.AroundPivot:
                RotateAroundPoint(rb2D, pivot, thisT.forward, moveSpeed);
                break;
            case MoveType.PointsBased:
                MoveBetweenPoints(Points, moveSpeed);
                break;
            case MoveType.None:
                break;
        }
    }

    //Rotate around pivot function;
    public void RotateAroundPoint(Rigidbody2D rb, Vector3 origin, Vector3 axis, float speed)
    {
        rotation = Quaternion.AngleAxis(speed * 10 * Time.fixedDeltaTime, axis);
        rb.MovePosition(rotation * ((Vector3)rb.position - origin) + origin);
    }

    //Move between points function;
    public void MoveBetweenPoints(Vector2[] points, float speed)
    {
        pointsDistance = Mathf.Abs((points[curPoint] - rb2D.position).magnitude);

        if (pointsDistance < 0.01F)
        {
            time = stopTime;
            if (curPoint == points.Length - 1)
                curPoint = 0;
            else
                curPoint++;

            delay = Time.time;
        }
        else
            if (Time.time > delay + time)
                thisT.position = Vector2.MoveTowards(transform.position, points[curPoint], speed / 10 * Time.deltaTime);
    }

    //Set random position, getting random point on radius circle with RandomOnCircle function;
    private void SetRandonPosition()
    {
        transform.position = RandomOnCircle(transform.position, moveRadius);
    }

    private Vector2 RandomOnCircle(Vector2 center, float radius)
    {
        RandomAngle = Random.value * 360;
        RandomCirclePosition.x = center.x + radius * Mathf.Sin(RandomAngle * Mathf.Deg2Rad);
        RandomCirclePosition.y = center.y + radius * Mathf.Cos(RandomAngle * Mathf.Deg2Rad);
        return RandomCirclePosition; 
    }

    //Inverse start point for moving type platform. Using by level generation script to avoid 2 platforms in a row to move in same direction;
    public void InverseStartPoint()
    {
        if (curPoint == Points.Length - 1)
            curPoint = 0;
        else
            curPoint++;
    }

    //Same as InverseStartPoint, but for AroundPivot type of platforms;
    public void InverseSpeed()
    {
        moveSpeed = -moveSpeed;
    }

    //Reset platform. using by level generator;
    public void ResetPlatform()
    {
        curPoint = 0;                                   //Reset moving platform current point;
        delay = 0;                                      //Reset moving platform pause time;
        GetComponent<BoxCollider2D>().enabled = true;   //Enable platform's collider because it can be disabled by player (OneTimePlatfotm toogle);
        pivot = transform.position;                     //Reset pivot pos;
        Points[0] = (Vector2)pivot + pointA;            //Reset move points;
        Points[1] = (Vector2)pivot + pointB;

        if (moveType == MoveType.AroundPivot)           //Set random pos on a circle again, if platform is AroundPivot;
            SetRandonPosition();

        if (Coin)                                       //Enable coin object because it goes to disable state when collected;
            Coin.gameObject.SetActive(true);
    }
    
    public void OverrideSpeed(float speed, float waitTime = 0)
    {
        moveSpeed = moveSpeed > 0 ? speed : -speed;
        stopTime = waitTime;
    }

    //Some scene gizmo graphics;
    void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            switch (moveType)
            {
                case MoveType.AroundPivot:
                    Utilities.DrawRadius(pivot, 0.05F, Color.green, moveRadius, Color.white);
                    break;
                case MoveType.PointsBased:
                    UnityEditor.Handles.DrawWireDisc((Vector2)transform.position + pointA, transform.forward, 0.5F);
                    UnityEditor.Handles.DrawWireDisc((Vector2)transform.position + pointB, transform.forward, 0.5F);
                    UnityEditor.Handles.DrawDottedLine(Points[0], Points[1], 5);

                    Vector3 posA = (Vector2)transform.position + pointA;
                    Utilities.SceneLabel(posA, Vector2.zero, 55, 100, 100, "A", 20, FontStyle.Normal, Color.green);
                    Vector3 posB = (Vector2)transform.position + pointB;
                    Utilities.SceneLabel(posB, Vector2.zero, 55, 100, 100, "B", 20, FontStyle.Normal, Color.green);
                    break;
                case MoveType.None:
                    break;
            }
        }
        else
        {
            switch (moveType)
            {
                case MoveType.AroundPivot:
                    Utilities.DrawRadius(pivot, 0.05F, Color.green, moveRadius, Color.white);
                    break;
                case MoveType.PointsBased:
                    UnityEditor.Handles.DrawWireDisc((Vector2)pivot + pointA, transform.forward, 0.5F);
                    UnityEditor.Handles.DrawWireDisc((Vector2)pivot + pointB, transform.forward, 0.5F);
                    UnityEditor.Handles.DrawDottedLine((Vector2)pivot + pointA, (Vector2)pivot + pointB, 5);

                    Vector3 posA = (Vector2)pivot + pointA;
                    Utilities.SceneLabel(posA, Vector2.zero, 55, 100, 100, "A", 20, FontStyle.Normal, Color.green);
                    Vector3 posB = (Vector2)pivot + pointB;
                    Utilities.SceneLabel(posB, Vector2.zero, 55, 100, 100, "B", 20, FontStyle.Normal, Color.green);
                    break;
                case MoveType.None:
                    break;
            }
        }
#endif
    }
}
