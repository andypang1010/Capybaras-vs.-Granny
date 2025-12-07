using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class GrannyController_Sticker : PlayerController
{
    [Header("Granny Specific Settings")]
    public WheelChairControl WheelChair;
    public float EnergyRegenRate = 1f; // Energy regenerated per second
    public float CurrentEnergy = 0;
    public float MaxEnergy = 3;
    public GameObject ShockRangeVisualizer;
    public AudioSource MissileFiredSFX;
    public AudioSource DashHitCapybaraSFX;

    [Header("Ability Settings")]
    public float DashDamage = 50f;
    public GrannyAbilityState Ability1State = GrannyAbilityState.NotReady; // Dash
    public GrannyAbilityState Ability2State = GrannyAbilityState.NotReady; // Missile
    public GrannyAbilityState Ability3State = GrannyAbilityState.NotReady; // Shock

    public int SelectedAbilityIndex = 0; // 1, 2, or 3
    public bool IsDashing = false;
    public float DashForce = 10f;
    public float ShockDuration = 2f;
    public float ShockRange = 5f; // Radius of shock effect
    public GameObject MissilePrefab;

    [HideInInspector]
    public UnityEvent<int> OnAbilityChosen;

    [HideInInspector]
    public UnityEvent OnMissileCanceled;

    //public UnityEvent<int> OnAbilityReady;

    protected override void Start()
    {
        base.Start();
        ShockRangeVisualizer.transform.localScale = new Vector3(
            ShockRange * 2,
            0.1f,
            ShockRange * 2
        );

        //GameManager.Instance.RegisterGrannyController(this);
    }

    private void OnDisable()
    {
        //GameManager.Instance.UnregisterGrannyController(this);
    }

    protected override void Update()
    {
        if (GameManager.Instance._currentState != GameState.Playing)
            return;
        base.Update();

        IncreaseEnergy();
        HandleAbilityChooseInput();
        if (SelectedAbilityIndex != 0)
            HandleAbilityTriggerInput();
    }

    protected override void FixedUpdate()
    {
        if (GameManager.Instance._currentState != GameState.Playing)
            return;
        base.FixedUpdate();
        WheelChair.WheelChairMovement(InputManager.Instance.GetStickButton2Holded());
    }

    private void IncreaseEnergy()
    {
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
        if (CurrentEnergy >= 1)
        {
            Ability1State = GrannyAbilityState.Ready;
            Ability2State = GrannyAbilityState.Ready;
            if (CurrentEnergy >= 3)
            {
                Ability3State = GrannyAbilityState.Ready;
            }
            else
            {
                Ability3State = GrannyAbilityState.NotReady;
            }
        }
        else
        {
            Ability1State = GrannyAbilityState.NotReady;
            Ability2State = GrannyAbilityState.NotReady;
            Ability3State = GrannyAbilityState.NotReady;
        }
    }

    private void HandleAbilityTriggerInput()
    {
        if (InputManager.Instance.GetStickTriggerPressed()) // Trigger Button
        {
            switch (SelectedAbilityIndex)
            {
                case 1:
                    Dash();
                    break;
                case 2:
                    if (!GameManager.Instance.MissileTarget)
                    {
                        OnMissileCanceled?.Invoke();
                        break;
                    }
                    Missile();
                    break;
                case 3:
                    Shock();
                    break;
                default:
                    break;
            }
        }
    }

    private void HandleAbilityChooseInput()
    {
        if (InputManager.Instance.GetStickButton3Pressed())
        {
            CheckAbilitiesReady();

            if (Ability1State == GrannyAbilityState.Ready)
            {
                SelectedAbilityIndex = 1; // Choose Dash ability
                OnAbilityChosen?.Invoke(1);
                Ability1State = GrannyAbilityState.Chosen;
            }
        }
        if (InputManager.Instance.GetStickButton4Pressed())
        {
            CheckAbilitiesReady();

            if (Ability2State == GrannyAbilityState.Ready)
            {
                SelectedAbilityIndex = 2; // Choose Shock ability
                OnAbilityChosen?.Invoke(2);
                Ability2State = GrannyAbilityState.Chosen;
            }
        }
        if (InputManager.Instance.GetStickButton6Pressed())
        {
            CheckAbilitiesReady();

            if (Ability3State == GrannyAbilityState.Ready)
            {
                SelectedAbilityIndex = 3; // Choose Missile ability
                OnAbilityChosen?.Invoke(3);
                Ability3State = GrannyAbilityState.Chosen;
            }
        }
    }

    public void Dash() // cost 1 energy
    {
        if (CurrentEnergy >= 1 && Ability1State == GrannyAbilityState.Chosen)
        {
            CurrentEnergy -= 1;
            StartCoroutine(KinematicImpulseCoroutine(transform.forward, DashForce, 0.5f));
            Debug.Log("Granny Dash Ability Triggered!");
            SelectedAbilityIndex = 0;
            OnAbilityChosen?.Invoke(0);
        }
        else
        {
            Debug.Log("Not enough energy to perform Dash!");
        }
    }

    private IEnumerator KinematicImpulseCoroutine(
        Vector3 impulseDirection,
        float impulseStrength,
        float duration
    )
    {
        IsDashing = true;
        float elapsed = 0f;

        Vector3 velocity = impulseDirection.normalized * impulseStrength;

        while (elapsed < duration)
        {
            Vector3 step = velocity * Time.fixedDeltaTime * (1f - (elapsed / duration)); // optional: taper off over time
            _rb.MovePosition(_rb.position + step);

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        IsDashing = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (
            IsDashing
            && collision.transform.root.TryGetComponent(out CapybaraController capybaraController)
        )
        {
            Debug.Log(
                $"Granny collided with Capybara {capybaraController.playerIndex} during Dash"
            );
            if (capybaraController.stackController != null)
            {
                // Capybara stack hit
                DashHitCapybaraSFX?.Play();
                capybaraController.stackController.TakeDamage(DashDamage);
                capybaraController
                    .stackController.GetComponent<Rigidbody>()
                    .AddExplosionForce(
                        capybaraController.stackController.GetComponent<Rigidbody>().mass * 10f,
                        transform.position,
                        1f,
                        2f,
                        ForceMode.Impulse
                    );
                print($"Projectile hit Capybara stack, dealing {DashDamage} damage.");
            }
            else
            {
                // Individual capybara hit
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

    public void Missile() // cost 1 energy
    {
        if (CurrentEnergy >= 1 && Ability2State == GrannyAbilityState.Chosen)
        {
            CurrentEnergy -= 1;
            if (MissilePrefab != null)
            {
                MissileFiredSFX?.Play();
                Instantiate(
                    MissilePrefab,
                    transform.position + transform.forward * 2,
                    Quaternion.identity
                );
                print("Granny Missile Ability Triggered!");
            }
            SelectedAbilityIndex = 0;
            OnAbilityChosen?.Invoke(0);
        }
        else
        {
            Debug.Log("Not enough energy to perform Missile! or not chosen!");
        }
    }

    public void Shock() // cost 3 energy
    {
        if (CurrentEnergy >= 3 && Ability3State == GrannyAbilityState.Chosen)
        {
            CurrentEnergy -= 3;
            StartCoroutine(ShockAllCapybarasCoroutine(ShockDuration));

            Debug.Log("Granny Shock Ability Triggered!");
            SelectedAbilityIndex = 0;
            OnAbilityChosen?.Invoke(0);
        }
        else
        {
            Debug.Log("Not enough energy to perform Shock or not chosen!");
        }
    }

    private IEnumerator ShockAllCapybarasCoroutine(float duration)
    {
        List<CapybaraController> capybaras = GameManager.Instance.CapybaraControllers;
        List<CapybaraController> capybarasInRange = new List<CapybaraController>();

        foreach (var capybara in capybaras)
        {
            if (capybara != null)
            {
                float distance = Vector3.Distance(transform.position, capybara.transform.position);
                if (distance <= ShockRange)
                {
                    capybarasInRange.Add(capybara);
                    capybara.IsPaused = true;
                }
            }
        }
        Debug.Log(
            $"{capybarasInRange.Count} capybaras shocked within range {ShockRange} for {duration} seconds"
        );

        yield return new WaitForSeconds(duration);

        foreach (var capybara in capybarasInRange)
        {
            if (capybara != null)
            {
                capybara.IsPaused = false;
            }
        }

        Debug.Log("All affected capybaras restored to normal state");
    }

    public void HittedBySpell()
    {
        float maxSpeed = WheelChair.MaxSpeed;
        WheelChair.MaxSpeed = maxSpeed / 2f;

        StartCoroutine(RestoreSpeedCoroutine(maxSpeed, 6f));

        IEnumerator RestoreSpeedCoroutine(float targetSpeed, float duration)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();

            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.materials)
                {
                    material.color = Color.cyan;
                }
            }

            yield return new WaitForSeconds(duration);
            WheelChair.MaxSpeed = targetSpeed;

            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.materials)
                {
                    material.color = Color.white;
                }
            }
        }
    }
}
