using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class GrannyUIManager : UIManager
{
    [Header("Granny UI Components")]
    public GrannyController Player;
    public RawImage TagAbility1;
    public RawImage TagAbility2;
    public RawImage TagAbility3;
    public List<RawImage> AbilityChosenIndicators;
    public Color TagAbilityActiveColor;
    public Color TagAbilityInactiveColor;
    public GameObject ShockedEffectRange;
    public GameObject WiningStarsContainer;
    public GameObject WiningStarPrefab;
    public GameObject GameOverWinPage;
    public GameObject GameOverLosePage;

    [Header("Tracking UI Settings")]
    public GameObject MissileCancelledTag;
    public Camera trackingCamera; // The camera to check field of view
    public Transform MissileLaunchPoint; // Point from which missiles are launched
    public string targetTag = "Capybara"; // Tag of objects to track
    public GameObject trackingCursor; // Prefab for the tracking cursor UI
    public bool IsTrackingCursorVisible = false;
    public Canvas uiCanvas; // UI Canvas to parent the tracking cursors
    public float updateFrequency = 0.1f; // How often to check for targets (in seconds)

    [Header("Raycast Settings")]
    public LayerMask targetLayerMask;

    private GameObject activeCursor;
    private List<CapybaraController> capybarasInView = new List<CapybaraController>();

    protected override void Start()
    {
        if (TutTexts.Count >= 6)
        {
            string icon = "<sprite index=11>";
            ControllerType controllerType = ControllerHelper.GetDeviceType(
                Gamepad.all.Count >= PlayerIndex ? Gamepad.all[PlayerIndex-1] : null
            );

            var t0 = TutTexts[0];
            t0.content = $"Use    {icon}to move forward/backward and rotate. ";
            TutTexts[0] = t0;

            var t1 = TutTexts[1];
            t1.content =
                "All abilities are greyed out while on cooldown.\nThey return to full color when ready.";
            TutTexts[1] = t1;

            var t2 = TutTexts[2];
            icon =
                controllerType == ControllerType.PlayStation
                    ? "<sprite index=2>"
                    : "<sprite index=9>";
            t2.content = $"Press    {icon}to dash forward, damaging capybaras and breaking walls.";
            TutTexts[2] = t2;

            var t3 = TutTexts[3];
            icon =
                controllerType == ControllerType.PlayStation
                    ? "<sprite index=3>"
                    : "<sprite index=7>";
            t3.content =
                $"Hold    {icon}to conjure lightning that breaks the capybara stack and stuns them.";
            TutTexts[3] = t3;

            var t4 = TutTexts[4];
            icon =
                controllerType == ControllerType.PlayStation
                    ? "<sprite index=0>"
                    : "<sprite index=8>";
            t4.content = $"Hold    {icon}to aim and fire a missile that homes in on a capybara.";
            TutTexts[4] = t4;

            var t5 = TutTexts[5];
            icon =
                controllerType == ControllerType.PlayStation
                    ? "<sprite index=0>"
                    : "<sprite index=8>";
            t5.content =
                $"Kill at least one capybara before time runs out.\nEarn more stars by killing more.";
            TutTexts[5] = t5;
        }
        setTutTexts();
        base.Start();
        if (Player)
        {
            PlayerIndex = Player.playerIndex;
            Player.OnPlayerDamaged.AddListener(UpdatePlayerHP);
            Player.OnAbilityChosen.AddListener(updateAbilityChosenStates);
            Player.OnMissileCanceled.AddListener(PopMissileFailedTag);
            Player.OnShockCanceled.AddListener(ShockFailed);
        }
        GameManager.Instance.OnGameOverEvent.AddListener(setGameOverUI);
        GameManager.Instance.OnGameResetEvent.AddListener(resetGameOverUI);
        ShockedEffectRange = Player.ShockRangeVisualizer;
        if (ShockedEffectRange)
            ShockedEffectRange.SetActive(false);
        targetLayerMask = LayerMask.GetMask("Capybara");
        MissileCancelledTag.SetActive(false);
        foreach (RawImage img in AbilityChosenIndicators)
        {
            img.enabled = false;
        }
        MissileLaunchPoint = Player.MissileSpawnPoint;
        InitializeTrackingSystem();
    }

    private void InitializeTrackingSystem()
    {
        if (trackingCamera == null)
        {
            trackingCamera = Camera.main;
        }

        if (uiCanvas == null)
        {
            uiCanvas = GetComponent<Canvas>();
        }
    }

    protected override void Update()
    {
        base.Update();
        if (
            GameManager.Instance._currentState == GameState.GameOver
            && InputManager.Instance.GetSouthButtonDownGranny() == ButtonState.Pressed
        )
        {
            OnRestartButtonPressed();
            if (RestartButtonVisual.IsPressed)
            {
                GameManager.Instance.RegisterReadyPlayer(Player);
            }
            else
            {
                GameManager.Instance.UnregisterReadyPlayer(Player);
            }
        }

        if (GameManager.Instance._currentState == GameState.Menu)
        {
            if (InputManager.Instance.GetRightShoulderDown(Player.playerIndex))
            {
                Debug.Log("Next Page");
                TutSlider.GoToNextPage();
            }

            if (InputManager.Instance.GetLeftShoulderDown(Player.playerIndex))
            {
                Debug.Log("Previous Page");
                TutSlider.GoToPreviousPage();
            }

            if (InputManager.Instance.GetSouthButtonDownGranny() == ButtonState.Pressed)
            {
                OnStartButtonPressed();
                if (StartButtonVisual.IsPressed)
                {
                    GameManager.Instance.RegisterReadyPlayer(Player);
                }
                else
                {
                    GameManager.Instance.UnregisterReadyPlayer(Player);
                }
            }
        }

        if (GameManager.Instance._currentState != GameState.Playing)
        {
            return;
        }
        UpdatePlayerEnergy(Player.CurrentEnergy / Player.MaxEnergy);
        updateAbilityReadyStates();
    }

    protected void FixedUpdate()
    {
        if (GameManager.Instance._currentState != GameState.Playing)
        {
            return;
        }

        if (IsTrackingCursorVisible)
        {
            UpdateTrackingTarget();
            UpdateCursorPosition();
        }
        else
        {
            trackingCursor.SetActive(false);
        }
    }

    private void updateAbilityReadyStates()
    {
        if (Player)
        {
            TagAbility1.color =
                Player.CurrentEnergy >= Player.DashEnergyCost
                    ? TagAbilityActiveColor
                    : TagAbilityInactiveColor;
            TagAbility2.color =
                Player.CurrentEnergy >= Player.ShockEnergyCost
                    ? TagAbilityActiveColor
                    : TagAbilityInactiveColor;
            TagAbility3.color =
                Player.CurrentEnergy >= Player.MissileEnergyCost
                    ? TagAbilityActiveColor
                    : TagAbilityInactiveColor;
        }
    }

    private void updateAbilityChosenStates(int index)
    {
        Debug.Log($"GrannyUIManager: Ability {index} chosen.");
        if(ShockedEffectRange)
            ShockedEffectRange.SetActive(false);
        foreach (RawImage img in AbilityChosenIndicators)
        {
            img.enabled = false;
        }
        if (index != 0)
        {
            MissileCancelledTag.SetActive(false);
        }
        switch (index)
        {
            case 0:
                IsTrackingCursorVisible = false;
                return;
            case 1:
                if (Player.Ability1State == GrannyAbilityState.Ready)
                {
                    AbilityChosenIndicators[0].enabled = true;
                    IsTrackingCursorVisible = false;
                }
                break;
            case 2:
                if (Player.Ability2State == GrannyAbilityState.Ready)
                {
                    if (ShockedEffectRange != null)
                    {
                        ShockedEffectRange.SetActive(true);
                    }
                    AbilityChosenIndicators[1].enabled = true;
                    IsTrackingCursorVisible = false;
                }
                break;
            case 3:
                if (Player.Ability3State == GrannyAbilityState.Ready)
                {
                    AbilityChosenIndicators[2].enabled = true;
                    IsTrackingCursorVisible = true;
                }
                break;
            default:
                Debug.LogWarning($"GrannyUIManager: Invalid ability index {index}.");
                break;
        }
    }

    private void ShockFailed()
    {
        if (ShockedEffectRange != null)
        {
            ShockedEffectRange.SetActive(false);
        }
    }

    #region Tracking System

    private void PopMissileFailedTag()
    {
        if (MissileCancelledTag != null)
        {
            MissileCancelledTag.SetActive(true);
            StartCoroutine(DisableMissileCancelledTag());
        }
    }

    private IEnumerator DisableMissileCancelledTag()
    {
        yield return new WaitForSeconds(1f);
        MissileCancelledTag.SetActive(false);
    }

    private void UpdateTrackingTarget()
    {
        if (
            trackingCamera == null
            || uiCanvas == null
            || GameManager.Instance._currentState != GameState.Playing
        )
        {
            activeCursor.SetActive(false);
            return;
        }

        List<CapybaraController> capybaras = GameManager.Instance.CapybaraControllers;
        List<CapybaraController> currentCapybarasInView = new List<CapybaraController>();

        foreach (CapybaraController capybara in capybaras)
        {
            if (capybara != null && IsObjectInCameraView(capybara) && capybara.hp > 0)
            {
                if (capybara.stackController)
                {
                    currentCapybarasInView.Add(
                        capybara.stackController.gameObject.GetComponent<CapybaraController>()
                    );
                    break;
                }
                else if (capybara.hp > 0)
                {

                    currentCapybarasInView.Add(capybara);
                }
            }
        }
        if (currentCapybarasInView.Count == 0)
        {
            GameManager.Instance.MissileTarget = null;
            return;
        }
        CapybaraController closestTarget = FindClosestTarget(currentCapybarasInView);
        GameObject currentMissileTarget = GameManager.Instance.MissileTarget;
        capybarasInView = currentCapybarasInView;
        if (closestTarget != null)
        {
            if (GameManager.Instance.MissileTarget == null)
            {
                GameManager.Instance.MissileTarget = closestTarget.gameObject;
            }
            else if (currentMissileTarget != closestTarget.gameObject)
            {
                GameManager.Instance.MissileTarget = closestTarget.gameObject;
            }
        }
    }

    private CapybaraController FindClosestTarget(List<CapybaraController> targets)
    {
        if (targets == null || targets.Count == 0)
            return null;
        if (targets.Count == 1)
            return targets[0];

        float closestDistance = Mathf.Infinity;
        CapybaraController closestCapy = null;

        foreach (CapybaraController capy in targets)
        {
            if (capy != null)
            {
                float distance = Vector3.Distance(transform.position, capy.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestCapy = capy;
                }
            }
        }

        return closestCapy;
    }

    private bool IsObjectInCameraView(CapybaraController capybara)
    {
        if (capybara == null || trackingCamera == null)
            return false;

        bool isInFrustum = false;

        Renderer renderer = capybara.GetComponent<Renderer>();
        if (renderer == null)
        {
            Vector3 screenPoint = trackingCamera.WorldToViewportPoint(capybara.transform.position);
            isInFrustum =
                screenPoint.z > 0
                && screenPoint.x > 0
                && screenPoint.x < 1
                && screenPoint.y > 0
                && screenPoint.y < 1;
        }
        else
        {
            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(trackingCamera);
            isInFrustum = GeometryUtility.TestPlanesAABB(frustumPlanes, renderer.bounds);
        }

        if (!isInFrustum)
            return false;

        Vector3 MissileLaunchPosition = MissileLaunchPoint.position;
        Vector3 targetPosition = capybara.transform.position;
        Vector3 direction = (targetPosition - MissileLaunchPosition).normalized;
        float distance = Vector3.Distance(MissileLaunchPosition, targetPosition);

        RaycastHit hit;
        if (Physics.Raycast(MissileLaunchPosition, direction, out hit, distance))
        {
            Transform hitTransform = hit.transform;

            int hitLayer = hitTransform.gameObject.layer;
            if (IsLayerInMask(hitLayer, targetLayerMask))
            {
                return true;
            }

            if (hitTransform.gameObject == capybara.gameObject)
            {
                return true;
            }
            if (hitTransform.IsChildOf(capybara.transform))
            {
                return true;
            }
            if (capybara.transform.IsChildOf(hitTransform))
            {
                return true;
            }
            return false;
        }

        return true;
    }

    private bool IsLayerInMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    private void UpdateCursorPosition()
    {
        GameObject target = GameManager.Instance.MissileTarget;
        if (target == null)
        {
            trackingCursor.SetActive(false);
            return;
        }

        Vector3 screenPos = trackingCamera.WorldToScreenPoint(target.transform.position);

        if (screenPos.z > 0) // Object is in front of camera
        {
            Vector2 uiPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                uiCanvas.transform as RectTransform,
                screenPos,
                uiCanvas.worldCamera,
                out uiPos
            );

            trackingCursor.transform.localPosition = uiPos;
            trackingCursor.SetActive(true);
        }
        else
        {
            trackingCursor.SetActive(false);
        }
    }
    #endregion

    #region Game Over UI
    private void setGameOverUI()
    {
        int capyDeadCount = GameManager.Instance.CheckCapybaraDeadCount();
        if (capyDeadCount == 0)
        {
            GameOverWinPage.SetActive(false);
            GameOverLosePage.SetActive(true);
        }
        else
        {
            GameOverWinPage.SetActive(true);
            GameOverLosePage.SetActive(false);
            setWiningStars(capyDeadCount);
        }
    }

    private void resetGameOverUI()
    {
        GameOverWinPage.SetActive(false);
        GameOverLosePage.SetActive(false);
    }

    private void setWiningStars(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Instantiate(WiningStarPrefab, WiningStarsContainer.transform);
        }
    }
    #endregion

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Player.OnPlayerDamaged.RemoveListener(UpdatePlayerHP);
        Player.OnAbilityChosen.RemoveListener(updateAbilityChosenStates);
        Player.OnMissileCanceled.RemoveListener(PopMissileFailedTag);
    }
}
