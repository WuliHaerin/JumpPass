using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(AudioSource))]
public class MainMenu : MonoBehaviour 
{
    [Tooltip("Game level index (See in Build Settings)")]
    public int GameLevelIndex = 1;              //Game level index (See in Build Settings)
    [Tooltip("Start game UI button")]
    public Button playButton;                   //Start game UI button;
    [Tooltip("Quit game UI button")]
    public Button quitButton;                   //Quit game UI button;
    [Tooltip("Score text for showing player's best score")]
    public TMP_Text scoreText;                      //Score text for showing player's best score;
    [Tooltip("Buttons click sound effect")]
    public AudioClip clickSfx;                  //Buttons click sound effect;
    [Tooltip("Background music settings")]
    public Ambience ambience;                   //Background music;
    [Tooltip("Loading settings")]
    public Loading loading;

    private AudioSource audioSource;
    private Shop shop;
    private AsyncOperation async = null;
    private CanvasGroup fadeGroup;

    private void Awake()
    {

        // 设置屏幕方向为竖屏
        Screen.orientation = ScreenOrientation.Portrait;

        // 可选：锁定屏幕方向，防止自动旋转
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = true;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
        Screen.orientation = ScreenOrientation.AutoRotation;
    }
    void Start () 
    {
        //Cache components;
        audioSource = GetComponent<AudioSource>();  
        shop = GetComponent<Shop>();
        //Add listeners to the buttons;
        playButton.onClick.AddListener(LoadGame);   
        quitButton.onClick.AddListener(QuitGame);

        //Load best score
        if (PlayerPrefs.HasKey("Best"))
            scoreText.text = "最佳：" + PlayerPrefs.GetInt("Best").ToString();
        else
            scoreText.text = "";
        //Disable loading screen;
        loading.LoadingScreen.SetActive(false);
       
        Utilities.PlaySFX(ambience.ambienceSource, ambience.musicLoop, 0.5F, true);
        //Add canvas group to the fade image;
        fadeGroup = loading.fadeOutImage.gameObject.AddComponent<CanvasGroup>();
        fadeGroup.blocksRaycasts = false;
        fadeGroup.alpha = 1; // set fade image alpha to 1 at start;
	}

    void Update()
    {
        //Set mobile escape button to quit a game
        if (Input.GetKey(KeyCode.Escape))
            Application.Quit();
        //Set the start game button interactivity. Based on shop function, which returns true if selected character is purchased or free and false if not;
        playButton.interactable = shop.CanStart();
        //Show loading progress;
        if(async != null)
            loading.loadingProgress.text = Mathf.FloorToInt(async.progress * 100) + "%";
        //Fade out
        if (fadeGroup.alpha > 0)
            fadeGroup.alpha = Mathf.MoveTowards(fadeGroup.alpha, 0, loading.fadeSpeed * Time.fixedDeltaTime);

        loading.fadeOutImage.gameObject.SetActive(fadeGroup.alpha > 0);
    }

    //Loading coroutine
    private IEnumerator Loading()
    {
        async = Application.LoadLevelAsync(GameLevelIndex);
        yield return async;
    }

    //Load game
    void LoadGame()
    {
        ambience.ambienceSource.Stop();                                 //Stop background music
        shop.Save();                                                    //Save shop changes (characters/coins);
        Utilities.PlaySFX(audioSource, clickSfx, 1);                    //Play click sound;
        loading.LoadingScreen.SetActive(loading.drawLoadingscreen);     //Enable loading screen;
        StartCoroutine("Loading");                                      //Start Loading coroutine;
    }

    //Quit game;
    void QuitGame()
    {
        shop.Save();                                
        Utilities.PlaySFX(audioSource, clickSfx, 1);
        Application.Quit();
    }
}
