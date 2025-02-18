using UnityEngine;
using System.Collections;

public class Paralax : MonoBehaviour 
{
    public Backgrounds[] backgrounds;
    private Transform camT;
    private Vector3 origin;
    private float offset;
	// Use this for initialization
	void Start () 
    {
        camT = Camera.main.transform;
        origin = camT.position;
	}
	
	// Update is called once per frame
	void Update () 
    {
        offset = (origin - camT.position).sqrMagnitude / 100;
        for (int i = 0; i < backgrounds.Length; i++)
            backgrounds[i].background.material.mainTextureOffset = new Vector2(offset * backgrounds[i].scrollSpeed / 100, 0);
	}
}


[System.Serializable]
public class Backgrounds
{
    public MeshRenderer background;
    public float scrollSpeed = 1;
}
