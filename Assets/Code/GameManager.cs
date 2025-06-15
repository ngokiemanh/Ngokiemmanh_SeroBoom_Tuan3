using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.Collections.Generic;
using TMPro;
using System.Collections;
using UnityEngine.Tilemaps;
public class GameManager : MonoBehaviour
{

    [Header("Canvas Prefabs")]
    public GameObject prefabCanvasStart;
    public GameObject prefabCanvasHowToPlay;
    public GameObject canvasPause;

    [Header("Audio Control")]
    public Slider bgmSlider;
    public Slider sfxSlider;
    public AudioMixer audioMixer;

    [Header("Level Prefabs")]
    public List<GameObject> levelPrefabs;

    [Header("Win Condition")]
    public GameObject lockedWin;
    public Sprite lockedSprite;
    public Sprite unlockedSprite;

    [Header("Gameplay Time")]
    public float levelTime = 60f;
    private float timeLeft;
    private bool timerRunning = false;

    [Header("UI Canvases")]
    public GameObject canvasLose;
    public GameObject canvasWin;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highscoreText;

    private bool winUnlocked = false;

    private GameObject currentLevel;
    private GameObject currentCanvasStart;
    private GameObject currentCanvasHowToPlay;

    private int currentLevelIndex = 0;
    private bool isGameStarted = false;

    public static GameManager Instance;

    void Awake() => Instance = this;

    public void Lose(string reason)
    {
        Debug.Log("Thua: " + reason);
        ShowLose();
    }

    void Start()
    {
        ShowStartCanvas();

        if (bgmSlider != null)
            bgmSlider.onValueChanged.AddListener((value) => audioMixer.SetFloat("BGM", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20));

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener((value) => audioMixer.SetFloat("SFX", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20));
    }

    void Update()
    {
        if (lockedWin == null)
        {
            lockedWin = GameObject.FindGameObjectWithTag("LockedWin");
        }

        if (!isGameStarted) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            canvasPause.SetActive(!canvasPause.activeSelf);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            OnClickReplay();
        }

