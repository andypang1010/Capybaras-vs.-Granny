using UnityEngine;

public class CapybaraPointer : MonoBehaviour
{
    [Header("Capybara Settings")]
    public CapybaraController targetCapybara;
    public GameObject targetIndicator;
    private CapybaraController ownerCapybara;

    [Header("Pointer Settings")]
    public Vector3 pointerOffset = new Vector3(0, -0.00428877817f, 0.00114480953f);
    public bool smoothRotation = true;

    [Range(0.1f, 20f)]
    public float rotationSpeed = 10f;

    [SerializeField]
    private Vector3 directionToTarget;

    void Start()
    {
        ownerCapybara = transform.root.GetComponent<CapybaraController>();
    }

    private void Update()
    {
        if (
            (ownerCapybara.stackController && ownerCapybara.playerIndex != 1)
            || ownerCapybara.hp <= 0f
        )
        {
            targetIndicator.SetActive(false);
            return;
        }
        if (targetCapybara == null)
        {
            targetIndicator.SetActive(false);
            return;
        }
        if (targetCapybara.stackController)
        {
            if (targetCapybara.playerIndex != 1)
            {
                targetIndicator.SetActive(false);
                return;
            }
            else if (targetCapybara.stackController.capybaraArray.Length >= 3)
            {
                targetIndicator.SetActive(false);
                return;
            }
            // if (targetCapybara.transform != targetCapybara.stackController.transform)
            // {
            //     targetIndicator.SetActive(false);
            //     return;
            // }
            // else if (targetCapybara.stackController.capybaraArray.Length > 1 && targetCapybara.playerIndex != 1)
            // {
            //     targetIndicator.SetActive(false);
            //     return;
            // }
        }
        targetIndicator.SetActive(true);
        PointToTarget();
    }

    private void PointToTarget()
    {
        Vector3 targetPosition = targetCapybara
            .transform.Find("Model/riggedCapybara/spine/spine.001/spine.002/spine.004")
            .position;
        Vector3 currentPosition = transform.position;

        directionToTarget = new Vector3(
            targetPosition.x - currentPosition.x,
            0f,
            targetPosition.z - currentPosition.z
        );

        if (directionToTarget.magnitude < 0.1f)
            return;

        directionToTarget.Normalize();

        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget, Vector3.up);

        if (smoothRotation)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
        else
        {
            transform.rotation = targetRotation;
        }

        transform.position = ownerCapybara
            .transform.Find("Model/riggedCapybara/spine/spine.001/spine.002/spine.004")
            .position + pointerOffset;
    }
}
