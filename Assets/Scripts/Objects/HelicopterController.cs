using System.Collections;
using UnityEngine;

public class HelicopterController : MonoBehaviour
{
    public float activationTime = 60f;
    public GameObject helicopter;
    public GameObject helicopterVFX;
    public GameObject propellers;
    public Transform landingZone;
    public float propellerRotationSpeed = 720f;

    Vector3 moveVelocity;

    void Start()
    {
        helicopter.SetActive(false);
        helicopterVFX.SetActive(false);
        moveVelocity =
            (landingZone.position - helicopter.transform.position) / (activationTime - 10f);
    }

    void Update()
    {
        helicopter.SetActive(GameManager.Instance.GameTimer <= activationTime);
        helicopterVFX.SetActive(GameManager.Instance.GameTimer <= activationTime);

        if (!helicopter.activeSelf)
            return;

        if (Vector3.Distance(helicopter.transform.position, landingZone.position) > 0.1f)
            helicopter.transform.position += moveVelocity * Time.deltaTime;

        if (GameManager.Instance.GameTimer > 10f)
        {
            propellers.transform.Rotate(Vector3.forward, propellerRotationSpeed * Time.deltaTime);
        }
        else
        {
            propellers.transform.Rotate(
                Vector3.forward,
                Mathf.Clamp(
                    propellerRotationSpeed * (GameManager.Instance.GameTimer / 20f),
                    0f,
                    propellerRotationSpeed
                ) * Time.deltaTime
            );
        }

        if (GameManager.Instance.GameTimer < 5f)
        {
            helicopter.GetComponentInChildren<Animator>().SetTrigger("DoorOpenTrigger");
        }
    }
}
