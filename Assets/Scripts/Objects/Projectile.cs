using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class Projectile : MonoBehaviour
{
    public bool isCapybaraProjectile = true;
    public bool isCapybaraStacked = false;
    public GameObject hitEffectPrefab;
    public GameObject debuffEffectPrefab;
    public float damage;
    public float speed;
    public float lifetime;

    public GameObject target;
    float timer = 0f;
    NavMeshAgent agent;
    Rigidbody rb;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        if (agent == null)
            Debug.LogWarning("ProjectileMovement expects a NavMeshAgent on the same GameObject.");

        if (rb == null)
        {
            Debug.LogWarning(
                "ProjectileMovement expects a Rigidbody on the same GameObject. Adding one automatically."
            );
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Use agent for pathfinding only; we'll move the Rigidbody so physics collisions work
        if (agent != null)
        {
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.speed = speed;
        }

        // Configure rigidbody for fast-moving projectiles
        rb.isKinematic = false;
        rb.useGravity = false; // keep projectile on navmesh plane; change if you want gravity
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (target == null)
        {
            target = FindClosestTarget();
            // Destroy(gameObject);
        }

        StartCoroutine(BuildNavMeshCoroutine());

        IEnumerator BuildNavMeshCoroutine()
        {
            if (GameObject.Find("ENVIRONMENT").TryGetComponent(out NavMeshSurface surface))
            {
                surface.BuildNavMesh();
            }

            yield return null;
        }
    }

    void Update()
    {
        // Let the agent compute a path and steering; movement happens in FixedUpdate via Rigidbody
        if (agent != null && target != null && agent.enabled)
            if (!agent.SetDestination(target.transform.position))
            {
                // find nearest point on navmesh to target
                NavMeshHit hit;
                if (
                    NavMesh.SamplePosition(
                        target.transform.position,
                        out hit,
                        10.0f,
                        NavMesh.AllAreas
                    )
                )
                {
                    agent.SetDestination(hit.position);
                }
            }
        transform.rotation = Quaternion.LookRotation(
            (target.transform.position - transform.position).normalized
        );

        if (timer > lifetime)
        {
            Destroy(gameObject);
            return;
        }

        timer += Time.deltaTime;
    }

    void FixedUpdate()
    {
        if (rb == null)
            return;
        if (target == null)
            return;

        // Preferred position computed by the agent
        Vector3 desired = (agent != null) ? agent.nextPosition : target.transform.position;

        // If we don't have an agent, just move toward the target
        if (agent == null)
            desired = target.transform.position;

        // Move the rigidbody towards the desired position using MovePosition so physics sees collisions
        Vector3 toTarget = desired - rb.position;
        Vector3 move = Vector3.MoveTowards(
            rb.position,
            rb.position + toTarget,
            speed * Time.fixedDeltaTime
        );
        rb.MovePosition(move);

        // Keep the agent in sync with the Rigidbody so it can continue pathfinding
        if (agent != null)
            agent.nextPosition = rb.position;
    }

    public GameObject FindClosestTarget()
    {
        if (isCapybaraProjectile)
        {
            GameObject[] grannys = GameObject.FindGameObjectsWithTag("Granny");

            float closestDistance = Mathf.Infinity;
            GameObject closestGranny = null;

            foreach (GameObject granny in grannys)
            {
                float distance = Vector3.Distance(transform.position, granny.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestGranny = granny;
                }
            }
            return closestGranny;
        }

        else
        {
            if (GameManager.Instance.MissileTarget != null)
            {
                Debug.Log("Using assigned Missile Target from Game Manager");
                return GameManager.Instance.MissileTarget;
            }
            Debug.Log("Not Using assigned Missile Target from Game Manager");
            GameObject closestCapy = GameManager.Instance.MissileTarget;
            // print("Closest Capybara's: " + closestCapy.name);

            if (closestCapy == null)
            {
                float closestDistance = Mathf.Infinity;

                foreach (var capy in GameManager.Instance.CapybaraControllers)
                {
                    if (capy != null)
                    {
                        float distance = Vector3.Distance(
                            transform.position,
                            capy.transform.position
                        );
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestCapy = capy.gameObject;
                        }
                    }
                }
            }

            return closestCapy;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Projectile from Capybara hitting Granny
        if (isCapybaraProjectile)
        {
            if (collision.transform.root.TryGetComponent(out GrannyController grannyController))
            {
                // Instantiate hit effect and destroy after 5 seconds
                Destroy(
                    Instantiate(
                        hitEffectPrefab,
                        collision.GetContact(0).point,
                        Quaternion.identity
                    ),
                    5f
                );

                if (isCapybaraStacked)
                {
                    GameObject debuffEffect = Instantiate(
                        debuffEffectPrefab,
                        collision.transform.root.position,
                        Quaternion.identity
                    );

                    debuffEffect.transform.localScale *= 2f;

                    Destroy(debuffEffect, 5f);
                }

                grannyController.HitBySpell(isCapybaraStacked);
                // grannyController.GetComponent<Rigidbody>().AddExplosionForce(grannyController.GetComponent<Rigidbody>().mass * 15f, transform.position, 1f, 2f, ForceMode.Impulse);
                print($"Projectile hit Granny, dealing {damage} damage.");
                Destroy(gameObject);
                return;
            }
        }
        // Projectile from Granny hitting Capybara individual or stack
        else
        {
            if (collision.transform.root.TryGetComponent(out CapybaraController capybaraController))
            {
                // if the capybara or any in its stack are parrying, do nothing
                if (
                    capybaraController.TryGetComponent(out CapybaraShield shield)
                        && shield.isParrying
                    || (
                        capybaraController.stackController != null
                        && capybaraController.stackController.capybarasStack.Any(capybara =>
                            capybara.TryGetComponent(out CapybaraShield stackShield)
                            && stackShield.isParrying
                        )
                    )
                )
                    return;
                // Apply damage to capybara or capybara stack
                else
                {
                    List<Rigidbody> rigidbodies = new List<Rigidbody>();

                    // Check if the capybara hit belongs to a stack
                    if (capybaraController.stackController != null)
                    {
                        // Capybara stack hit
                        capybaraController.stackController.TakeDamage(damage, false);

                        // rigidbodies is set to the rigidbodies of all capybaras in the stack
                        foreach (
                            CapybaraController capy in capybaraController
                                .stackController
                                .capybarasStack
                        )
                        {
                            rigidbodies.AddRange(capy.GetComponentsInChildren<Rigidbody>());
                        }
                    }
                    else
                    {
                        // Individual capybara hit
                        capybaraController.TakeDamage(damage);

                        rigidbodies.AddRange(
                            capybaraController.GetComponentsInChildren<Rigidbody>()
                        );
                    }

                    foreach (Rigidbody rb in rigidbodies)
                    {
                        rb.AddForce(
                            (-collision.GetContact(0).normal.normalized + Vector3.up).normalized
                                * (rb.mass * 20f),
                            ForceMode.Impulse
                        );
                    }

                    // Instantiate hit effect and destroy after 5 seconds
                    MissileExploded(collision.GetContact(0).point);
                }
            }
        }
    }

    public void MissileExploded(Vector3 explosionPosition)
    {
        Destroy(Instantiate(hitEffectPrefab, explosionPosition, Quaternion.identity), 5f);
        Destroy(gameObject);
    }
}
