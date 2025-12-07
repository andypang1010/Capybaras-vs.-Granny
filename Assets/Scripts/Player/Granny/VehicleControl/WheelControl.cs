using UnityEngine;

public class WheelControl : MonoBehaviour
{
    public Transform wheelModel;

    [HideInInspector] public WheelCollider WheelCollider;

    // Create properties for the CarControl script
    // (You should enable/disable these via the 
    // Editor Inspector window)
    public bool steerable;
    public bool motorized;

    Vector3 position;
    Quaternion rotation;

    Vector3 originalPosition;
    Quaternion originalRotation;


    // Start is called before the first frame update
    private void Start()
    {
        WheelCollider = GetComponent<WheelCollider>();
        originalRotation = wheelModel.transform.localRotation;
        //originalPosition = wheelModel.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        // Get the Wheel collider's world pose values and
        // use them to set the wheel model's position and rotation
        if (wheelModel == null) return;
        WheelCollider.GetWorldPose(out position, out rotation);
        //wheelModel.transform.position = position + originalPosition;
        wheelModel.transform.rotation = rotation * originalRotation;
    }
}
