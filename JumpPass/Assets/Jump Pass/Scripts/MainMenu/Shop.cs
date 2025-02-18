using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class Shop : MonoBehaviour 
{
    public RectTransform contentHolder;
    public Text coinsText;                  //Coin display UI text;
    public TMP_Text priceInfo;                  //Price info display UI text;
    public Button unlockButton;             //Character unlock button;
    public Button leftArrow, rightArrow;    //Change characters shop buttons;
    public Scrollbar scrollBar;             //Scrollbar, we will use it for a scroll rect for characters shop;
    public AudioClip clickSfx;              //Click sound effect;
    public Characters[] characters;         //Characters list;

    private int coins;
    private float scrollStep, scrollValue;
    private int curCharacter;
    private AudioSource audioSource;

	void Start () 
    {
        audioSource = GetComponent<AudioSource>();
        //Add listeners;
        leftArrow.onClick.AddListener(() => Scroll(-1));
        rightArrow.onClick.AddListener(() => Scroll(1));
        unlockButton.onClick.AddListener(UnlockChar);
        //Calculate scrollstep for scrollbar;
        scrollStep = (float)1 / (characters.Length - 1);
        //Set scroll bar value;
        scrollBar.value = scrollStep * curCharacter;
        scrollBar.GetComponent<RectTransform>().pivot = Vector2.zero;

        //Set default selected character;
        if (!PlayerPrefs.HasKey("Char"))
            for (int i = 0; i < characters.Length; i++)
                if (characters[i].isSelected)
                    PlayerPrefs.SetString("Char", characters[i].characterPrefab.name);

        Load(); //Load saves;
        LoadCharactersPreview(); // Load characters preview images;
	}
	
    //Characters scroll function;
    public void Scroll(int dir)
    {
        Utilities.PlaySFX(audioSource, clickSfx, 1);
        curCharacter += dir;
    }

	void Update () 
    {
        //Link buttons interactable state to current character index;
        leftArrow.interactable = curCharacter > 0;
        rightArrow.interactable = curCharacter < characters.Length-1;

        //Clamp values;
        curCharacter = Mathf.Clamp(curCharacter, 0, characters.Length - 1);
        scrollValue = Mathf.Clamp01(scrollValue);
        //Calculate scroll value;
        scrollValue = scrollStep * curCharacter;
        //if scrollbar value isn't equal to scrollValue - move value;
        if (scrollBar.value != scrollValue)
            scrollBar.value = Mathf.MoveTowards(scrollBar.value, scrollValue, 0.03F);
        //Set character to selected if its free or has been purchased;
        for (int i = 0; i < characters.Length; i++)
            characters[i].isSelected = (i == curCharacter && (characters[i].purchased || characters[i].free));
        //Set price info UI text;     
        priceInfo.text = characters[curCharacter].free ? "免费" : characters[curCharacter].purchased ? "已解锁" : characters[curCharacter].Price.ToString();
        //Set price info text color;
        priceInfo.color = characters[curCharacter].priceColor;
        //Set coinsCoins to coins count;
        coinsText.text = coins.ToString();
        //Set unlock button active state depends on weather current character is purchased/free or not;
        if (!characters[curCharacter].free)
            unlockButton.gameObject.SetActive(!characters[curCharacter].purchased);
        else
            unlockButton.gameObject.SetActive(false);
        //Link unlock button interactable to coins coint and price;
        //unlockButton.interactable = (characters[curCharacter].Price <= coins);
	}

    void LoadCharactersPreview()
    {
        for(int i = 0; i < characters.Length; i++)
        {
            characters[i].characterPreview = (RectTransform)Instantiate(characters[i].characterPreview, Vector3.zero, Quaternion.identity);
            characters[i].characterPreview.SetParent(contentHolder, false);
        }
    }

    public GameObject AdPanel;
    //Unlock Character;
    void UnlockChar()
    {
        //If coins we have is bigger that current character price;
        if(coins >= characters[curCharacter].Price)
        {
            Utilities.PlaySFX(audioSource, clickSfx, 1);    //Play click sfx;
            coins -= characters[curCharacter].Price;        //Decrease coins count;
            characters[curCharacter].purchased = true;      //Set character state to purchased;
        }
        else if (characters[curCharacter].Price > coins)
        {
            AdPanel.SetActive(true);
        }
        //Save things;
        Save();
    }

    public void AdMoney()
    {
        AdManager.ShowVideoAd("192if3b93qo6991ed0",
           (bol) => {
               if (bol)
               {
                   coins += characters[curCharacter].Price / 2;
                   Save();
                   AdPanel.SetActive(false);

                   AdManager.clickid = "";
                   AdManager.getClickid();
                   AdManager.apiSend("game_addiction", AdManager.clickid);
                   AdManager.apiSend("lt_roi", AdManager.clickid);


               }
               else
               {
                   StarkSDKSpace.AndroidUIManager.ShowToast("观看完整视频才能获取奖励哦！");
               }
           },
           (it, str) => {
               Debug.LogError("Error->" + str);
               //AndroidUIManager.ShowToast("广告加载异常，请重新看广告！");
           });
      
    }

    //Load saves;
    void Load()
    {
        //Load coins;
        if (PlayerPrefs.HasKey("Coins"))
            coins = PlayerPrefs.GetInt("Coins");
        //Load Character stats;
        for (int i = 0; i < characters.Length; i++)
        {
            if (PlayerPrefs.HasKey(characters[i].characterPrefab.name + i + "s"))
                characters[i].isSelected = Utilities.GetBool(characters[i].characterPrefab.name + i + "s");
            if (PlayerPrefs.HasKey(characters[i].characterPrefab.name + i + "p"))
                characters[i].purchased = Utilities.GetBool(characters[i].characterPrefab.name + i + "p");

            if (characters[i].isSelected)
                curCharacter = i;
        }
    }

    //Save shop settings
   public void Save()
    {
       //Save character stats
        for (int i = 0; i < characters.Length; i++)
        {
            Utilities.SetBool(characters[i].characterPrefab.name + i + "s", characters[i].isSelected);
            Utilities.SetBool(characters[i].characterPrefab.name + i + "p", characters[i].purchased);

            if (characters[i].isSelected)
                PlayerPrefs.SetString("Char", characters[i].characterPrefab.name);
        }
       //Save coins
        PlayerPrefs.SetInt("Coins", coins);
    }

    //If the game can be started or not, based om weather current character is purchased/free or not;
    public bool CanStart()
    {
        return characters[curCharacter].purchased || characters[curCharacter].free;
    }
}

//Character class;
[System.Serializable]
public class Characters
{
    public RectTransform characterPreview;      //Prefab of character preview image; 
    public GameObject characterPrefab;          //Character prefab;
    public bool free;                           //If character is free;
    public bool purchased;                      //If character is purchased;
    public bool isSelected;                     //If character is selected;
    public int Price;                           //Character price;
    public Color priceColor = Color.white;      //Price text color;
}