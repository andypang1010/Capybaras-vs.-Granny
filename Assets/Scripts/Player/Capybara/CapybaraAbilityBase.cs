using System.Collections;
using UnityEngine;

public abstract class CapybaraAbilityBase : MonoBehaviour
{
    [Header("Cooldown Settings")]
    public float stackedCD;
    public float regularCD;

    [HideInInspector]
    public float currentCD;

    [HideInInspector]
    public bool isUsingAbility;

    [HideInInspector]
    public float cdTimer;

    [Header("Audio Settings")]
    public AudioClip abilitySFX;

    protected CapybaraController capybaraController;
    protected AudioSource audioSource;
    protected Animator animator;
    protected Rigidbody rb;

    public Coroutine currentAbilityCoroutine;

    protected virtual void Start()
    {
        capybaraController = GetComponent<CapybaraController>();
        audioSource = capybaraController.audioSource;
        animator = capybaraController._animator;
        rb = GetComponent<Rigidbody>();

        cdTimer = 0;
    }

    public virtual void UseAbility()
    {
        if (CanUseAbility())
        {
            cdTimer = currentCD;
        }
    }

    protected bool CanUseAbility()
    {
        return cdTimer <= 0 && !isUsingAbility;
    }

    protected void Update()
    {
        if (cdTimer > 0)
        {
            cdTimer -= Time.deltaTime;
        }
        else if (cdTimer < 0)
        {
            cdTimer = 0;
        }
    }

    public virtual void SetOnStack(bool onStack)
    {
        currentCD = onStack ? stackedCD : regularCD;
    }

    protected virtual IEnumerator AbilityCoroutine()
    {
        yield return null;
    }
}
