using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CapybaraStackController : MonoBehaviour
{
    [Header("Stack Settings")]
    public float stackHP;
    public float stackOffsetY = 3f;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip addCapybaraAudio;

    [Header("Capybara References")]
    public CapybaraController[] capybaraArray;
    public Stack<CapybaraController> capybarasStack = new Stack<CapybaraController>(3);

    [HideInInspector]
    public CapybaraController bottomCapybara;

    void Start()
    {
        bottomCapybara = GetComponent<CapybaraController>();
        capybarasStack.Push(bottomCapybara);
        bottomCapybara.canMove = true;
        bottomCapybara.stackController = this;
    }

    void Update()
    {
        UpdateStackHP();
        UpdateStackPosition();

        capybaraArray = capybarasStack.ToArray().Reverse().ToArray();
    }

    void UpdateStackHP()
    {
        stackHP = capybarasStack.Aggregate(0f, (acc, capybara) => acc + capybara.hp);
    }

    void UpdateStackPosition()
    {
        for (int i = 1; i < capybaraArray.Length; i++)
        {
            capybaraArray[i].transform.position = (
                capybaraArray[i - 1]
                    .transform.Find("Model/riggedCapybara/spine/spine.001/spine.002/spine.004")
                    .position
                + Vector3.up * stackOffsetY
            );

            capybaraArray[i].canMove = false;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.root.TryGetComponent(out CapybaraController capybaraController))
        {
            if (
                !capybarasStack.Contains(capybaraController)
                && capybaraController.hp > 0f
                && GetComponent<CapybaraController>().hp > 0f
            )
            {
                AddCapybara(capybaraController);
            }
        }
    }

    void AddCapybara(CapybaraController newCapybara)
    {
        bottomCapybara.canMove = false;

        newCapybara.canMove = false;
        newCapybara.GetComponent<Rigidbody>().isKinematic = true;
        newCapybara.GetComponent<Collider>().enabled = false;

        audioSource.PlayOneShot(addCapybaraAudio);
        newCapybara._animator.SetTrigger("StackTrigger");
        Vector3 newCapybaraStartPos = newCapybara.transform.position;

        StartCoroutine(AddCapybaraCoroutine());

        IEnumerator AddCapybaraCoroutine()
        {
            yield return new WaitForSeconds(0.32f); // wait for the stacking animation to play a bit

            // Improved parabolic arc that avoids clipping into the stack by:
            // - raising the mid point based on current stack height (clearance)
            // - approaching from a small lateral offset so the capybara doesn't intersect the column
            // - applying easing so the motion has a strong start burst and a quick settle at the end

            Vector3 startPos = newCapybara.transform.position;
            Vector3 baseTargetPos =
                capybarasStack
                    .Peek()
                    .transform.Find("Model/riggedCapybara/spine/spine.001/spine.002/spine.004")
                    .position
                + Vector3.up * stackOffsetY;

            // Give extra clearance based on how tall the stack already is
            float clearance = Mathf.Max(2f, 1.5f * capybarasStack.Count + 1f);

            // Lateral offset to approach from the side rather than intersecting the vertical column
            Vector3 approachDir = startPos - baseTargetPos;
            approachDir.y = 0f;
            if (approachDir.sqrMagnitude < 0.01f)
            {
                // If start is nearly above target, pick a perpendicular direction to the world's forward
                approachDir = Vector3.Cross(Vector3.up, transform.forward).normalized;
            }
            else
            {
                approachDir = approachDir.normalized;
            }

            float lateralOffset = Mathf.Clamp(1.0f + capybarasStack.Count * 0.25f, 0.8f, 2.5f);
            Vector3 offsetTargetPos = baseTargetPos + approachDir * lateralOffset;

            float elapsedTime = 0f;
            float duration = 0.63f; // a touch faster than before for snappier feel

            // Midpoint lifted by clearance and pulled slightly towards the offset target so arc clears the stack
            Vector3 midPoint = (startPos + offsetTargetPos) * 0.5f + Vector3.up * clearance;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float rawT = Mathf.Clamp01(elapsedTime / duration);

                // Easing: bias toward a strong start and a quick settle at the end.
                // Use a pow < 1 to give more movement early (felt as a stronger initial burst).
                float burstBias = Mathf.Pow(rawT, 0.6f);

                // Smooth the curve for natural in/out motion
                float eased = Mathf.SmoothStep(0f, 1f, burstBias);

                // Bezier-style interpolation via two lerps (quadratic bezier)
                Vector3 a = Vector3.Lerp(startPos, midPoint, eased);
                Vector3 b = Vector3.Lerp(midPoint, offsetTargetPos, eased);
                Vector3 currentPos = Vector3.Lerp(a, b, eased);

                // Near the end, quickly move from the offset target into the exact stack position for a solid settle
                if (rawT > 0.85f)
                {
                    float settleT = Mathf.InverseLerp(0.85f, 1f, rawT);
                    // Use a fast ease-in for the settle so it 'snaps' into place
                    settleT = Mathf.Pow(settleT, 2.5f);
                    currentPos = Vector3.Lerp(currentPos, baseTargetPos, settleT);
                }

                newCapybara.transform.position = currentPos;
                newCapybara.transform.rotation = Quaternion.Slerp(
                    newCapybara.transform.rotation,
                    transform.rotation,
                    0.18f
                );

                yield return null;
            }

            // Final placement exactly on stack
            newCapybara.transform.position = baseTargetPos;
            newCapybara.transform.rotation = transform.rotation;

            capybarasStack.Push(newCapybara);
            newCapybara.stackController = this;

            newCapybara.ability.SetOnStack(true);
            bottomCapybara.ability.SetOnStack(true);

            bottomCapybara.canMove = true;
        }
    }

    public void RemoveCapybara(Vector3 direction)
    {
        if (capybarasStack.Count <= 1)
            return;

        CapybaraController topCapybara = capybarasStack.Pop();
        topCapybara.stackController = null;

        topCapybara.ability.SetOnStack(false);

        if (capybarasStack.Count == 1)
        {
            bottomCapybara.ability.SetOnStack(false);
        }

        PropellCapybaraFromStack(topCapybara, direction);
    }

    public void DestroyStack()
    {
        while (capybarasStack.Peek() != bottomCapybara)
        {
            RemoveCapybara(capybarasStack.Peek().transform.forward);
        }
    }

    void PropellCapybaraFromStack(CapybaraController capybara, Vector3 direction)
    {
        StartCoroutine(PropellCoroutine(capybara));

        IEnumerator PropellCoroutine(CapybaraController capybara)
        {
            capybara.GetComponent<Rigidbody>().isKinematic = false;
            capybara.GetComponent<Collider>().enabled = false;
            capybara
                .GetComponent<Rigidbody>()
                .AddForce(
                    direction * capybara.GetComponent<Rigidbody>().mass * 30f,
                    ForceMode.Impulse
                );

            // foreach (Rigidbody rb in capybara.GetComponentsInChildren<Rigidbody>())
            // {
            //     rb.isKinematic = false;
            //     rb.AddForce((capybara.transform.forward + Vector3.up).normalized * rb.mass * 5f, ForceMode.Impulse);
            // }

            yield return new WaitForSeconds(0.2f);
            capybara.GetComponent<Collider>().enabled = true;
            capybara.canMove = true;
        }
    }

    public void TakeDamage(float damage, bool splitDamageAmongStack = true)
    {
        if (splitDamageAmongStack)
        {
            if (capybarasStack.Count == 2)
            {
                damage *= 0.75f;
            }
            else if (capybarasStack.Count == 3)
            {
                damage *= 0.5f;
            }
            foreach (CapybaraController capybara in capybarasStack)
            {
                capybara.TakeDamage(damage);
                // capybara.TakeDamage(damage / capybarasStack.Count);
            }
        }

        else
        {
            capybarasStack.Peek().TakeDamage(damage);
        }

        UpdateStackHP();
        RemoveCapybara(transform.forward);

        if (bottomCapybara.hp <= 0f)
        {
            RemoveCapybara(transform.forward);
        }
    }
}
