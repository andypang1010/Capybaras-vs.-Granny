using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public enum GameState
{
    //Lobby,
    Menu,
    Playing,
    GameOver,
    None,
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // Singleton instance

    public bool IsDebug = false;
    public GameState _currentState = GameState.Playing;

    [Header("Timer Settings")]
    public float GameDuration = 300f; // Total game duration in seconds
    public bool IsTimerRunning = false;
    public float GameTimer = 0f;

    [Header("Player Settings")]
    public List<CapybaraController> CapybaraControllers;
    public GrannyController GrannyController;
    public List<PlayerController> ReadyPlayers;

    public GameObject MissileTarget;

    // [Header("UI Settings")]
    // public List<UIManager> UIManagers;

    [Header("State Change Events")]
    public UnityEvent OnGameStartEvent;
    public UnityEvent OnGameOverEvent;
    public UnityEvent OnGameResetEvent;

    [Header("BGM Settings")]
    public AudioSource BGM;
    public AudioSource MenuBGM;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        Debug.Log("displays connected: " + Display.displays.Length);
        // Display.displays[0] is the primary, default display and is always ON.
        // Check if additional displays are available and activate each.
        if (Display.displays.Length > 1)
            Display.displays[1].Activate();
    }

    private void OnEnable() { }

    private void Start()
    {
        CapybaraController[] foundCapybaras = FindObjectsByType<CapybaraController>(
            sortMode: FindObjectsSortMode.None
        );
        Debug.Log($"Found {foundCapybaras.Length} CapybaraControllers in the scene.");
        CapybaraControllers = new List<CapybaraController>(
            new CapybaraController[foundCapybaras.Length]
        );
        foreach (var capy in foundCapybaras)
        {
            CapybaraControllers[capy.playerIndex - 1] = capy;
        }
        // ---------------------------------------------------------------------------
        GrannyController = FindFirstObjectByType<GrannyController>();
        OnGameResetEvent?.Invoke();

        if (!MenuBGM.isPlaying)
        {
            if (BGM.isPlaying)
                OnAudioChanged(BGM, MenuBGM);
            else
                MenuBGM.Play();
        }
        IsTimerRunning = false;
        GameTimer = GameDuration;
        foreach (var capy in CapybaraControllers)
        {
            capy.currentState = CapybaraState.Default;
        }

        _currentState = GameState.Menu;
    }

    void Update()
    {
        if (IsTimerRunning)
        {
            if (GameTimer <= 0)
            {
                GameTimer = 0;
                IsTimerRunning = false;
                EndGame();
            }
            GameTimer -= Time.deltaTime;
        }

        HandleDebugInput();
    }

    private void HandleDebugInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Debug: Starting game with Space key");
            SetGameState(GameState.Playing);
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            Debug.Log("Debug: Ending game with Enter key");
            SetGameState(GameState.GameOver);
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
        {
            Debug.Log("Debug: Resetting game with Shift key");
            SetGameState(GameState.Menu);
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log("Debug: Quitting application with Q key");
            Application.Quit();
        }
    }

    public void RegisterGrannyController(GrannyController controller)
    {
        if (controller == null)
            return;

        GrannyController = controller;
    }

    public void UnregisterGrannyController(GrannyController controller)
    {
        if (controller == null)
            return;

        if (GrannyController == controller)
        {
            GrannyController = null;
        }
    }

    public void SetGameState(GameState newState)
    {
        switch (newState)
        {
            case GameState.Menu:
                if (_currentState == GameState.GameOver)
                {
                    ResetGame();
                }
                break;
            case GameState.Playing:
                if (_currentState == GameState.Menu)
                {
                    StartGame();
                }
                break;
            case GameState.GameOver:
                if (_currentState == GameState.Playing)
                {
                    EndGame();
                }
                break;
            default:
                Time.timeScale = 1f; // Default to normal time scale
                break;
        }
        Debug.Log($"Game State changed to: {_currentState}");
    }

    public void ResetGame()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        string currentSceneName = currentScene.name;

        SceneManager.LoadScene(currentSceneName, LoadSceneMode.Single);
    }

    public void StartGame()
    {
        //OnAudioChanged(MenuBGM, BGM);
        if(MenuBGM.isPlaying)
        {
            MenuBGM.Stop();
        }
        IsTimerRunning = true;
        OnGameStartEvent?.Invoke();
        ReadyPlayers = new List<PlayerController>();
        StartCoroutine(tempCoroutine(5f));
        IEnumerator tempCoroutine(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            foreach (var capy in CapybaraControllers)
            {
                capy.currentState = CapybaraState.Playing;
            }
            _currentState = GameState.Playing;
        }
    }

    public void EndGame()
    {
        OnAudioChanged(BGM, MenuBGM);
        IsTimerRunning = false;
        OnGameOverEvent?.Invoke();

        _currentState = GameState.GameOver;
    }

    public int CheckCapybaraDeadCount() // call by CapybaraController when taking damage
    {
        int deadCount = 0;
        foreach (var capy in CapybaraControllers)
        {
            if (capy.currentState == CapybaraState.Died)
            {
                deadCount++;
            }
        }
        return deadCount;
    }

    private void OnAudioChanged(AudioSource audio1, AudioSource audio2)
    {
        if (audio1 != null && audio2 != null)
        {
            StartCoroutine(CrossfadeAudio(audio1, audio2, 2.0f));
        }
    }

    public void PlayBGM()
    {
        if (!BGM.isPlaying)
        {
            BGM.Play();
        }
    }

    private IEnumerator CrossfadeAudio(AudioSource fadeOut, AudioSource fadeIn, float duration)
    {
        float initialFadeInVolume = fadeIn.volume;
        float initialFadeOutVolume = fadeOut.volume;
        float fadeOutStartVolume = fadeOut.volume;
        float fadeInStartVolume = fadeIn.volume;

        if (!fadeIn.isPlaying)
        {
            fadeIn.volume = 0f;
            fadeIn.Play();
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            fadeOut.volume = Mathf.Lerp(fadeOutStartVolume, 0f, t);

            fadeIn.volume = Mathf.Lerp(0f, fadeInStartVolume, t);

            yield return null;
        }

        fadeOut.volume = 0f;
        fadeIn.volume = fadeInStartVolume;

        fadeOut.Stop();
        fadeOut.volume = initialFadeOutVolume;
    }

    public void JudgeGameOver()
    {
        int deadCount = CheckCapybaraDeadCount();
        if (deadCount >= 3)
        {
            EndGame();
        }
    }

    public void RegisterReadyPlayer(PlayerController player)
    {
        if (player != null && !ReadyPlayers.Contains(player))
        {
            ReadyPlayers.Add(player);
        }

        if (ReadyPlayers.Count >= 4 && _currentState == GameState.Menu)
        {
            StartGame();
        }

        if (ReadyPlayers.Count >= 4 && _currentState == GameState.GameOver)
        {
            ResetGame();
            //StartGame();
        }
    }

    public void UnregisterReadyPlayer(PlayerController player)
    {
        if (player != null && ReadyPlayers.Contains(player))
        {
            ReadyPlayers.Remove(player);
        }
    }
}
