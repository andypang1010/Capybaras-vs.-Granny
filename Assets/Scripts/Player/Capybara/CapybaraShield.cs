using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class CapybaraShield : CapybaraAbilityBase
{
    [Header("Range Settings")]
    public float stackedParryRange;
    public float regularParryRange;

    [HideInInspector]
    public float currentParryRange;

    [HideInInspector]
    public bool isParrying = false;

    [Header("VFX Settings")]
    public GameObject parrySphere;
    VisualEffect parryVFX;

    protected override void Start()
    {
        base.Start();

        parrySphere.SetActive(false);
        parryVFX = parrySphere.GetComponent<VisualEffect>();
        parryVFX?.Stop();

        SetOnStack(false);
    }

    // Update is called once per frame
    public override void UseAbility()
    {
        if (!CanUseAbility())
            return;

        cdTimer = currentCD;

        if (currentAbilityCoroutine != null)
            StopCoroutine(currentAbilityCoroutine);

        currentAbilityCoroutine = StartCoroutine(AbilityCoroutine());
    }

    public override void SetOnStack(bool onStack)
    {
        base.SetOnStack(onStack);
        currentParryRange = onStack ? stackedParryRange : regularParryRange;
        parryVFX.SetBool("IsStacked", onStack);
    }

    protected override IEnumerator AbilityCoroutine()
    {
        isParrying = true;
        isUsingAbility = true;
        capybaraController.canMove = false;
        animator.SetTrigger("ParryTrigger");
        yield return new WaitForSeconds(0.35f);

        audioSource.PlayOneShot(abilitySFX);
        parrySphere.SetActive(true);
        parryVFX.Play();
        yield return new WaitForSeconds(0.35f);

        float startTime = Time.time;

        while (Time.time - startTime < 1.625f)
        {
            // buffer for overlap results
            Collider[] hitColliders = new Collider[10];
            int grannyLayerMask = LayerMask.GetMask("Granny");

            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                currentParryRange,
                hitColliders,
                grannyLayerMask
            );

            if (hitCount > 0)
            {
                for (int i = 0; i < hitCount; i++)
                {
                    Collider hit = hitColliders[i];
                    if (hit == null)
                        continue;

                    if (hit.CompareTag("Missile"))
                    {
                        parryVFX.SetVector3("HitPosition", hit.transform.position);
                        hit.gameObject.GetComponent<Projectile>()
                            .MissileExploded(hit.transform.position);

                        break; // Only parry one missile at a time
                    }
                    else
                    {
                        Transform root = hit.transform.root;
                        print("Parried object: " + root.name);
                        if (root != null && root.CompareTag("Granny"))
                        {
                            print($"Parried granny: {root.name}");
                            if (
                                root.TryGetComponent(out GrannyController grannyController)
                                && grannyController.GetIsDashing()
                            )
                            {
                                parryVFX.SetVector3("HitPosition", hit.transform.position);
                                Rigidbody grannyRB = grannyController.GetComponent<Rigidbody>();
                                print($"Parried granny rigidbody: {grannyRB.name}");
                                Vector3 forceDirection =
                                    grannyRB.transform.position - transform.position;
                                forceDirection.y = 0f;
                                forceDirection.Normalize();
                                grannyRB.AddForce(
                                    grannyRB.mass * 5f * forceDirection,
                                    ForceMode.Impulse
                                );

                                break; // Only parry one granny at a time
                            }
                        }
                    }
                }
            }

            yield return null; // wait for next frame
        }

        // Clear state
        isParrying = false;
        isUsingAbility = false;
        capybaraController.canMove = true;
        parrySphere.SetActive(false);
        currentAbilityCoroutine = null;
    }
}
