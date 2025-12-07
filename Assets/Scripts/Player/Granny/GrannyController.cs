using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.VFX;

public enum GrannyAbilityState
{
    NotReady,
    Ready,
    Chosen,
}

public class GrannyController : PlayerController
{
    [Header("Volume Components")]
    public VolumeController volumeController;

    [Header("Animation Settings")]
    public Animator GunAnimator;

    [Header("Wheel Chair Control Settings")]
    public float defaultMaxSpeed = 10f;
    public float defaultMaxRotationSpeed = 100f;

    [Range(0f, 1f)]
    public float slowDownSpeedMultiplier = 0.5f; // Percentage to reduce speed when hit by spell

    //private CharacterController characterController;
    public List<Transform> Wheels;

    public float currentMaxSpeed;
    public float currentMaxRotationSpeed;

    [Header("Granny Settings")]
    public float EnergyRegenRate = 0.2f; // Energy regenerated per second
    public float CurrentEnergy = 0;
    public float MaxEnergy = 4;
    public GameObject ShockRangeVisualizer;

    [Header("Audio Settings")]
    public AudioSource MissileFiredSFX;
    public AudioSource DashHitCapybaraSFX;

    [Header("Ability Settings")]
    public GrannyAbilityState Ability1State = GrannyAbilityState.NotReady; // Dash
    public GrannyAbilityState Ability2State = GrannyAbilityState.NotReady; // Missile
    public GrannyAbilityState Ability3State = GrannyAbilityState.NotReady; // Shock

    [SerializeField]
    private bool isDashing = false;

    [SerializeField]
    private bool isLaunchingShock;

    [SerializeField]
    private bool isLaunchingMissile;
    public float DashEnergyCost = 1f;
    public float ShockEnergyCost = 2f;
    public float MissileEnergyCost = 3f;
    public float DashDamage = 1f;
    public float DashSpeed = 10f;
    public VisualEffect DashVFX;
    public VisualEffect MoveVFX;
    public VisualEffect ShockBurstVFX;
    public VisualEffect ShockChargeUpVFX;
    public GameObject MissilePrefab;
    public Transform MissileSpawnPoint;
    public float ShockDuration = 5f;
    public float ShockRange = 10f; // Radius of shock effect
    private bool isToShock;
    private bool isShockReady;
    public float ToShockDuration = 2.0f;
    public float ToShockMaxDuration = 3.0f;
    public float ToShockTimer = 0f;
    public Material FreezeMaterial; // Material to apply when frozen

    [HideInInspector]
    public UnityEvent<int> OnAbilityChosen;

    [HideInInspector]
    public UnityEvent OnDashCanceled;

    [HideInInspector]
    public UnityEvent OnMissileCanceled;

    [HideInInspector]
    public UnityEvent OnShockCanceled;

    Coroutine currentFreezeCoroutine;
    private bool isMoveAnimationPlaying = false;
    private int moveAnimationHash;
    private bool isHitBySpell;

    // Track which capybaras have already been damaged during the current dash
    private HashSet<int> dashHitTargets;

    protected override void Start()
    {
        base.Start();
        dashHitTargets = new HashSet<int>();
        DashVFX?.Stop();
        MoveVFX?.Stop();
        ShockBurstVFX?.Stop();
        ShockChargeUpVFX?.Stop();
        if (ShockRangeVisualizer)
            ShockRangeVisualizer.transform.localScale = new Vector3(
                ShockRange * 2,
                0.1f,
                ShockRange * 2
            );
        //characterController = GetComponent<CharacterController>();
        GameManager.Instance.RegisterGrannyController(this);
        MoveVFX?.SetBool("IsMoving", false);

        currentMaxSpeed = defaultMaxSpeed;
        currentMaxRotationSpeed = defaultMaxRotationSpeed;

        // Initialize animation hash for performance
        moveAnimationHash = Animator.StringToHash("TriggerMove");
    }

    private void OnDisable()
    {
        GameManager.Instance.UnregisterGrannyController(this);
    }

    protected override void Update()
    {
        if (GameManager.Instance._currentState != GameState.Playing)
            return;
        base.Update();
        IncreaseEnergy();
        HandleAbilityTriggerInput();

        if (isToShock)
        {
            ToShockTimer += Time.deltaTime;
            if (ToShockTimer >= ToShockDuration)
            {
                isShockReady = true;
                if (ToShockTimer >= ToShockMaxDuration)
                {
                    Shock();
                }
            }
        }

        if (MoveVFX)
        {
            MoveVFX.SetBool(
                "IsMoving",
                InputManager.Instance.GetPlayerMovement(playerIndex).magnitude > 0f
            );
        }
    }

