using UnityEngine;
using System.Collections;

public class CoinCollector : MonoBehaviour 
{
    private GameManager GM;     //GameManager script;

	void Start () 
    {
        //Cache GameManager script;
        GM = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();
	}
	
    //As only one trigger in the game is coin, use OnTriggerEnter to collect it;
	void OnTriggerEnter2D (Collider2D col) 
    {
        GM.DrawCoinAtPosition(col.transform.position);  //Draw pickup effect;
        col.gameObject.SetActive(false);                //Disable coin object;
	}
}
