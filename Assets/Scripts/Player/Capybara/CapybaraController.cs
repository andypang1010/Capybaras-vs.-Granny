using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.VFX;

public enum CapybaraState
{
    Default,
    Playing,
    Died,
}

public class CapybaraController : PlayerController
{
    [Header("General Settings")]
    public CapybaraState currentState = CapybaraState.Default;
    public GameObject stackHat;
    public CapybaraStackController stackController;
    public CapybaraAbilityBase ability;
    public bool IsPaused = false;

    [Header("Revive Settings")]
    public Material aliveMaterial;
    public Material deadMaterial;
    public int numResuscitatesRequired = 15;
    public float reviveRange = 4f;

    [HideInInspector]
    public int resuscitateCount = 0;

    [HideInInspector]
    public bool canResuscitate = true;

    // === Camera and audio ===

    [Header("Camera Settings")]
    public Camera playerCamera;
    public Vector3 cameraOffset = new Vector3(0, 14, -14);
    public VolumeController volumeController;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioSource walkAudioSource;
    public Collider[] footColliders;
    public AudioClip hurtAudio;
    public AudioClip deadAudio;
    public AudioClip resuscitateAudio;
    public AudioClip reviveAudio;

    [Header("VFX Settings")]
    public VisualEffect runVFX;
    public VisualEffect shockVFX;
    public VisualEffect hittedVFX;
    private SkinnedMeshRenderer skinnedMeshRenderer;
    private Coroutine currentResuscitateCoroutine = null;

    [HideInInspector]
    public UnityEvent<float> OnCapybaraRevived;

    [HideInInspector]
    public UnityEvent<float> OnCapybaraDied;

    [HideInInspector]
    public UnityEvent<float> OnCapybaraResuscitate;

    // --------------------
    // Initialization
    // --------------------
    protected override void Start()
    {
        base.Start();

        audioSource = GetComponent<AudioSource>();
        skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

        runVFX.SetBool("IsRunning", false);
        shockVFX?.Stop();
        hittedVFX?.Stop();

        ToggleRagdoll(hp <= 0f);

        ability = GetComponent<CapybaraAbilityBase>();
    }

    // --------------------
    // Frame update (input, non-physics)
    // --------------------
    protected override void Update()
    {
        if (GameManager.Instance._currentState != GameState.Playing)
            return;

        base.Update();

        if (IsPaused || hp <= 0f)
            return;

        _animator.SetFloat(
            "Speed",
            InputManager.Instance.GetPlayerMovement(playerIndex).magnitude
                * (ability.isUsingAbility ? 0f : 1f)
        );

        runVFX.SetBool(
            "IsRunning",
            canMove && InputManager.Instance.GetPlayerMovement(playerIndex).magnitude > 0f
        );

        HandleInputs();
        CameraFollow();

        stackHat.SetActive(
            stackController != null && this == stackController.capybarasStack.Peek()
        );

        // Add a pulsating effect to the vignette when the player is not at full health, with intensity based on how much HP is missing
        if (hp < maxHP && hp > 0f)
        {
            volumeController.SetVignette(
                Color.red,
                0.2f + 0.05f * (maxHP - hp + 1) + 0.1f * Mathf.Sin(4 * Time.time),
                1f
            );

            volumeController.SetChromaticAberrationIntensity(1f);
        }
    }

    private void HandleInputs()
    {
        if (InputManager.Instance.GetCapybaraReviveDown(playerIndex))
        {
            Resuscitate();
        }

        if (InputManager.Instance.GetCapybaraAbilityDown(playerIndex))
        {
            ability.UseAbility();
        }
    }

    private void CameraFollow()
    {
        playerCamera.transform.position = transform.position + cameraOffset;
    }

    // --------------------
    // Physics update (movement, rotation)
    // --------------------
    protected override void FixedUpdate()
    {
        if (GameManager.Instance._currentState != GameState.Playing)
            return;
        base.FixedUpdate();
        if (IsPaused || hp <= 0f)
            return;

        // if (!isDashing)
        if (!ability.isUsingAbility)
        {
            if (canMove)
            {
                Move();
            }

            Rotate();
        }
    }