    protected override void FixedUpdate()
    {
        if (GameManager.Instance._currentState != GameState.Playing)
            return;
        base.FixedUpdate();
        if (isDashing || isLaunchingMissile || isLaunchingShock)
            return;
        HandleMovement();
    }

    private void HandleMovement()
    {
        Vector2 inputMovement = InputManager.Instance.GetPlayerMovement(playerIndex);

        if (Mathf.Abs(inputMovement.x) > 0.1f)
        {
            _rb.constraints =
                RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            float rotationAmount = inputMovement.x * currentMaxRotationSpeed * Time.fixedDeltaTime;
            Quaternion deltaRotation = Quaternion.Euler(0, rotationAmount, 0);
            _rb.MoveRotation(_rb.rotation * deltaRotation);
        }
        else
        {
            _rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        Vector3 move = transform.forward * inputMovement.y;
        if (move.magnitude > 1f)
        {
            move.Normalize();
        }
        Vector3 moveVelocity = move * currentMaxSpeed;

        // Use physics-driven linear velocity so collisions are resolved by Rigidbody
        Vector3 desiredVelocity = moveVelocity;

        if (Mathf.Abs(inputMovement.y) > 0.01f)
        {
            // Apply horizontal velocity while preserving vertical velocity (gravity/jumps)
            _rb.linearVelocity = new Vector3(
                desiredVelocity.x,
                _rb.linearVelocity.y,
                desiredVelocity.z
            );
        }
        else
        {
            // Smoothly damp horizontal velocity when there's no input to avoid sliding
            Vector3 horizontalVel = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            Vector3 damped = Vector3.Lerp(horizontalVel, Vector3.zero, 0.2f);
            _rb.linearVelocity = new Vector3(damped.x, _rb.linearVelocity.y, damped.z);
        }

        if (Mathf.Abs(inputMovement.y) > 0.1f && Wheels != null)
        {
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            bool isMoveAnimCurrentlyPlaying =
                stateInfo.IsName("Move") || stateInfo.IsName("TriggerMove");

            if (!isMoveAnimCurrentlyPlaying || stateInfo.normalizedTime >= 1.0f)
            {
                _animator.SetTrigger("TriggerMove");
                isMoveAnimationPlaying = true;
            }

            float wheelRotationSpeed = inputMovement.y * currentMaxSpeed * 50f; // Adjust multiplier as needed
            foreach (Transform wheel in Wheels)
            {
                if (wheel != null)
                {
                    wheel.Rotate(0, 0, -wheelRotationSpeed * Time.fixedDeltaTime, Space.Self);
                }
            }
        }
        else
        {
            isMoveAnimationPlaying = false;
        }
    }

    private void IncreaseEnergy()
    {
        if (isDashing || isLaunchingMissile || isLaunchingShock || isHitBySpell)
            return;
        if (CurrentEnergy > MaxEnergy)
        {
            CurrentEnergy = MaxEnergy;
            return;
        }
        else if (CurrentEnergy == MaxEnergy)
        {
            return;
        }

        CurrentEnergy += EnergyRegenRate * Time.deltaTime;
    }

    private void CheckAbilitiesReady()
    {
        Debug.Log($"Checking abilities readiness with CurrentEnergy: {CurrentEnergy}");
        if (CurrentEnergy >= DashEnergyCost)
        {
            Ability1State = GrannyAbilityState.Ready;
        }
        else
        {
            Ability1State = GrannyAbilityState.NotReady;
        }
        if (CurrentEnergy >= ShockEnergyCost)
        {
            Ability2State = GrannyAbilityState.Ready;
        }
        else
        {
            Ability2State = GrannyAbilityState.NotReady;
        }
        if (CurrentEnergy >= MissileEnergyCost)
        {
            Ability3State = GrannyAbilityState.Ready;
        }
        else
        {
            Ability3State = GrannyAbilityState.NotReady;
        }
    }

    private void HandleAbilityTriggerInput()
    {
        if (isDashing || isLaunchingMissile || isLaunchingShock)
            return;

        ButtonState southButtonState = InputManager.Instance.GetSouthButtonDownGranny();
        switch (southButtonState)
        {
            case ButtonState.Pressed:
                CheckAbilitiesReady();
                if (Ability1State == GrannyAbilityState.Ready)
                {
                    OnAbilityChosen?.Invoke(1);
                    return;
                }
                break;
            case ButtonState.Released:
                if (Ability1State == GrannyAbilityState.Ready)
                {
                    Dash();
                }
                OnAbilityChosen?.Invoke(0);
                return;
            default:
                break;
        }

        ButtonState westButtonState = InputManager.Instance.GetWestButtonDownGranny();
        switch (westButtonState)
        {
            case ButtonState.Pressed:
                CheckAbilitiesReady();
                if (Ability2State == GrannyAbilityState.Ready)
                {
                    CheckAbilitiesReady();
                    _animator.SetTrigger("TriggerToShock");
                    isLaunchingShock = true;
                    if (!isToShock)
                    {
                        isToShock = true;
                        ToShockTimer = 0f;
                        _animator.SetBool("IsHoldingShock", isToShock);
                        ShockChargeUpVFX.SetBool("IsCharging", true);
                        ShockChargeUpVFX.Play();
                    }
                    OnAbilityChosen?.Invoke(2);
                    return;
                }
                break;
            case ButtonState.Released:
                if (Ability2State == GrannyAbilityState.Ready)
                {
                    Shock();
                }
                else
                {
                    _animator.SetBool("IsHoldingShock", false);
                }
                isShockReady = false;
                isToShock = false;
                ToShockTimer = 0f;
                isLaunchingShock = false;
                OnAbilityChosen?.Invoke(0);
                return;
            default:
                break;
        }

        ButtonState northButtonState = InputManager.Instance.GetNorthButtonDownGranny();
        switch (northButtonState)
        {
            case ButtonState.Pressed:
                CheckAbilitiesReady();
                if (Ability3State == GrannyAbilityState.Ready)
                {
                    OnAbilityChosen?.Invoke(3);
                    return;
                }
                break;
            case ButtonState.Released:
                if (Ability3State == GrannyAbilityState.Ready)
                {
                    Missile();
                }
                OnAbilityChosen?.Invoke(0);
                return;
            default:
                break;
        }
    }

    private IEnumerator playShockVFX(float duration)
    {
        yield return new WaitForSeconds(duration);
        ShockChargeUpVFX.SetBool("IsCharging", false);
        ShockBurstVFX.Play();
        ShockBurstVFX.SetFloat("TimeVar", 0f);
        StartCoroutine(AnimateChargeAmount(1f, 0f, 0.5f));
    }

    private IEnumerator AnimateChargeAmount(float duration, float startValue, float endValue)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float currentValue = Mathf.Lerp(startValue, endValue, t);
            ShockBurstVFX.SetFloat("TimeVar", currentValue);
            yield return null;
        }
        ShockBurstVFX.SetFloat("TimeVar", endValue);
    }

    public void Dash() // cost 1 energy
    {
        if (CurrentEnergy >= DashEnergyCost)
        {
            CurrentEnergy -= DashEnergyCost;
            if (dashHitTargets != null)
                dashHitTargets.Clear();
            StartCoroutine(KinematicDashCoroutine(DashSpeed));
            Debug.Log("Granny Dash Ability Triggered!");
        }
        else
        {
            Debug.Log("Not enough energy to perform Dash!");
            OnDashCanceled?.Invoke();
        }
    }

    private IEnumerator KinematicDashCoroutine(float DashSpeed)
    {
        isDashing = true;
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
        _animator.SetTrigger("TriggerBump");
        yield return new WaitForSeconds(0.36f);
        DashVFX.Play();

        GetComponent<SphereCollider>().enabled = true;

        // _rb.AddForce(impulseStrength * transform.forward, ForceMode.Impulse);

        // Compute initial horizontal velocity for the dash (preserve current vertical velocity)
        Vector3 initialHorizontal = transform.forward * DashSpeed;
        _rb.linearVelocity = new Vector3(
            initialHorizontal.x,
            _rb.linearVelocity.y,
            initialHorizontal.z
        );

        float elapsed = 0f;
        float duration = 0.5f;

        // Gradually lerp horizontal velocity to zero over the dash duration.
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 targetHorizontal = Vector3.Lerp(initialHorizontal, Vector3.zero, t);

            _rb.linearVelocity = new Vector3(
                targetHorizontal.x,
                _rb.linearVelocity.y,
                targetHorizontal.z
            );

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // Ensure horizontal velocity is zero at the end of the dash
        _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);

        GetComponent<SphereCollider>().enabled = false;
        _rb.constraints =
            RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        _rb.angularVelocity = Vector3.zero;
        yield return new WaitForSeconds(0.5f);
        isDashing = false;
    }

    public void Missile() // cost 3 energy
    {
        GameObject missileTarget = GameManager.Instance.MissileTarget;
        if (!missileTarget || missileTarget.GetComponent<CapybaraController>()?.hp <= 0f)
        {
            GameManager.Instance.MissileTarget = null;
            OnMissileCanceled?.Invoke();
            return;
        }
        if (CurrentEnergy >= MissileEnergyCost)
        {
            CurrentEnergy -= MissileEnergyCost;
            StartCoroutine(launchMissileCoroutine(0.5f));
            // SelectedAbilityIndex = 0;
        }
        else
        {
            Debug.Log("Not enough energy to perform Missile! or not chosen!");
        }
    }

    private IEnumerator launchMissileCoroutine(float duration)
    {
        isLaunchingMissile = true;
        yield return new WaitForSeconds(duration); // Assume missile launch takes 1 second

        if (MissilePrefab != null)
        {
            MissileFiredSFX?.Play();
            Instantiate(MissilePrefab, MissileSpawnPoint.position, Quaternion.identity);
            GunAnimator.SetTrigger("FireTrigger");
            print("Granny Missile Ability Triggered!");
        }
        isLaunchingMissile = false;
    }

    public void Shock() // cost 2 energy
    {
        if (isShockReady && CurrentEnergy >= ShockEnergyCost)
        {
            CurrentEnergy -= ShockEnergyCost;
            _animator.SetTrigger("TriggerShock");
            _animator.SetBool("IsHoldingShock", false);
            StartCoroutine(playShockVFX(0.8f));
            StartCoroutine(ShockAllCapybarasCoroutine(0.14f, ShockDuration));
            //playShockVFX(1f);
            Debug.Log("Granny Shock Ability Triggered!");
        }
        else
        {
            OnShockCanceled?.Invoke();
            Debug.Log("Not enough energy to perform Shock or not chosen!");
        }
        isShockReady = false;
        isToShock = false;
        ToShockTimer = 0f;
        isLaunchingShock = false;
        OnAbilityChosen?.Invoke(0);
    }

    private IEnumerator ShockAllCapybarasCoroutine(float animationDuration, float shockDuration)
    {
        yield return new WaitForSeconds(animationDuration);
        List<CapybaraController> capybaras = GameManager.Instance.CapybaraControllers;
        List<CapybaraController> capybarasInRange = new List<CapybaraController>();

        foreach (var capybara in capybaras)
        {
            if (capybara)
            {
                float distance = Vector3.Distance(transform.position, capybara.transform.position);
                if (distance <= ShockRange)
                {
                    capybarasInRange.Add(capybara);
                    capybara.IsPaused = true;
                    capybara.shockVFX.Play();
                }
            }

            foreach (var capy in capybarasInRange)
            {
                capy._animator.SetFloat("Speed", 0f); // Freeze animation
                if (capy.stackController && capy.stackController.gameObject == capybara.gameObject)
                {
                    capy.stackController.DestroyStack();
                }
            }
        }
        Debug.Log(
            $"{capybarasInRange.Count} capybaras shocked within range {ShockRange} for {shockDuration} seconds"
        );

        yield return new WaitForSeconds(shockDuration);

        foreach (var capybara in capybarasInRange)
        {
            if (capybara != null)
            {
                capybara.IsPaused = false;
            }
        }

        Debug.Log("All affected capybaras restored to normal state");
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        base.OnCollisionEnter(collision);
    }

    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);

        if (
            isDashing
            && other.transform.root.TryGetComponent(out CapybaraController capybaraController)
        )
        {
            // Prevent dealing damage more than once to the same capybara during one dash
            int targetIndex = capybaraController.playerIndex;
            if (dashHitTargets != null && dashHitTargets.Contains(targetIndex))
            {
                return;
            }
            dashHitTargets?.Add(targetIndex);

            Debug.Log(
                $"Granny collided with Capybara {capybaraController.playerIndex} during Dash"
            );
            if (capybaraController.stackController != null)
            {
                foreach (var capy in capybaraController.stackController.capybarasStack)
                {
                    dashHitTargets?.Add(capy.playerIndex);
                }
                // Capybara stack hit
                DashHitCapybaraSFX?.Play();
                if (
                    capybaraController.stackController.capybarasStack.Any(capy =>
                        capy.TryGetComponent(out CapybaraShield shield) && shield.isParrying
                    )
                )
                    return;

                capybaraController
                    .stackController.GetComponent<Rigidbody>()
                    .AddExplosionForce(
                        capybaraController.stackController.GetComponent<Rigidbody>().mass * 10f,
                        transform.position,
                        1f,
                        2f,
                        ForceMode.Impulse
                    );
                capybaraController.stackController.TakeDamage(DashDamage);
            }
            else
            {
                // Individual capybara hit
                DashHitCapybaraSFX?.Play();
                if (
                    capybaraController.TryGetComponent(out CapybaraShield shield)
                    && shield.isParrying
                )
                    return;

                capybaraController.TakeDamage(DashDamage);
                capybaraController
                    .GetComponent<Rigidbody>()
                    .AddExplosionForce(
                        capybaraController.GetComponent<Rigidbody>().mass * 10f,
                        transform.position,
                        1f,
                        2f,
                        ForceMode.Impulse
                    );
                print($"Projectile hit Capybara, dealing {DashDamage} damage.");
            }
        }
    }

    public bool GetIsDashing()
    {
        return isDashing;
    }

    public void HitBySpell(bool isCapybaraStacked)
    {
        if (isCapybaraStacked)
        {
            if (currentFreezeCoroutine != null)
            {
                StopCoroutine(currentFreezeCoroutine);
            }

            // Freeze granny for 3 seconds
            currentFreezeCoroutine = StartCoroutine(FreezeCoroutine(0f, 1.5f));
        }
        else
        {
            if (currentFreezeCoroutine != null)
            {
                StopCoroutine(currentFreezeCoroutine);
            }

            // Reduce granny speed by FreezeSpeedRate for 3 seconds
            currentFreezeCoroutine = StartCoroutine(FreezeCoroutine(slowDownSpeedMultiplier, 3f));
        }

        IEnumerator FreezeCoroutine(float speedMultiplier, float duration)
        {
            isHitBySpell = speedMultiplier == 0f;

            currentMaxSpeed = defaultMaxSpeed * speedMultiplier;
            currentMaxRotationSpeed = defaultMaxRotationSpeed * speedMultiplier;

            volumeController.SetColorAdjustmentFilter(new Color(0f, 100f / 255f, 1f));
            Renderer[] renderers = GetComponentsInChildren<Renderer>();

            // Keep original materials per-renderer so we can restore them exactly as they were.
            List<Material[]> originalMaterials = new List<Material[]>();
            // If FreezeMaterial is not assigned, fall back to storing/restoring colors per-material.
            List<Color[]> originalColors = new List<Color[]>();
            bool useFreezeMaterial = FreezeMaterial != null;

            for (int r = 0; r < renderers.Length; r++)
            {
                var renderer = renderers[r];
                if (renderer == null)
                {
                    originalMaterials.Add(null);
                    originalColors.Add(null);
                    continue;
                }

                // Store a copy of the original materials array
                var mats = renderer.materials;
                originalMaterials.Add(mats);

                if (useFreezeMaterial)
                {
                    // Create new instances of the FreezeMaterial for each material slot to avoid modifying the shared asset
                    Material[] freezeMats = new Material[mats.Length];
                    for (int i = 0; i < freezeMats.Length; i++)
                    {
                        freezeMats[i] = new Material(FreezeMaterial);
                    }
                    renderer.materials = freezeMats;
                    originalColors.Add(null);
                }
                else
                {
                    // Fallback: store original colors for each material and tint them cyan
                    Color[] cols = new Color[mats.Length];
                    for (int i = 0; i < mats.Length; i++)
                    {
                        cols[i] = mats[i].color;
                        mats[i].color = Color.cyan;
                    }
                    originalColors.Add(cols);
                    // materials remain the same (we modified color properties)
                }
            }

            yield return new WaitForSeconds(duration);

            // Restore speeds
            currentMaxSpeed = defaultMaxSpeed;
            currentMaxRotationSpeed = defaultMaxRotationSpeed;

            // Restore original materials or colors
            volumeController.SetColorAdjustmentFilter(new Color(1f, 1f, 1f));
            for (int r = 0; r < renderers.Length; r++)
            {
                var renderer = renderers[r];
                if (renderer == null)
                    continue;

                if (useFreezeMaterial)
                {
                    var orig = originalMaterials[r];
                    if (orig != null)
                        renderer.materials = orig;
                }
                else
                {
                    var cols = originalColors[r];
                    if (cols == null)
                        continue;

                    var mats = renderer.materials;
                    for (int i = 0; i < mats.Length && i < cols.Length; i++)
                    {
                        mats[i].color = cols[i];
                    }
                }
            }

            isHitBySpell = false;
        }
    }
}
