using UnityEngine;
using UnityEngine.InputSystem;

enum CapybaraType
{
    DASH,
    PARRY,
    SPELL,
}

public class CapybaraUIManager : UIManager
{
    [Header("Player References")]
    public CapybaraController player;
    CapybaraType capybaraType;
    public GameObject GameOverWinPage;
    public GameObject GameOverPartlyWinPage;
    public GameObject GameOverLosePage;
    public GameObject DeadPage;
    public ProgressBar resuscitateProgressBar;

    public Camera trackingCamera; // Camera to check if Granny is in view
    public LayerMask obstacleLayerMask = -1; // Layers that can block line of sight

    protected override void Start()
    {
        if (TutTexts.Count >= 6)
        {
            string icon = "<sprite index=11>";
            ControllerType controllerType = ControllerHelper.GetDeviceType(
                Gamepad.all.Count >= PlayerIndex ? Gamepad.all[PlayerIndex - 1] : null
            );

            var t0 = TutTexts[0];
            t0.content = $"Use    {icon}to move around. ";
            TutTexts[0] = t0;

            var t1 = TutTexts[1];
            t1.content = "Your ability UI will turn white when ready.";
            TutTexts[1] = t1;

            var t2 = TutTexts[2];
            icon =
                controllerType == ControllerType.PlayStation
                    ? "<sprite index=2>"
                    : "<sprite index=9>";
            t2.content = $"Press    {icon}to use your ability.";
            TutTexts[2] = t2;

            var t3 = TutTexts[3];
            t3.content = $"Follow the arrow at your feet to find your teammates\nand stack on them.";
            TutTexts[3] = t3;

            var t4 = TutTexts[4];
            t4.content = $"Stack with your teammates to enhance your ability.";
            TutTexts[4] = t4;

            var t5 = TutTexts[5];
            icon =
                controllerType == ControllerType.PlayStation
                    ? "<sprite index=0>"
                    : "<sprite index=8>";
            t5.content = $"Tap    {icon}repeatedly to revive dead teammates.";
            TutTexts[5] = t5;

            var t6 = TutTexts[6];
            t6.content = $"Survive until the helicopter arrives to win.";
            TutTexts[6] = t6;
        }

        base.Start();
        setTutTexts();

        if (trackingCamera == null)
        {
            trackingCamera = Camera.main;
        }

        if (player)
        {
            PlayerIndex = player.playerIndex;
            player.OnPlayerDamaged.AddListener(UpdatePlayerHP);
            player.OnCapybaraRevived.AddListener(UpdatePlayerHP);
            player.OnCapybaraRevived.AddListener(UpdateCapybaraDeadState);
            player.OnCapybaraResuscitate.AddListener(UpdateCapybaraResuscitate);
            player.OnCapybaraDied.AddListener(UpdateCapybaraDeadState);
            GameManager.Instance.OnGameOverEvent.AddListener(setGameOverUI);
            GameManager.Instance.OnGameResetEvent.AddListener(resetGameOverUI);
            if (player.ability is CapybaraDash)
            {
                capybaraType = CapybaraType.DASH;
            }
            if (player.ability is CapybaraShield)
            {
                capybaraType = CapybaraType.PARRY;
            }
            if (player.ability is CapybaraSpell)
            {
                capybaraType = CapybaraType.SPELL;
            }
        }
        resuscitateProgressBar.SetProgress(0f);
        DeadPage.SetActive(false);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (player)
        {
            player.OnPlayerDamaged.RemoveListener(UpdatePlayerHP);
            player.OnCapybaraRevived.RemoveListener(UpdatePlayerHP);
            player.OnCapybaraRevived.RemoveListener(UpdateCapybaraDeadState);
            player.OnCapybaraDied.RemoveListener(UpdateCapybaraDeadState);
            player.OnCapybaraResuscitate.RemoveListener(UpdateCapybaraResuscitate);
            GameManager.Instance.OnGameOverEvent.RemoveListener(setGameOverUI);
            GameManager.Instance.OnGameResetEvent.RemoveListener(resetGameOverUI);
        }
    }

    protected override void Update()
    {
        base.Update();

        if (
            GameManager.Instance._currentState == GameState.GameOver
            && InputManager.Instance.GetCapybaraAbilityDown(player.playerIndex)
        )
        {
            OnRestartButtonPressed();
            if (RestartButtonVisual.IsPressed)
            {
                GameManager.Instance.RegisterReadyPlayer(player);
            }
            else
            {
                GameManager.Instance.UnregisterReadyPlayer(player);
            }
        }

        if (GameManager.Instance._currentState == GameState.Menu)
        {
            if (InputManager.Instance.GetRightShoulderDown(player.playerIndex))
            {
                //Debug.Log("Next Page");
                TutSlider.GoToNextPage();
            }

            if (InputManager.Instance.GetLeftShoulderDown(player.playerIndex))
            {
                //Debug.Log("Previous Page");
                TutSlider.GoToPreviousPage();
            }
            if (InputManager.Instance.GetCapybaraAbilityDown(player.playerIndex))
            {
                OnStartButtonPressed();
                if (StartButtonVisual.IsPressed)
                {
                    GameManager.Instance.RegisterReadyPlayer(player);
                }
                else
                {
                    GameManager.Instance.UnregisterReadyPlayer(player);
                }
            }
        }

        if (GameManager.Instance._currentState != GameState.Playing)
        {
            return;
        }

        switch (capybaraType)
        {
            case CapybaraType.DASH:
                UpdatePlayerEnergy(player.ability.cdTimer / player.ability.currentCD);
                break;
            case CapybaraType.PARRY:
                UpdatePlayerEnergy(player.ability.cdTimer / player.ability.currentCD);
                break;
            case CapybaraType.SPELL:
                UpdatePlayerEnergy(player.ability.cdTimer / player.ability.currentCD);
                break;
            default:
                break;
        }
    }

    private void UpdateCapybaraDeadState(float hp)
    {
        if (hp <= 0)
        {
            DeadPage.SetActive(true);
            UpdateCapybaraResuscitate(0f);
        }
        else
        {
            DeadPage.SetActive(false);
            UpdateCapybaraResuscitate(0f);
        }
    }

    private void UpdateCapybaraResuscitate(float progress)
    {
        if (resuscitateProgressBar)
        {
            resuscitateProgressBar.SetProgress(progress);
        }
    }

    private void setGameOverUI()
    {
        int capyDeadCount = GameManager.Instance.CheckCapybaraDeadCount();
        Debug.Log("Capybara Dead Count: " + capyDeadCount);
        if (player.currentState == CapybaraState.Died)
        {
            GameOverWinPage.SetActive(false);
            GameOverPartlyWinPage.SetActive(false);
            GameOverLosePage.SetActive(true);
        }
        else if (player.currentState == CapybaraState.Playing)
        {
            if (capyDeadCount <= 0)
            {
                GameOverWinPage.SetActive(true);
                GameOverPartlyWinPage.SetActive(false);
                GameOverLosePage.SetActive(false);
            }
            else
            {
                GameOverWinPage.SetActive(false);
                GameOverPartlyWinPage.SetActive(true);
                GameOverLosePage.SetActive(false);
            }
        }
    }

    private void resetGameOverUI()
    {
        GameOverWinPage.SetActive(false);
        GameOverLosePage.SetActive(false);
    }
}
