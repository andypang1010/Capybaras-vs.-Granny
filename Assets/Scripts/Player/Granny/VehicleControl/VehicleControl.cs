using NUnit.Framework;
using UnityEngine;

public class WheelChairControl : MonoBehaviour
{
    public bool IsWithController = false;
    [Header("Vehicle Properties")]
    public float MotorTorque = 3000f;
    public float BrakeTorque = 4500f;
    public float MaxSpeed = 10f;
    public float SteeringRange = 30f;
    public float SteeringRangeAtMaxSpeed = 10f;
    public float CentreOfGravityOffsetX = -0.5f;
    public float CentreOfGravityOffsetY = -0.5f;
    public float CentreOfGravityOffsetZ = -0.5f;
    public bool IsStopped = false;
    [SerializeField] float _vInput;
    [SerializeField] float _hInput;

    private WheelControl[] wheels;
    private Rigidbody rigidBody;

    void Start()
    {
        rigidBody = GetComponent<Rigidbody>();

        Vector3 centerOfMass = rigidBody.centerOfMass;
        centerOfMass.x += CentreOfGravityOffsetX;
        centerOfMass.y += CentreOfGravityOffsetY;
        centerOfMass.z += CentreOfGravityOffsetZ;
        rigidBody.centerOfMass = centerOfMass;

        wheels = GetComponentsInChildren<WheelControl>();
    }

    void FixedUpdate()
    {
        //WheelChairMovement();
    }

    public void WheelChairMovement(bool isBraking)
    {
        if (IsStopped)
        {
            StopCar();
            return;
        }
        if (IsWithController)
        {
            _vInput = Input.GetAxis("Vertical"); // Forward/backward input
            _hInput = Input.GetAxis("Horizontal"); // Steering input
        }
        else
        {
            Vector2 vector2 = InputManager.Instance.GetPlayerMovement(0);
            _vInput = vector2.y;
            _hInput = vector2.x;
            
        }
        float forwardSpeed = Vector3.Dot(transform.forward, rigidBody.linearVelocity);
        float speedFactor = Mathf.InverseLerp(0, MaxSpeed, Mathf.Abs(forwardSpeed)); // Normalized speed factor

        float currentMotorTorque;

        if (Mathf.Abs(forwardSpeed) >= MaxSpeed)
        {
            currentMotorTorque = 0f; // No additional torque needed at max speed
        }
        else
        {
            currentMotorTorque = MotorTorque; // Full torque to reach max speed
        }

        float currentSteerRange = Mathf.Lerp(SteeringRange, SteeringRangeAtMaxSpeed, speedFactor);

        // Determine motor and brake behavior based on isReverse flag
        float motorTorqueInput = 0f;
        float brakeTorqueInput = 0f;

        // if (!isReverse) // Normal mode
        // {
        //     if (_vInput > 0) // Positive input - accelerate forward
        //     {
        //         motorTorqueInput = _vInput;
        //         brakeTorqueInput = 0f;
        //     }
        //     else if (_vInput < 0) // Negative input - brake
        //     {
        //         motorTorqueInput = 0f;
        //         brakeTorqueInput = Mathf.Abs(_vInput);
        //     }
        // }
        // else // Reverse mode
        // {
        //     if (_vInput > 0) // Positive input - brake
        //     {
        //         motorTorqueInput = 0f;
        //         brakeTorqueInput = _vInput;
        //     }
        //     else if (_vInput < 0) // Negative input - accelerate backward
        //     {
        //         motorTorqueInput = _vInput; // Negative value for reverse
        //         brakeTorqueInput = 0f;
        //     }
        // }
        motorTorqueInput = _vInput;
        brakeTorqueInput = isBraking ? 1 : 0f;

        foreach (var wheel in wheels)
        {
            if (wheel.steerable)
            {
                wheel.WheelCollider.steerAngle = _hInput * currentSteerRange;
            }

            // Apply motor torque
            if (wheel.motorized)
            {
                wheel.WheelCollider.motorTorque = motorTorqueInput * currentMotorTorque;
            }
            
            // Apply brake torque
            wheel.WheelCollider.brakeTorque = brakeTorqueInput * BrakeTorque;
        }
    }   

    
    public void StopCar()
    {
        rigidBody.linearVelocity = Vector3.zero;
        rigidBody.angularVelocity = Vector3.zero;

        foreach (var wheel in wheels)
        {
            wheel.WheelCollider.motorTorque = 0f;
            wheel.WheelCollider.brakeTorque = BrakeTorque * 2f; // Apply stronger brakes to ensure stopping
            wheel.WheelCollider.steerAngle = 0f; // Reset steering
        }
        IsStopped = true;
    }

    // Visualize center of mass in editor
    private void OnDrawGizmosSelected()
    {
        // Draw original center of mass
        Gizmos.color = Color.yellow;
        Vector3 originalCenterOfMass = transform.position;
        if (rigidBody != null)
        {
            // Get the original center of mass (without our modifications)
            Vector3 originalCOM = transform.TransformPoint(rigidBody.centerOfMass - new Vector3(CentreOfGravityOffsetX, CentreOfGravityOffsetY, CentreOfGravityOffsetZ));
            Gizmos.DrawWireSphere(originalCOM, 0.1f);
            Gizmos.DrawLine(transform.position, originalCOM);
        }

        // Draw adjusted center of mass
        Gizmos.color = Color.red;
        Vector3 adjustedCenterOfMass = transform.position;
        if (rigidBody != null)
        {
            // Calculate the adjusted center of mass position in world space
            Vector3 adjustedCOM = transform.TransformPoint(rigidBody.centerOfMass);
            Gizmos.DrawSphere(adjustedCOM, 0.15f);
            Gizmos.DrawLine(transform.position, adjustedCOM);
        }
        else
        {
            // Preview the adjusted center of mass even without rigidbody (in edit mode)
            Vector3 previewCOM = transform.position + transform.TransformDirection(new Vector3(CentreOfGravityOffsetX, CentreOfGravityOffsetY, CentreOfGravityOffsetZ));
            Gizmos.DrawSphere(previewCOM, 0.15f);
            Gizmos.DrawLine(transform.position, previewCOM);
        }

        // Draw vehicle center for reference
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.2f);
    }

    private void OnDrawGizmos()
    {
        // Always show the adjusted center of mass (even when not selected)
        if (rigidBody != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f); // Semi-transparent red
            Vector3 adjustedCOM = transform.TransformPoint(rigidBody.centerOfMass);
            Gizmos.DrawSphere(adjustedCOM, 0.1f);
        }
        else
        {
            // Preview in edit mode
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Vector3 previewCOM = transform.position + transform.TransformDirection(new Vector3(CentreOfGravityOffsetX, CentreOfGravityOffsetY, CentreOfGravityOffsetZ));
            Gizmos.DrawSphere(previewCOM, 0.1f);
        }
    }
}
