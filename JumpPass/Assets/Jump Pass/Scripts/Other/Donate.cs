using UnityEngine;
using System.Collections;

public class Donate : MonoBehaviour
{
    public int Coins;                   //How much coins to add;
    private int currentCoins;           //The coins we already have;

    public void DonateCoins()
    {
        currentCoins = PlayerPrefs.GetInt("Coins");     //Get coinsm if we already have some.
        currentCoins += Coins;                          //Increase coins count;
        PlayerPrefs.SetInt("Coins", currentCoins);      //Save new coins count;
    }
}
