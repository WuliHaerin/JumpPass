using UnityEngine;
using System.Collections;

public class PlayerCamera : MonoBehaviour {

    public Transform Player;
    public float xOffset;
    public float followSpeed = 15;
    private Vector3 followPos;
    private Transform thisT;
    private Player playerControls;
    private Vector2 deadPoint;

    void Awake()
    {
        gameObject.tag = "MainCamera";
    }

	// Use this for initialization
	void Start () {
        if (!Player)
            Player = GameObject.FindGameObjectWithTag("Player").transform;

        thisT = transform;
        thisT.position = new Vector3(Player.position.x + xOffset, thisT.position.y, thisT.position.z);
        playerControls = Player.GetComponent<Player>();
        deadPoint = Player.position;
	}
	
	// Update is called once per frame
	void FixedUpdate () 
    {
        if (!Player || !Player.gameObject.activeSelf || Player.position.x < deadPoint.x)
            return;

        followPos = new Vector3(Player.position.x + xOffset, thisT.position.y, thisT.position.z);

        if (playerControls.IsGrounded())
            thisT.position = Vector3.Lerp(thisT.position, followPos, followSpeed * Time.deltaTime);
	}

    public void Reset()
    {
        thisT.position = new Vector3(Player.position.x + xOffset, thisT.position.y, thisT.position.z);
    }
}
