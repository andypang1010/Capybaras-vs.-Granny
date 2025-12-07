using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class CapybaraDash : CapybaraAbilityBase
{
    [Header("Speed Settings")]
    public float stackedDashSpeed;
    public float regularDashSpeed;

    [HideInInspector]
    public float currentDashSpeed;

    [Header("VFX Settings")]
    public VisualEffect dashVFX;

    [HideInInspector]
    public bool isDashing = false;

    [Header("Trail Settings")]
    public float meshRefreshRate = 0.1f;
    public float meshDestroyDelay = 0.5f;
    public Transform trailSpawnPoint;
    public Material dashMaterial;
    public string shaderFloatName = "_Alpha";
    public float shaderRate = 0.1f;
    public float shaderRefreshRate = -0.05f;
    private SkinnedMeshRenderer[] skinnedMeshRenderers;

    protected override void Start()
    {
        base.Start();
        dashVFX?.Stop();

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
        currentDashSpeed = onStack ? stackedDashSpeed : regularDashSpeed;
    }

    protected override IEnumerator AbilityCoroutine()
    {
        animator.SetTrigger("DashTrigger");
        isDashing = true;
        capybaraController.canMove = false;
        isUsingAbility = true;

        yield return new WaitForSeconds(0.17f); // small delay before starting the dash

        dashVFX.Play();

        // Compute initial horizontal velocity for the dash (preserve current vertical velocity)
        Vector3 initialHorizontal = transform.forward * currentDashSpeed;
        rb.linearVelocity = new Vector3(
            initialHorizontal.x,
            rb.linearVelocity.y,
            initialHorizontal.z
        );

        StartCoroutine(ActivateTrail(0.875f));

        float elapsed = 0f;
        float duration = 0.875f;

        // Gradually lerp horizontal velocity to zero over the dash duration.
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 targetHorizontal = Vector3.Lerp(initialHorizontal, Vector3.zero, t);

            rb.linearVelocity = new Vector3(
                targetHorizontal.x,
                rb.linearVelocity.y,
                targetHorizontal.z
            );

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // Ensure horizontal velocity is zero at the end of the dash
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

        isDashing = false;
        capybaraController.canMove = true;
        isUsingAbility = false;
    }

    #region Trail Generation
    private IEnumerator ActivateTrail(float timeActive)
    {
        while (timeActive > 0)
        {
            timeActive -= meshRefreshRate;

            if (skinnedMeshRenderers == null)
                skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();

            for (int i = 0; i < skinnedMeshRenderers.Length; i++)
            {
                GameObject gObj = new GameObject();
                gObj.transform.SetPositionAndRotation(
                    trailSpawnPoint.position,
                    trailSpawnPoint.rotation
                );

                MeshRenderer mr = gObj.AddComponent<MeshRenderer>();
                MeshFilter mf = gObj.AddComponent<MeshFilter>();

                Mesh mesh = new Mesh();
                skinnedMeshRenderers[i].BakeMesh(mesh);

                mf.mesh = mesh;
                mr.material = dashMaterial;

                StartCoroutine(AnimateMaterialFloat(mr.material, 0, shaderRate, shaderRefreshRate));
                Destroy(gObj, meshDestroyDelay);
            }

            yield return new WaitForSeconds(meshRefreshRate);
        }
    }

    private IEnumerator AnimateMaterialFloat(
        Material mat,
        float targetValue,
        float rate,
        float refreshRate
    )
    {
        float animatedFloat = mat.GetFloat(shaderFloatName);

        while (animatedFloat > targetValue)
        {
            animatedFloat += rate;
            mat.SetFloat(shaderFloatName, animatedFloat);
            print(animatedFloat + "\n");
            yield return new WaitForSeconds(refreshRate);
        }
    }
    #endregion
}