        CheckWinCondition();
        HandleTimer();
    }

    void HandleTimer()
    {
        if (!timerRunning) return;

        timeLeft -= Time.deltaTime;

        if (timeText != null)
            timeText.text = Mathf.CeilToInt(timeLeft).ToString();

        if (timeLeft <= 0)
        {
            timeLeft = 0;
            timerRunning = false;
            ShowLose();
        }
    }

    void CheckWinCondition()
    {
        if (lockedWin == null)
            lockedWin = GameObject.FindGameObjectWithTag("LockedWin");

        if (winUnlocked) return;

        GameObject[] apples = GameObject.FindGameObjectsWithTag("Apple");
        GameObject[] bananas = GameObject.FindGameObjectsWithTag("Banana");

        if (apples.Length == 0 && bananas.Length == 0)
        {
            winUnlocked = true;
            if (lockedWin != null && unlockedSprite != null)
            {
                SpriteRenderer sr = lockedWin.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sprite = unlockedSprite;
            }
        }
        else
        {
            if (lockedWin != null && lockedSprite != null)
            {
                SpriteRenderer sr = lockedWin.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sprite = lockedSprite;
            }
        }
    }

    public void TryWin(GameObject obj)
    {
        if (winUnlocked && obj == lockedWin)
        {
            ShowWin();
            Debug.Log("YOU WIN!");
        }
    }

    void ShowStartCanvas()
    {
        if (currentCanvasHowToPlay != null) Destroy(currentCanvasHowToPlay);
        if (currentCanvasStart != null) Destroy(currentCanvasStart);
        if (timeText != null)
            timeText.gameObject.SetActive(false);

        currentCanvasStart = Instantiate(prefabCanvasStart);

        FindAndHookButton(currentCanvasStart, "ButtonPlay", StartGame);
        FindAndHookButton(currentCanvasStart, "ButtonHowToPlay", ShowHowToPlay);
    }

    void ShowHowToPlay()
    {
        if (currentCanvasStart != null) Destroy(currentCanvasStart);

        currentCanvasHowToPlay = Instantiate(prefabCanvasHowToPlay);
        FindAndHookButton(currentCanvasHowToPlay, "ButtonBack", BackToStart);
    }

    void BackToStart()
    {
        if (currentCanvasHowToPlay != null) Destroy(currentCanvasHowToPlay);
        ShowStartCanvas();
    }

    void StartGame()
    {
        if (currentCanvasStart != null) Destroy(currentCanvasStart);

        currentLevelIndex = 0;
        isGameStarted = true;
        LoadLevel(currentLevelIndex);
    }

    void LoadLevel(int index)
    {
        if (timeText != null)
            timeText.gameObject.SetActive(true);

        if (currentLevel != null)
        {
            Destroy(currentLevel);
            currentLevel = null;
        }

        WormMovement oldPlayer = FindObjectOfType<WormMovement>();
        if (oldPlayer != null)
            Destroy(oldPlayer.gameObject);

        DestroyAllBodySegments();

        if (canvasWin != null) canvasWin.SetActive(false);
        if (canvasLose != null) canvasLose.SetActive(false);

        if (index >= 0 && index < levelPrefabs.Count)
        {
            currentLevel = Instantiate(levelPrefabs[index]);

            FindAndHookButton(currentLevel, "ButtonHome", OnClickHome);
            FindAndHookButton(currentLevel, "ButtonReplay", OnClickReplay);
            FindAndHookButton(currentLevel, "ButtonNextLevel", NextLevel);

            lockedWin = GameObject.FindGameObjectWithTag("LockedWin");
            if (lockedWin != null && lockedSprite != null)
            {
                SpriteRenderer sr = lockedWin.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sprite = lockedSprite;
            }

            AssignTilemapsToPushables();
        }

        winUnlocked = false;

        timeLeft = levelTime;
        timerRunning = true;
        if (timeText != null)
            timeText.text = Mathf.CeilToInt(timeLeft).ToString();

        StartCoroutine(WaitAndHookMobileButtons());
    }

    void AssignTilemapsToPushables()
    {
        Tilemap[] tilemaps = FindObjectsOfType<Tilemap>();

        foreach (var apple in FindObjectsOfType<PushableApple>())
        {
            apple.tilemaps = tilemaps;
        }

        foreach (var banana in FindObjectsOfType<PushableBanana>())
        {
            banana.tilemaps = tilemaps;
        }
    }


    IEnumerator WaitAndHookMobileButtons()
    {
        yield return new WaitForSeconds(0.1f);

        WormMovement player = FindObjectOfType<WormMovement>();
        if (player != null)
        {
            HookMobileButton("ButtonUp", () => player.SetDirectionFromUI("Up"));
            HookMobileButton("ButtonDown", () => player.SetDirectionFromUI("Down"));
            HookMobileButton("ButtonLeft", () => player.SetDirectionFromUI("Left"));
            HookMobileButton("ButtonRight", () => player.SetDirectionFromUI("Right"));
        }
    }

    void HookMobileButton(string tag, UnityEngine.Events.UnityAction callback)
    {
        GameObject btnObj = GameObject.FindGameObjectWithTag(tag);
        if (btnObj != null)
        {
            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(callback);
            }
        }
    }

    void FindAndHookButton(GameObject parent, string tag, UnityEngine.Events.UnityAction callback)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.CompareTag(tag))
            {
                Button btn = child.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(callback);
                    return;
                }
            }
        }
    }


    public void OnClickHome()
    {
        if (currentLevel != null)
        {
            Destroy(currentLevel);
            currentLevel = null;
        }

        WormMovement oldPlayer = FindObjectOfType<WormMovement>();
        if (oldPlayer != null)
            Destroy(oldPlayer.gameObject);

        DestroyAllBodySegments();

        isGameStarted = false;
        timerRunning = false;

        canvasPause.SetActive(false);
        canvasWin.SetActive(false);
        canvasLose.SetActive(false);

        if (timeText != null)
            timeText.gameObject.SetActive(false);

        ShowStartCanvas();
    }

    public void OnClickReplay()
    {
        LoadLevel(currentLevelIndex);
    }

    public void NextLevel()
    {
        currentLevelIndex++;
        if (currentLevelIndex < levelPrefabs.Count)
        {
            LoadLevel(currentLevelIndex);
            canvasPause.SetActive(false);
        }
        else
        {
            OnClickHome();
        }
    }

    void ShowLose()
    {
        timerRunning = false;
        if (canvasLose != null) canvasLose.SetActive(true);

    }

    void ShowWin()
    {
        timerRunning = false;
        if (canvasWin != null) canvasWin.SetActive(true);

        int score = Mathf.CeilToInt(timeLeft);
        int high = GetHighscore(currentLevelIndex);

        if (score > high)
        {
            SetHighscore(currentLevelIndex, score);
            high = score;
        }

        if (scoreText != null) scoreText.text = "\u0110i\u1ec3m: " + score;
        if (highscoreText != null) highscoreText.text = "K\u1ef7 l\u1ee5c: " + high;
    }

    int GetHighscore(int levelIndex)
    {
        return PlayerPrefs.GetInt("Highscore_Level_" + levelIndex, 0);
    }

    void SetHighscore(int levelIndex, int score)
    {
        PlayerPrefs.SetInt("Highscore_Level_" + levelIndex, score);
        PlayerPrefs.Save();
    }

    void DestroyAllBodySegments()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj != null && obj.layer == LayerMask.NameToLayer("Body"))
            {
                Destroy(obj);
            }
        }
    }
}
