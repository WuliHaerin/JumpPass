using UnityEngine;
using System.Collections;

public class Rotator2D : MonoBehaviour 
{

    public float rotationSpeed = 25; //Roation speed;
	private Transform thisT;
    
    void Start()
    {
        thisT = transform;  //Cache transform;
    }

	void FixedUpdate () 
    {
        //Rotate 2D object;
        thisT.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
	}
}