    private void Move()
    {
        Vector2 moveVector = Vector2.zero;

        if (playerIndex != 1)
        {
            moveVector = InputManager.Instance.GetPlayerMovement(playerIndex);
        }
        else
        {
            foreach (CapybaraController capybara in stackController.capybarasStack)
            {
                moveVector +=
                    InputManager.Instance.GetPlayerMovement(capybara.playerIndex)
                    * (capybara.ability.isUsingAbility ? 0f : 1f);
            }

            moveVector.Normalize();
        }

        // If there's no input, smoothly damp horizontal velocity to avoid sliding
        if (moveVector == Vector2.zero)
        {
            Vector3 horizontalVel = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            Vector3 damped = Vector3.Lerp(horizontalVel, Vector3.zero, 0.2f);
            _rb.linearVelocity = new Vector3(damped.x, _rb.linearVelocity.y, damped.z);
            return;
        }

        // Compute desired horizontal velocity from input and MoveSpeed
        Vector3 desiredVelocity =
            new Vector3(moveVector.x, 0f, moveVector.y).normalized * MoveSpeed;

        // Preserve current vertical velocity (gravity, jumps, etc.) and apply horizontal velocity
        Vector3 newVelocity = new Vector3(
            desiredVelocity.x,
            _rb.linearVelocity.y,
            desiredVelocity.z
        );

        // Assign to Rigidbody so collisions are handled by physics (prevents clipping/tunneling)
        _rb.linearVelocity = newVelocity;
    }

