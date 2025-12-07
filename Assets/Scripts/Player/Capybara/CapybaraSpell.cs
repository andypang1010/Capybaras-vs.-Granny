using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.VFX;

public class CapybaraSpell : CapybaraAbilityBase
{
    [Header("Spell Prefab")]
    public GameObject spellPrefab;

    [Header("Lifetime Settings")]
    public float stackedSpellLifetime;
    public float regularSpellLifetime;

    private float currentSpellLifetime;

    [HideInInspector]
    public bool isCastingSpell = false;

    protected override void Start()
    {
        base.Start();

        SetOnStack(false);
    }

    public override void UseAbility()
    {
        if (!CanUseAbility())
            return;

        cdTimer = currentCD;

        audioSource.PlayOneShot(abilitySFX);

        if (currentAbilityCoroutine != null)
            StopCoroutine(currentAbilityCoroutine);

        currentAbilityCoroutine = StartCoroutine(AbilityCoroutine());
    }

    public override void SetOnStack(bool onStack)
    {
        base.SetOnStack(onStack);
        currentSpellLifetime = onStack ? stackedSpellLifetime : regularSpellLifetime;
    }

    protected override IEnumerator AbilityCoroutine()
    {
        animator.SetTrigger("SpellTrigger");
        capybaraController.canMove = false;
        isCastingSpell = true;
        isUsingAbility = true;

        yield return new WaitForSeconds(0f);

        // Instantiate the spell prefab at the capybara's position and rotation
        if (spellPrefab != null)
        {
            Projectile spell = Instantiate(
                    spellPrefab,
                    transform.position + transform.forward * 1.5f,
                    Quaternion.identity
                )
                .GetComponent<Projectile>();

            spell.lifetime = currentSpellLifetime;
            spell.isCapybaraStacked = capybaraController.stackController != null;

            var vfx = spell.GetComponentInChildren<VisualEffect>();
            if (vfx != null)
            {
                vfx.Play();
            }

            var agent = spell.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = false;
            }

            yield return new WaitForSeconds(1f);

            if (agent != null)
            {
                agent.enabled = true;
            }
        }

        yield return new WaitForSeconds(0.5f); // wait for animation to finish

        capybaraController.canMove = true;
        isCastingSpell = false;
        isUsingAbility = false;
    }
}
