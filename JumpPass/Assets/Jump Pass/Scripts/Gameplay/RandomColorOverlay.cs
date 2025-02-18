using UnityEngine;
using System.Collections;

public class RandomColorOverlay : MonoBehaviour
{
    public MeshRenderer OverlayRenderer;    //Target renderer
    public int sortingOrder = 10;           //Sorting order
    private float alpha;

	void Start () 
    {
        alpha = OverlayRenderer.material.color.a;       //Cache default alpha;
        OverlayRenderer.sortingOrder = sortingOrder;              
	}
	
    //Set random color; This is a public dunction used by GameManager;
	public void SetRandomColorOverlay()
    {
        OverlayRenderer.material.color = new Color(Random.value, Random.value, Random.value, alpha);
    }
}
