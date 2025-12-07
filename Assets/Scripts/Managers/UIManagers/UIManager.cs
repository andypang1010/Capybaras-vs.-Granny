using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using TS.PageSlider;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[System.Serializable]
public struct TutTextContent
{
    public TMPro.TextMeshProUGUI textUI;
    public string content;

    // public TutTextContent(TMPro.TextMeshProUGUI textUI, string content)
    // {
    //     this.textUI = textUI;
    //     this.content = content;
    // }
}

public class UIManager : MonoBehaviour
{
    public int PlayerIndex = 0;
    public TMPro.TextMeshProUGUI InGameTimerText;
    public ProgressBar TimerBar;
    public ProgressBar HPBar;
    public ProgressBar EnergyBar;
    public List<TutTextContent> TutTexts;
    public RawImage ControllerImage;
    public Texture2D PlaystationTexture;
    public Texture2D XboxTexture;

    [Header("UI Panels")]
    public GameObject MenuPanel;
    public GameObject StartCountingPanel;
    public GameObject InGamePanel;
    public GameObject GameOverPanel;
    public PageSlider TutSlider;
    public ButtonVisual StartButtonVisual;
    public ButtonVisual RestartButtonVisual;

    protected virtual void Awake() { }

    protected virtual void OnDestroy()
    {
        GameManager.Instance.OnGameResetEvent.RemoveListener(ShowMenuUI);
        GameManager.Instance.OnGameStartEvent.RemoveListener(ShowStartCountingUI);
        GameManager.Instance.OnGameOverEvent.RemoveListener(ShowGameOverUI);
    }

    protected virtual void Start()
    {
        ControllerType controllerType = ControllerHelper.GetDeviceType(
            Gamepad.all.Count >= PlayerIndex ? Gamepad.all[PlayerIndex - 1] : null
        );
        if (controllerType == ControllerType.PlayStation)
        {
            if (ControllerImage && PlaystationTexture)
            {
                ControllerImage.texture = PlaystationTexture;
            }
        }
        else if (controllerType == ControllerType.Xbox)
        {
            if (ControllerImage && XboxTexture)
            {
                ControllerImage.texture = XboxTexture;
            }
        }

        GameManager.Instance.OnGameResetEvent.AddListener(ShowMenuUI);
        GameManager.Instance.OnGameStartEvent.AddListener(ShowStartCountingUI);
        GameManager.Instance.OnGameOverEvent.AddListener(ShowGameOverUI);
        if (!TutSlider)
        {
            TutSlider = GetComponentInChildren<PageSlider>();
        }
        //setTutTexts();
        ShowMenuUI();
    }

    protected virtual void Update()
    {
        if (GameManager.Instance.IsTimerRunning)
        {
            float time = GameManager.Instance.GameTimer;
            UpdateInGameTimer(time);
        }
    }

    public void setTutTexts()
    {
        foreach (var tutText in TutTexts)
        {
            if (tutText.textUI != null)
            {
                tutText.textUI.text = tutText.content;
            }
        }
    }

    public void UpdateInGameTimer(float time)
    {
        if (InGameTimerText != null)
        {
            int minutes = Mathf.FloorToInt(time / 60F);
            int seconds = Mathf.FloorToInt(time - minutes * 60);
            InGameTimerText.text = $"Time Left: {string.Format("{0:0}:{1:00}", minutes, seconds)}";
        }

        if (TimerBar)
        {
            TimerBar.SetProgress(time / GameManager.Instance.GameDuration);
        }
    }

    public void UpdatePlayerHP(float hp)
    {
        if (HPBar != null)
        {
            HPBar.SetProgress(hp / 100f);
        }
    }

    public void UpdatePlayerEnergy(float energyRate)
    {
        if (EnergyBar != null)
        {
            EnergyBar.SetProgress(energyRate);
        }
    }

    public virtual void ShowMenuUI()
    {
        //Debug.Log("Show Menu UI");
        MenuPanel.SetActive(true);
        StartCountingPanel.SetActive(false);
        InGamePanel.SetActive(false);
        GameOverPanel.SetActive(false);
    }

    public virtual void ShowStartCountingUI()
    {
        //Debug.Log("Show Start Counting UI");
        MenuPanel.SetActive(false);
        StartCountingPanel.SetActive(true);
        InGamePanel.SetActive(false);
        GameOverPanel.SetActive(false);
        StartCoroutine(startToShowInGaneUI());
        IEnumerator startToShowInGaneUI()
        {
            StartCountingPanel.GetComponent<StartCountingUI>()?.StartCountdown();
            yield return new WaitForSeconds(5f);
            ShowInGameUI();
        }
    }

    public virtual void ShowInGameUI()
    {
        GameManager.Instance.PlayBGM();
        //Debug.Log("Show In Game UI");
        MenuPanel.SetActive(false);
        StartCountingPanel.SetActive(false);
        InGamePanel.SetActive(true);
        GameOverPanel.SetActive(false);
    }

    public virtual void ShowGameOverUI()
    {
        //Debug.Log("Show Game Over UI");
        MenuPanel.SetActive(false);
        StartCountingPanel.SetActive(false);
        InGamePanel.SetActive(false);
        GameOverPanel.SetActive(true);
    }

    public void OnStartButtonPressed()
    {
        if (!StartButtonVisual)
        {
            return;
        }
        if (!StartButtonVisual.IsPressed)
        {
            StartButtonVisual.setPressedVisual(true);
        }
        else
        {
            StartButtonVisual.setPressedVisual(false);
        }
    }

    public void OnRestartButtonPressed()
    {
        if (!RestartButtonVisual)
        {
            return;
        }
        if (!RestartButtonVisual.IsPressed)
        {
            RestartButtonVisual.setPressedVisual(true);
        }
        else
        {
            RestartButtonVisual.setPressedVisual(false);
        }
    }
}