    private void Rotate()
    {
        Vector2 lookVector = InputManager.Instance.GetPlayerMovement(playerIndex);

        if (lookVector == Vector2.zero)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(
            new Vector3(lookVector.x, 0, lookVector.y)
        );
        _rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, 0.2f));
    }

    // Implement the revive logic
    // While reviving, cannot move. After reviveCD, the revived capybara comes back with 100% HP. While reviving, the target capybara's reviveTimer adds up. If reviveTimer >= reviveCD, revive the capybara. If stops before reviveCD, reset the reviveTimer.
    public void Resuscitate()
    {
        if (!canResuscitate)
            return;

        Collider[] hitColliders = new Collider[100];
        int numCapybarasInRange = Physics.OverlapSphereNonAlloc(
            transform.position + transform.forward,
            reviveRange,
            hitColliders,
            LayerMask.GetMask("Capybara")
        );

        for (int i = 0; i < numCapybarasInRange; i++)
        {
            CapybaraController capybaraController = hitColliders[i]
                .transform.root.GetComponent<CapybaraController>();

            if (capybaraController != null && capybaraController.hp <= 0f)
            {
                // Randomize which resuscitate animation variant the Animator should play.
                // Make sure the Animator has a float parameter named "ResuscitateVariant" and
                // transitions that use it (0 or 0.5 or 1).

                float randomVariant = UnityEngine.Random.Range(0, 3);

                // Ensure the new variant is less likely to be the same as the current one
                if (randomVariant == _animator.GetFloat("ResuscitateVariant"))
                {
                    randomVariant = UnityEngine.Random.Range(0, 3);
                }

                _animator.SetFloat("ResuscitateVariant", randomVariant);
                _animator.SetTrigger("ResuscitateTrigger");

                if (currentResuscitateCoroutine != null)
                {
                    StopCoroutine(currentResuscitateCoroutine);
                }
                currentResuscitateCoroutine = StartCoroutine(
                    ResuscitateCoroutine(capybaraController)
                );

                break; // Only revive one capybara at a time
            }
        }
    }

    // Extracted coroutines to top-level for clarity
    private IEnumerator ResuscitateCoroutine(CapybaraController capybaraToRevive)
    {
        canMove = false;
        canResuscitate = false;

        yield return new WaitForSeconds(0.075f);

        audioSource.pitch = UnityEngine.Random.Range(0.8f, 1.2f);
        audioSource.volume = UnityEngine.Random.Range(0.8f, 1f);
        audioSource.PlayOneShot(resuscitateAudio);
        if (capybaraToRevive.resuscitateCount == 0)
        {
            capybaraToRevive.OnCapybaraResuscitate?.Invoke(0f);
        }
        else
        {
            Debug.Log(
                $"Capybara {capybaraToRevive.playerIndex} resuscitate progress: {(float)capybaraToRevive.resuscitateCount / (float)numResuscitatesRequired}"
            );
            capybaraToRevive.OnCapybaraResuscitate?.Invoke(
                (float)capybaraToRevive.resuscitateCount / (float)numResuscitatesRequired
            );
        }

        foreach (Rigidbody rb in capybaraToRevive.GetComponentsInChildren<Rigidbody>())
        {
            rb.AddForce(
                (rb.transform.position - transform.position) * rb.mass * 0.5f,
                ForceMode.Impulse
            );
        }

        if (_animator.GetFloat("ResuscitateVariant") == 2)
        {
            yield return new WaitForSeconds(0.3f);
            capybaraToRevive.resuscitateCount += 1;
            StartCoroutine(capybaraToRevive.DamageCoroutine(Color.green, 0f));

            if (capybaraToRevive.resuscitateCount >= numResuscitatesRequired)
            {
                Revive(capybaraToRevive);
            }

            yield return new WaitForSeconds(0.325f);
        }
        else
        {
            capybaraToRevive.resuscitateCount += 1;
            capybaraToRevive.StartCoroutine(capybaraToRevive.DamageCoroutine(Color.green, 0f));

            if (capybaraToRevive.resuscitateCount >= numResuscitatesRequired)
            {
                Revive(capybaraToRevive);
            }

            yield return new WaitForSeconds(0.075f);
        }

        canResuscitate = true;
        canMove = true;
    }

    private void Revive(CapybaraController capybaraToRevive)
    {
        capybaraToRevive.audioSource.PlayOneShot(reviveAudio);
        capybaraToRevive.hp = capybaraToRevive.maxHP;
        capybaraToRevive.resuscitateCount = 0;
        capybaraToRevive.ToggleRagdoll(false);

        Debug.Log(
            $"Capybara {capybaraToRevive.playerIndex} has been revived by Capybara {playerIndex}!"
        );
        capybaraToRevive.OnCapybaraRevived?.Invoke(10);
        capybaraToRevive.volumeController.SetVignette(
            volumeController.defaultVignetteSettings.color,
            volumeController.defaultVignetteSettings.intensity,
            volumeController.defaultVignetteSettings.smoothness
        );
        capybaraToRevive.volumeController.SetColorAdjustmentSaturation(
            volumeController.defaultColorAdjustmentSettings.saturation,
            0.5f
        );
        capybaraToRevive.volumeController.SetChromaticAberrationIntensity(
            volumeController.defaultChromaticAberrationSettings.intensity
        );
        GameManager.Instance.JudgeGameOver();
    }

    public override void TakeDamage(float damage)
    {
        if (hp <= 0f)
            return;

        if (hp - damage <= 0f)
        {
            ToggleRagdoll(true);
            volumeController.SetColorAdjustmentSaturation(-100f, 0.5f);
            OnCapybaraDied?.Invoke(-10);
        }
        else
        {
            volumeController.SetVignette(Color.red, 0.2f + 0.05f * (maxHP - hp + 1), 1f);
        }
        hittedVFX?.Play();
        base.TakeDamage(damage);

        GameManager.Instance.JudgeGameOver();
    }

    public void ToggleRagdoll(bool value)
    {
        currentState = value ? CapybaraState.Died : CapybaraState.Playing;

        GetComponent<Collider>().enabled = !value;
        _rb.isKinematic = value;

        canMove = !value;
        _animator.enabled = !value;

        if (value)
        {
            skinnedMeshRenderer.SetMaterials(
                new List<Material> { deadMaterial, skinnedMeshRenderer.materials[1] }
            );
            print($"Set material to: {skinnedMeshRenderer.materials[0].name}");
            audioSource.PlayOneShot(deadAudio);

            if (ability.currentAbilityCoroutine != null)
                StopCoroutine(ability.currentAbilityCoroutine);
        }
        else
        {
            skinnedMeshRenderer.SetMaterials(
                new List<Material> { aliveMaterial, skinnedMeshRenderer.materials[1] }
            );
            GetComponent<Collider>().enabled = false;

            transform.position = _animator.GetComponentInChildren<Collider>().bounds.center;
            _rb.MovePosition(transform.position);

            GetComponent<Collider>().enabled = true;
        }

        foreach (Rigidbody rb in _animator.gameObject.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = !value;
        }

        foreach (Collider col in _animator.gameObject.GetComponentsInChildren<Collider>())
        {
            if (col.isTrigger)
                continue;
            col.enabled = value;
        }

        print($"Capybara {playerIndex} now at position: {transform.position}");
    }

    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);

        if (
            hp > 0f
            && canMove
            && InputManager.Instance.GetPlayerMovement(playerIndex).magnitude > 0f
        )
        {
            walkAudioSource.Play();
        }
    }
}
