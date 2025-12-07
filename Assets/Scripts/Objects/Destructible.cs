using System.Collections;
using System.Linq;
using UnityEngine;

public class Destructible : MonoBehaviour
{
    public float destroyInitialAcceleration = 5f;
    public AudioClip destroyAudio;
    public GameObject wallBreakVFX;
    MeshCollider meshCollider;
    Rigidbody rb;
    AudioSource audioSource;

    void Start()
    {
        meshCollider = GetComponent<MeshCollider>();
        rb = GetComponent<Rigidbody>();

        meshCollider.convex = false;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void OnCollisionStay(Collision collision) {
        if (!rb.isKinematic)
            return;

        if (collision.gameObject.CompareTag("Missile"))
        {
            ExplodeNearbyDestructibles(collision, destroyInitialAcceleration);
            Destroy(collision.gameObject);
        }

        if (collision.transform.root.TryGetComponent(out CapybaraController capybaraController))
        {
            // print($"Destructible hit by Capybara {capybaraController.playerIndex}");
            // Check if capybara is dashing or capybara is in a stack and there exists a capybara in the stack that is dashing
            if (
                capybaraController.stackController != null
                && capybaraController.stackController.capybarasStack.Count > 1
                && (
                    (CapybaraDash)capybaraController.stackController.bottomCapybara.ability
                ).isDashing
            )
            {
                ExplodeNearbyDestructibles(collision, destroyInitialAcceleration);
            }
        }

        if (collision.transform.root.TryGetComponent(out GrannyController grannyController))
        {
            if (grannyController.GetIsDashing())
            {
                ExplodeNearbyDestructibles(collision, destroyInitialAcceleration);
            }
        }
    }

    private void ExplodeNearbyDestructibles(Collision collision, float initialAcceleration)
    {
        audioSource.PlayOneShot(destroyAudio);
        Destroy(Instantiate(wallBreakVFX, collision.GetContact(0).point, Quaternion.identity), 5f);

        // Find all destructibles within a radius of 4 units
        Collider[] hitColliders = Physics.OverlapSphere(
            GetComponent<Collider>().bounds.center,
            4f,
            LayerMask.GetMask("Water")
        );

        // Set all destructibles to non-kinematic
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider != null && hitCollider.TryGetComponent(out Destructible destructible))
            {
                destructible.transform.SetParent(null);
                destructible.rb.isKinematic = false;
                destructible.GetComponent<MeshCollider>().convex = true;
            }
        }

        // Apply force to all destructibles within radius
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider != null && hitCollider.TryGetComponent(out Destructible destructible))
            {
                destructible.rb.AddForce(
                    destructible.rb.mass
                        * initialAcceleration
                        * collision.GetContact(0).normal.normalized,
                    ForceMode.Impulse
                );

                // destructible.rb.AddExplosionForce(
                //     destructible.rb.mass * initialAcceleration,
                //     collision.GetContact(0).point,
                //     6f,
                //     2f,
                //     ForceMode.Impulse
                // );

                destructible.StartCoroutine(DestroyWall(destructible.gameObject));
            }
        }
    }

    IEnumerator DestroyWall(GameObject target)
    {
        yield return new WaitForSeconds(0.1f);

        target.layer = LayerMask.NameToLayer("Destroyed");

        yield return new WaitForSeconds(5f);

        target.GetComponent<Collider>().enabled = false;
        target.GetComponent<Rigidbody>().isKinematic = true;

        // Slowly sink into the ground
        float elapsed = 0f;
        float duration = 12f;
        Vector3 initialPosition = target.transform.position;
        Vector3 targetPosition = initialPosition + Vector3.down * 12f;

        while (elapsed < duration)
        {
            target.transform.position = Vector3.Lerp(
                initialPosition,
                targetPosition,
                elapsed / duration
            );
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(target);
    }
}
