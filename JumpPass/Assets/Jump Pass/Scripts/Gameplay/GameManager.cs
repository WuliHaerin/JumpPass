using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class GameManager : MonoBehaviour 
{
    public LevelSettings levelSettings;             //Level settings class;
    public PlayerSettings playerSettings;           //Player settings class; 
    public ScoreSettings scoreSettings;             //Score settings class;
    public DeathEffect deathEffect;                 //Death effect settings class;
    public GameOverSettings gameOverSettings;       //Game over settings class;
    public Loading loading;                         //Loading settings class;
    public Ambience ambience;

    private int coinsCount;
    private int defaultCoins;
    private int score;
    private int bestScore = 0;
    private AudioSource audioSource;
    private Player playerControls;
    private Camera cam;
    private PlayerCamera playerCam;
    private Transform camT;
    private BoxCollider2D playerCollider;
    private Plane[] planes;
    private CanvasGroup deathAlpha;
    private bool gameOver;
    private Vector2 gameOverHidePos;
    private Transform Player;
    private Vector2 newPos;
    private float platformDiff;
    private List<PreloadedPlatforms> preloadedPlatforms = new List<PreloadedPlatforms>();
    private List<PreloadedPlatforms> passedPlatforms = new List<PreloadedPlatforms>();
    private List<PreloadedPlatforms> allPlatforms = new List<PreloadedPlatforms>();
    private Platform prevPlatform, curPlatform, lastPlatform;
    private RandomColorOverlay rco;
    private Canvas UICanvas;
    private Text coinsInfoText;
    private CanvasGroup fadeGroup;
    private AsyncOperation async = null;

    void Awake()
    {
        Screen.orientation = ScreenOrientation.LandscapeRight;
        Screen.orientation = ScreenOrientation.AutoRotation;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        //Set object tag;
        gameObject.tag = "GameController";
        //Instatiate player;
        for (int i = 0; i < playerSettings.playerPrefabs.Length; i++) // loop throught player prefabs array;
        {
            //If player prefab name equal to saved selected player, instantiate this prefab;
            if (playerSettings.playerPrefabs[i].name == PlayerPrefs.GetString("Char"))
                Player = (Transform)Instantiate(playerSettings.playerPrefabs[i], playerSettings.spawnPosition.position, Quaternion.identity);
        }
    }

	// Use this for initialization
	void Start () {
        //Cache components;
        audioSource = GetComponent<AudioSource>();
        playerCollider = Player.GetComponent<BoxCollider2D>();
        playerControls = Player.GetComponent<Player>();
        cam = Camera.main;
        camT = cam.transform;
        rco = cam.GetComponent<RandomColorOverlay>();
        playerCam = cam.GetComponent<PlayerCamera>();
        UICanvas = GameObject.FindObjectOfType<Canvas>();
        fadeGroup = loading.fadeOutImage.gameObject.AddComponent<CanvasGroup>();
        fadeGroup.alpha = 1; // set fade image alpha to 1 at start;

        //Instatiate coin prefab, we will use it to visualize coin collection;
        scoreSettings.coinPrefab = (RectTransform)Instantiate(scoreSettings.coinPrefab, Vector3.zero, Quaternion.identity);
        scoreSettings.coinPrefab.SetParent(UICanvas.transform);
        scoreSettings.coinPrefab.gameObject.SetActive(false);
        //Get coins info text component. Must be a default Text object, child of CoinsInfo transform;
        coinsInfoText = scoreSettings.CoinsInfo.transform.Find("Text").GetComponent<Text>();

        LoadCoinsCount();   //Load coins;
        LoadBestScore();    //Load best score;  
        UpdateScore();      //Update score info;

        //Add listeners to the buttons;
        gameOverSettings.restartButton.onClick.AddListener(Restart);
        gameOverSettings.menuButton.onClick.AddListener(LoadMenu);

        //Set game over panel hide postion to its default position out of screen;
        gameOverHidePos = gameOverSettings.gameOverPanel.anchoredPosition;
        //Instatiate level platforms;
        InstatiatePlatforms();

        //if death effect image is null do nothing
        if (!deathEffect.effectImage)
            return;
        //else add a canvas group component to control alpha;
        deathAlpha = deathEffect.effectImage.gameObject.AddComponent<CanvasGroup>();
        deathAlpha.alpha = 0;
        //Play background music;
        Utilities.PlaySFX(ambience.ambienceSource, ambience.musicLoop, 0.3F, true);
	}

    void FixedUpdate()
    {
        //if death effect alpha is bigger than zero, decrease it to zero;
        if (deathAlpha && deathAlpha.alpha > 0)
            deathAlpha.alpha = Mathf.MoveTowards(deathAlpha.alpha, 0, deathEffect.effectSpeed * Time.deltaTime);
        //if score settings, coin prefab is enabled, move it up to the Coins info transfrom position;
        if (scoreSettings.coinPrefab.gameObject.activeSelf)
            scoreSettings.coinPrefab.position = Vector2.MoveTowards(scoreSettings.coinPrefab.position, scoreSettings.CoinsInfo.position, 25);
        //Fade out: if fade image alpha is bigger than zero, decrease it;
        if (fadeGroup.alpha > 0)
            fadeGroup.alpha = Mathf.MoveTowards(fadeGroup.alpha, 0, loading.fadeSpeed * Time.deltaTime);
    }
	
	void Update () 
    {
        //Set mobile escape button to load menu;
        if (Input.GetKey(KeyCode.Escape))
            LoadMenu();
        //Link fade image active state to fade alpha; If fade alpha bigger than zero, enable, else - disable;
        loading.fadeOutImage.gameObject.SetActive(fadeGroup.alpha > 0);
        //Display score;
        scoreSettings.ScoreInfo.text = score.ToString();
        //Display coins count;
        coinsInfoText.text = coinsCount.ToString();
        //Check coins is basicaly an add coins logic;
        CheckCoins();
        //Display loading progress;
        if (async != null)
            loading.loadingProgress.text = Mathf.FloorToInt(async.progress * 100) + "%";
        //Calculate camera frustum;
        planes = GeometryUtility.CalculateFrustumPlanes(cam);
        //If player is out of camera frustum, call KillPlayer function;
        if (OutOfView(playerCollider) && !isCancelAd)
            PreDie();
        //Link death image active state to fade alpha; If fade alpha bigger than zero, enable, else - disable;
        if (deathEffect.effectImage)
            deathEffect.effectImage.gameObject.SetActive(deathAlpha.alpha > 0);
        //Link gameOverPanel active state to gameOver flag;
        gameOverSettings.gameOverPanel.gameObject.SetActive(gameOver);
        //Link scoreInfo active state to opposite gameOver flag;
        scoreSettings.ScoreInfo.enabled = !gameOver;
        //Show/Hide game over menu;
        if (gameOver)
        {
            gameOverSettings.gameOverPanel.anchoredPosition = Vector2.MoveTowards(gameOverSettings.gameOverPanel.anchoredPosition, gameOverSettings.normalPosition, 35.0F);
        }
        else
            gameOverSettings.gameOverPanel.anchoredPosition = gameOverHidePos;
        //If platform is passed, move it to the passed platform array list;
        for(int i = 0; i < preloadedPlatforms.Count; i++)
        {
            if(preloadedPlatforms[i].platformTransform.position.x < camT.position.x && OutOfView(preloadedPlatforms[i].platformCollider))
            {
                preloadedPlatforms[i].platformCollider.enabled = true;
                passedPlatforms.Add(preloadedPlatforms[i]);
                preloadedPlatforms.RemoveAt(i);
            }
        }
        //If passed platforms count is bigger than preloaded platforms count
        if (passedPlatforms.Count > preloadedPlatforms.Count)
        {
            //Shuffle passed platforms array;
            passedPlatforms.Shuffle();
            for (int i = 0; i < passedPlatforms.Count; i++)
            {
                //Adjust position offset based on platform type;
                if (lastPlatform.moveType == Platform.MoveType.PointsBased && passedPlatforms[i].platform.moveType == Platform.MoveType.PointsBased)
                    newPos.x += Mathf.Abs(lastPlatform.pointA.x) + Mathf.Abs(passedPlatforms[i].platform.pointB.x);
                else if (lastPlatform.moveType == Platform.MoveType.None && passedPlatforms[i].platform.moveType == Platform.MoveType.PointsBased)
                    newPos.x += Mathf.Abs(passedPlatforms[i].platform.pointA.x);
                else if (lastPlatform.moveType == Platform.MoveType.PointsBased && passedPlatforms[i].platform.moveType == Platform.MoveType.None)
                    newPos.x += Mathf.Abs(lastPlatform.pointB.x);
                else if (lastPlatform.moveType == Platform.MoveType.AroundPivot && passedPlatforms[i].platform.moveType == Platform.MoveType.AroundPivot)
                    newPos.x += Mathf.Abs(lastPlatform.moveRadius) + Mathf.Abs(passedPlatforms[i].platform.moveRadius);
                else if (lastPlatform.moveType == Platform.MoveType.None && passedPlatforms[i].platform.moveType == Platform.MoveType.AroundPivot)
                    newPos.x += Mathf.Abs(passedPlatforms[i].platform.moveRadius);
                else if (lastPlatform.moveType == Platform.MoveType.AroundPivot && passedPlatforms[i].platform.moveType == Platform.MoveType.None)
                    newPos.x += Mathf.Abs(lastPlatform.moveRadius);
                else if (lastPlatform.moveType == Platform.MoveType.PointsBased && passedPlatforms[i].platform.moveType == Platform.MoveType.AroundPivot)
                    newPos.x += Mathf.Abs(lastPlatform.pointB.x) + Mathf.Abs(passedPlatforms[i].platform.moveRadius);
                else if (lastPlatform.moveType == Platform.MoveType.AroundPivot && passedPlatforms[i].platform.moveType == Platform.MoveType.PointsBased)
                    newPos.x += Mathf.Abs(lastPlatform.moveRadius) + Mathf.Abs(passedPlatforms[i].platform.pointA.x);
                //Reposition platform; move it forward;
                passedPlatforms[i].platformTransform.position = new Vector3(levelSettings.LevelStartPoint.position.x + newPos.x,
                Random.Range(passedPlatforms[i].minPos.y, passedPlatforms[i].maxPos.y));
                //After platform is on its new position, reset it;
                passedPlatforms[i].platform.ResetPlatform();
                //Add platform min/max position to the next position;
                newPos.x += Random.Range(passedPlatforms[i].minPos.x, passedPlatforms[i].maxPos.x);
                lastPlatform = passedPlatforms[i].platform;
                //Move platform to preloaded platforms array list;
                preloadedPlatforms.Add(passedPlatforms[i]);
                passedPlatforms.RemoveAt(i);
            }
        }
        //Increase dificulty
        if (levelSettings.difficulties.Length != 0)
        {
            for (int a = 0; a < levelSettings.difficulties.Length; a++)
            {
                if (score >= levelSettings.difficulties[a].scoreAmount && !levelSettings.difficulties[a].reached)
                {
                    scoreSettings.ScoreInfo.color = levelSettings.difficulties[a].scoreTextColor;
                    for (int p = 0; p < allPlatforms.Count; p++)
                    {
                        if (allPlatforms[p].platform.moveType == Platform.MoveType.AroundPivot)
                            allPlatforms[p].platform.OverrideSpeed(levelSettings.difficulties[a].pivotPlatformSpeed);
                        else
                            allPlatforms[p].platform.OverrideSpeed(levelSettings.difficulties[a].movePlatformSpeed,
                                levelSettings.difficulties[a].movePlatformStopTime);
                    }
                    levelSettings.difficulties[a].reached = true;
                }
            }
        }
	}


    public GameObject AdPanel;
    public bool isCancelAd=false;
    public void SetAdPanel(bool a)
    {
        AdPanel.SetActive(a);
        Time.timeScale = a ? 0 : 1;
    }
    //Check if camera frustum contains object's collider bounds;
    public bool OutOfView(Collider2D collider)
    {
        return !GeometryUtility.TestPlanesAABB(planes, collider.bounds);
    }

    public void PreDie()
    {
        SetAdPanel(true);
    }

    public void Revive()
    {
        AdManager.ShowVideoAd("192if3b93qo6991ed0",
            (bol) => {
                if (bol)
                {
                    playerControls.Revive();
                    SetAdPanel(false);

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

    public void CancelAd()
    {
        SetAdPanel(false);
        isCancelAd = true;
        KillPlayer();
    }

    //Kill player function;
    void KillPlayer()
    {

        //Stop background music;
        ambience.ambienceSource.Stop();
        //If game is not over;
        if (!gameOver)
        {
            playerControls.Death();     //Set player state to death;     
            DrawDeathEffect();          //Draw death effetc;
            SaveBestScore();            //Save best score;
            SaveCoinsCount();           //Save coins count;
            UpdateScore();              //Update score; 
            Utilities.PlaySFX(audioSource, gameOverSettings.gameOverSfx, 1); //Play game over sound effect;
            gameOver = true;            //Set game over to true;
            AdManager.ShowInterstitialAd("1lcaf5895d5l1293dc",
() => {
  Debug.Log("--插屏广告完成--");

},
(it, str) => {
  Debug.LogError("Error->" + str);
});

        }
    }

    //Reset difficulty
    void ResetDifficulty()
    {
        for (int a = 0; a < levelSettings.difficulties.Length; a++)
            levelSettings.difficulties[a].reached = a == 0;
        for (int i = 0; i < allPlatforms.Count; i++)
        {
            for (int a = 0; a < allPlatforms.Count; a++)
            {
                if (allPlatforms[a].platform.moveType == Platform.MoveType.AroundPivot)
                    allPlatforms[a].platform.OverrideSpeed(levelSettings.difficulties[0].pivotPlatformSpeed);
                else
                    allPlatforms[a].platform.OverrideSpeed(levelSettings.difficulties[0].movePlatformSpeed,
                        levelSettings.difficulties[0].movePlatformStopTime);
            }
        }
        scoreSettings.ScoreInfo.color = levelSettings.difficulties[0].scoreTextColor;
    }

    //Draw Death Effect;
    void DrawDeathEffect()
    {
        deathAlpha.alpha = 1;   //Set death effect alpha to 1;
    }

    //Restart function;
    void Restart()
    {
        Utilities.PlaySFX(audioSource, gameOverSettings.clickSfx, 1);   //Play click sound effect;
        fadeGroup.alpha = 1;
        PlacePlatforms();                                               //Place level platforms;
        score = 0;                                                      //Reset score;
        coinsCount = 0;                                                 //Reset coins;
        ResetDifficulty();                                              //Reset difficulty;
        LoadBestScore();                                                //Load best score;
        playerControls.transform.position = playerSettings.spawnPosition.position;  //Move player to spawn position;
        playerControls.SetAlive();                                      //Set player state to alive;
        playerCam.Reset();                                              //Reset camera;
        if (rco)                            //rco is a RandomColorOverlay script, used to randomize material color;
            rco.SetRandomColorOverlay();    //Set random color to the renderer assigned to it;
        //Play background music;
        Utilities.PlaySFX(ambience.ambienceSource, ambience.musicLoop, 0.3F, true);
        gameOver = false;                   //Set game over to false;
        isCancelAd = false;
    }

    //Load menu
    void LoadMenu()
    {
        Utilities.PlaySFX(audioSource, gameOverSettings.clickSfx, 1);   //Play click sound effect;
        loading.LoadingScreen.SetActive(loading.drawLoadingscreen);     //If draw loading screen is checked, activate loading screen;                
        StartCoroutine("Loading");                                      //Start loading coroutine;
    }

    //Load scene with menuSceneIndex;
    private IEnumerator Loading()
    {
        async = Application.LoadLevelAsync(gameOverSettings.menuSceneIndex);
        yield return async;
    }

    //Draw coin pickup effect function;
    public void DrawCoinAtPosition(Vector3 position)
    {
        scoreSettings.coinPrefab.position = cam.WorldToScreenPoint(position);   //Move coinPrefab transform to given position;
        scoreSettings.coinPrefab.gameObject.SetActive(true);                    //Enable coin. As you can see in FixedUpdate, 
                                                                                //if coinPrefab is active, we moving it to the CoinsInfo transform position;
    }

    //CheckCoins is checked is coinPrefab position is the same as Coins info position;
    public void CheckCoins()
    {
        if (scoreSettings.coinPrefab.position == scoreSettings.CoinsInfo.position && scoreSettings.coinPrefab.gameObject.activeSelf)
        {
            coinsCount++;                                                   //Add coin;
            Utilities.PlaySFX(audioSource, scoreSettings.coinSFX, 0.5F);    //Play coin collect sound effect;
            scoreSettings.coinPrefab.gameObject.SetActive(false);           //Disable coin;
        }
    }

    public void AddScore()
    {
        score += scoreSettings.scorePerJump;                        //Increase score;
        Utilities.PlaySFX(audioSource, scoreSettings.scoreSFX, 1);  //Play score sound effect;
    }

    //Update score for game over menu;
    void UpdateScore()
    {
        SetScore(score);
        SetBestScore(bestScore, score);
    }
    //Display current score;
    void SetScore(int score)
    {
        gameOverSettings.Score.text = "<color=yellow>" + score.ToString() + "</color>";
    }
    //Display best score based on current score;
    void SetBestScore(int bestScore, int score)
    {
        gameOverSettings.BestScore.text = score <= bestScore ?  "<color=yellow>" + bestScore.ToString() + "</color>" : 
            "<color=yellow>" + score.ToString() + "</color>";
    }
    //Save best score to player prefs;
    void SaveBestScore()
    {
        if (score > bestScore)
            PlayerPrefs.SetInt("Best", score);
    }
    //Load best score;
    void LoadBestScore()
    {
        if (PlayerPrefs.HasKey("Best"))
            bestScore = PlayerPrefs.GetInt("Best");
    }
    //Save coins count;
    private void SaveCoinsCount()
    {
        defaultCoins += coinsCount;
        PlayerPrefs.SetInt("Coins", defaultCoins);
    }
    //Load coins count;
    private void LoadCoinsCount()
    {
        if (PlayerPrefs.HasKey("Coins"))
            defaultCoins = PlayerPrefs.GetInt("Coins");
    }
    
    //Istatiate platforms;
    void InstatiatePlatforms()
    {
        //Loop through platform list; 
        for(int i = 0; i < levelSettings.Platforms.Count; i++)
        {
            //Instatiate each platforms based on preload count and cache platform settings and components;
            for(int c = 0; c < levelSettings.Platforms[i].preloadCount; c++)
            {
                PreloadedPlatforms pp = new PreloadedPlatforms();
                pp.platformTransform = (Transform)Instantiate(levelSettings.Platforms[i].platformPrefab, Vector2.up * 300, Quaternion.identity);
                pp.platformTransform.SetParent(levelSettings.LevelStartPoint);
                pp.minPos = levelSettings.Platforms[i].minPos;
                pp.maxPos = levelSettings.Platforms[i].maxPos;
                pp.platform = pp.platformTransform.GetComponent<Platform>();
                pp.platformCollider = pp.platformTransform.GetComponent<Collider2D>();
                preloadedPlatforms.Add(pp);
                allPlatforms.Add(pp);
            }
        }

        PlacePlatforms();
    }
    //Place platforms;
    void PlacePlatforms()
    {
        //Move all passed platforms to preloaded list;
        if(passedPlatforms.Count > 0)
            for(int p = 0; p < passedPlatforms.Count; p++)
                preloadedPlatforms.Add(passedPlatforms[p]);

        passedPlatforms.Clear();        //Clean passed platforms;
        preloadedPlatforms.Shuffle();   //Shuffle preloaded list;
        curPlatform = null;             //Clean current platform;
        prevPlatform = null;            //Clean previous platform;
        newPos = Vector2.zero;          //Reset position offset;
        curPlatform = preloadedPlatforms[0].platform;//Set current platform to first one;

        for(int i = 0; i < preloadedPlatforms.Count; i++)
        {
            //Calculate newPos offset based on platfoms type;
            if(!prevPlatform)
            {
                if (curPlatform.moveType == Platform.MoveType.PointsBased)
                    newPos.x += Mathf.Abs(curPlatform.pointA.x);
                else if(curPlatform.moveType == Platform.MoveType.AroundPivot)
                    newPos.x += Mathf.Abs(curPlatform.moveRadius);
            }
            else
            {
                if (prevPlatform.moveType == Platform.MoveType.PointsBased && preloadedPlatforms[i].platform.moveType == Platform.MoveType.PointsBased)
                    newPos.x += Mathf.Abs(prevPlatform.pointA.x) + Mathf.Abs(preloadedPlatforms[i].platform.pointB.x);
                else if (prevPlatform.moveType == Platform.MoveType.None && preloadedPlatforms[i].platform.moveType == Platform.MoveType.PointsBased)
                    newPos.x += Mathf.Abs(preloadedPlatforms[i].platform.pointA.x);
                else if (prevPlatform.moveType == Platform.MoveType.PointsBased && preloadedPlatforms[i].platform.moveType == Platform.MoveType.None)
                    newPos.x += Mathf.Abs(prevPlatform.pointB.x);
                else if (prevPlatform.moveType == Platform.MoveType.AroundPivot && preloadedPlatforms[i].platform.moveType == Platform.MoveType.AroundPivot)
                    newPos.x += Mathf.Abs(prevPlatform.moveRadius) + Mathf.Abs(preloadedPlatforms[i].platform.moveRadius);
                else if (prevPlatform.moveType == Platform.MoveType.None && preloadedPlatforms[i].platform.moveType == Platform.MoveType.AroundPivot)
                    newPos.x += Mathf.Abs(preloadedPlatforms[i].platform.moveRadius);
                else if (prevPlatform.moveType == Platform.MoveType.AroundPivot && preloadedPlatforms[i].platform.moveType == Platform.MoveType.None)
                    newPos.x += Mathf.Abs(prevPlatform.moveRadius);
                else if (prevPlatform.moveType == Platform.MoveType.PointsBased && preloadedPlatforms[i].platform.moveType == Platform.MoveType.AroundPivot)
                    newPos.x += Mathf.Abs(prevPlatform.pointB.x) + Mathf.Abs(preloadedPlatforms[i].platform.moveRadius);
                else if (prevPlatform.moveType == Platform.MoveType.AroundPivot && preloadedPlatforms[i].platform.moveType == Platform.MoveType.PointsBased)
                    newPos.x += Mathf.Abs(prevPlatform.moveRadius) + Mathf.Abs(preloadedPlatforms[i].platform.pointA.x);
            }
            //Reposition platform;
            preloadedPlatforms[i].platformTransform.position = new Vector3(levelSettings.LevelStartPoint.position.x + newPos.x,
                Random.Range(preloadedPlatforms[i].minPos.y, preloadedPlatforms[i].maxPos.y));
            //Reset platform;
            preloadedPlatforms[i].platform.ResetPlatform();
            //Increase offset;
            newPos.x += Random.Range(preloadedPlatforms[i].minPos.x, preloadedPlatforms[i].maxPos.x);
            if (prevPlatform)
            {
                //Inverse moving platform start point if we previous platform is also moving;
                if (prevPlatform.moveType == Platform.MoveType.PointsBased)
                {
                    if (prevPlatform.curPoint == preloadedPlatforms[i].platform.curPoint)
                        preloadedPlatforms[i].platform.InverseStartPoint();
                }//Same fo the rotating platfom;
                else if (prevPlatform.moveType == Platform.MoveType.AroundPivot && preloadedPlatforms[i].platform.moveType != Platform.MoveType.PointsBased)
                {
                    if (prevPlatform.moveSpeed > 0 && preloadedPlatforms[i].platform.moveSpeed > 0 ||
                        prevPlatform.moveSpeed < 0 && preloadedPlatforms[i].platform.moveSpeed < 0)
                        preloadedPlatforms[i].platform.InverseSpeed();
                }
            }
            prevPlatform = preloadedPlatforms[i].platform; //Replace previous platform;
        }
        //Set last platform;
        lastPlatform = preloadedPlatforms[preloadedPlatforms.Count - 1].platform;
    }

    //Draw some scene gizmos;
    void OnDrawGizmos()
    {
        if (levelSettings.LevelStartPoint)
        {
            Utilities.DrawRadius(levelSettings.LevelStartPoint.position, 0.1F, Color.blue, 0.09F, Color.white);
            Utilities.SceneLabel(levelSettings.LevelStartPoint.position, new Vector2(0, -0.15F), 55, 200, 50, "Level Start Point", 10, FontStyle.Bold, Color.blue);
        }

        if (playerSettings.spawnPosition)
        {
            Utilities.DrawRadius(playerSettings.spawnPosition.position, 0.1F, Color.blue, 0.09F, Color.white);
            Utilities.SceneLabel(playerSettings.spawnPosition.position, new Vector2(0, -0.15F), 55, 200, 50, "Player Spawn Position", 10, FontStyle.Bold, Color.blue);
        }
    }
}

[System.Serializable]
public class LevelSettings
{
    public Transform LevelStartPoint;                           //Level generation start point;
    public List<Platforms> Platforms = new List<Platforms>();   //Platforms array;
    public Difficulty[] difficulties;
}

[System.Serializable]
public class Platforms
{
    public Transform platformPrefab;    //Platform prefab;
    public int preloadCount = 5;        //Platform preload count;
    public Vector2 minPos;              //Platform minimum position ofssets;
    public Vector2 maxPos;              //Platform maximum position offsets;
}

[System.Serializable]
public class Difficulty
{
    public int scoreAmount;
    public Color scoreTextColor = Color.white;
    public float movePlatformSpeed;
    public float movePlatformStopTime;
    public float pivotPlatformSpeed;
    [HideInInspector]
    public bool reached;
}

[System.Serializable]
public class ScoreSettings
{
    public Text ScoreInfo;              //Score info text to display current score;
    public int scorePerJump = 1;        //Hom much points add to score at once;
    public AudioClip scoreSFX;          //Increase scrore sound effect;
    public RectTransform CoinsInfo;     //Coins info transform;
    public RectTransform coinPrefab;    //Coin prefab, used to visualize coin pickups;
    public AudioClip coinSFX;           //Coin collect sound effect;
}

[System.Serializable]
public class DeathEffect
{
    public Image effectImage;           //Death effect image;
    public float effectSpeed;           //Effect speed;
}

[System.Serializable]
public class PlayerSettings
{
    public Transform[] playerPrefabs;   //Player prefabs;
    public Transform spawnPosition;     //Player spawn position;
}


[System.Serializable]
public class GameOverSettings
{
    public AudioClip gameOverSfx;       //Game over sound effect;
    public AudioClip clickSfx;          //Buttons click sound effect;
    public RectTransform gameOverPanel; //Game over UI panel;
    public Vector2 normalPosition;      //Game over panel normal position. Panel will move to this position if gameover is true;

    public Text Score;                  //Score display text;
    public Text BestScore;              //Best score display text

    public Button restartButton;        //Game restart button;
    public Button menuButton;           //Go to main menu button;
    public int menuSceneIndex;          //Main menu scene index;
}

[System.Serializable]
public class Loading
{
    [Tooltip("Fade out image, usually its a fullscreen image")]
    public Image fadeOutImage;              //Fade out image;
    [Tooltip("Fade out speed")]
    public float fadeSpeed = 0.5F;          //Fade out speed;
    [Tooltip("Show loading screen or not")]
    public bool drawLoadingscreen = true;   //Draw loading screen toogle;
    [Tooltip("Loading screen object, usually it is a background image")]
    public GameObject LoadingScreen;        //Loading screen object;
    [Tooltip("UI text for displaying loading progress")]
    public TMPro.TMP_Text loadingProgress;            //Loading progress display text;
}

[System.Serializable]
public class Ambience
{
    [Tooltip("Background music audio clip")]
    public AudioClip musicLoop;         //Background music audio clip;
    [Tooltip("Background music audio source")]
    public AudioSource ambienceSource;  //Source;
}

//Preloaded platforms cache variables;
[System.Serializable]
public class PreloadedPlatforms
{
    public Transform platformTransform;         //Transform;
    public Collider2D platformCollider;         //Collider;
    public Vector2 minPos;                      //Min positions
    public Vector2 maxPos;                      //Max positions;
    public Platform platform;                   //Script component;
}



