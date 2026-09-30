using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CarRush.Car
{
    /// <summary>
    /// Arcade car controller using Unity's WheelColliders with anti-roll,
    /// downforce, speed-sensitive steering, and visual wheel mesh synchronisation.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CarController : MonoBehaviour
    {
        [System.Serializable]
        public struct WheelInfo
        {
            public WheelCollider collider;
            public Transform visualMesh;
            public bool isMotor;
            public bool isSteer;
        }

        [Header("Wheel Setup")]
        [SerializeField] private WheelInfo frontLeftWheel;
        [SerializeField] private WheelInfo frontRightWheel;
        [SerializeField] private WheelInfo rearLeftWheel;
        [SerializeField] private WheelInfo rearRightWheel;

        [Header("Engine & Driving")]
        [Tooltip("Max motor torque applied to driven wheels in N*m.")]
        [SerializeField] private float motorTorque = 1600f;

        [Tooltip("Braking torque applied when pressing reverse/brake in N*m.")]
        [SerializeField] private float brakeTorque = 3000f;

        [Tooltip("Handbrake torque applied to rear wheels in N*m.")]
        [SerializeField] private float handbrakeTorque = 6000f;

        [Tooltip("Top speed in km/h.")]
        [SerializeField] private float maxSpeedKmh = 160f;

        [Tooltip("Max steering angle at low speeds.")]
        [SerializeField] private float maxSteerAngle = 32f;

        [Tooltip("Steering angle at top speed (prevents oversteering).")]
        [SerializeField] private float highSpeedSteerAngle = 12f;

        [Header("Physics Stability")]
        [Tooltip("Center of mass offset (Y should be negative to prevent rollovers).")]
        [SerializeField] private Vector3 centerOfMassOffset = new Vector3(0f, -0.45f, 0.1f);

        [Tooltip("Aerodynamic downforce multiplier for high-speed grip.")]
        [SerializeField] private float downforce = 60f;

        [Tooltip("Anti-roll bar stiffness to minimize body roll.")]
        [SerializeField] private float antiRollStiffness = 5000f;

        private Rigidbody rb;
        private float currentSteerInput;
        private float currentThrottleInput;
        private bool isHandbraking;

        public float CurrentSpeedKmh => rb != null ? rb.linearVelocity.magnitude * 3.6f : 0f;
        public float ThrottleInput => currentThrottleInput;
        public float SteerInput => currentSteerInput;
        public bool IsBraking => currentThrottleInput < 0f || isHandbraking;
        public Rigidbody Rigidbody => rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.centerOfMass += centerOfMassOffset;
        }

        private void Update()
        {
            ReadInput();
            UpdateWheelMeshes();
        }

        private void FixedUpdate()
        {
            ApplyMotorAndBrakes();
            ApplySteering();
            ApplyAntiRoll(frontLeftWheel.collider, frontRightWheel.collider);
            ApplyAntiRoll(rearLeftWheel.collider, rearRightWheel.collider);
            ApplyDownforce();
        }

        private void ReadInput()
        {
            float steer = 0f;
            float throttle = 0f;
            bool handbrake = false;

#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;

            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) throttle += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) throttle -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) steer += 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) steer -= 1f;
                if (keyboard.spaceKey.isPressed) handbrake = true;
            }

            if (gamepad != null)
            {
                throttle += gamepad.rightTrigger.ReadValue() - gamepad.leftTrigger.ReadValue();
                steer += gamepad.leftStick.x.ReadValue();
                if (gamepad.buttonSouth.isPressed) handbrake = true;
            }
#else
            steer = Input.GetAxis("Horizontal");
            throttle = Input.GetAxis("Vertical");
            handbrake = Input.GetKey(KeyCode.Space);
#endif

            currentSteerInput = Mathf.Clamp(steer, -1f, 1f);
            currentThrottleInput = Mathf.Clamp(throttle, -1f, 1f);
            isHandbraking = handbrake;
        }

        private void ApplyMotorAndBrakes()
        {
            float speed = CurrentSpeedKmh;
            float speedFactor = Mathf.Clamp01(speed / maxSpeedKmh);

            float activeMotorTorque = 0f;
            float activeBrakeTorque = 0f;

            if (isHandbraking)
            {
                activeBrakeTorque = handbrakeTorque;
            }
            else if (currentThrottleInput > 0f)
            {
                // Accelerate forward (fade torque at top speed)
                activeMotorTorque = (1f - speedFactor) * currentThrottleInput * motorTorque;
                activeBrakeTorque = 0f;
            }
            else if (currentThrottleInput < 0f)
            {
                // Moving forward -> Brake; Moving backward / stopped -> Reverse
                bool movingForward = Vector3.Dot(rb.linearVelocity, transform.forward) > 0.5f;
                if (movingForward)
                {
                    activeBrakeTorque = Mathf.Abs(currentThrottleInput) * brakeTorque;
                    activeMotorTorque = 0f;
                }
                else
                {
                    // Reverse
                    activeMotorTorque = currentThrottleInput * (motorTorque * 0.6f);
                    activeBrakeTorque = 0f;
                }
            }

            ApplyTorqueToWheel(frontLeftWheel, activeMotorTorque, activeBrakeTorque);
            ApplyTorqueToWheel(frontRightWheel, activeMotorTorque, activeBrakeTorque);
            ApplyTorqueToWheel(rearLeftWheel, activeMotorTorque, activeBrakeTorque);
            ApplyTorqueToWheel(rearRightWheel, activeMotorTorque, activeBrakeTorque);
        }

        private void ApplyTorqueToWheel(WheelInfo wheel, float motor, float brake)
        {
            if (wheel.collider == null) return;

            if (wheel.isMotor)
                wheel.collider.motorTorque = motor;

            wheel.collider.brakeTorque = brake;
        }

        private void ApplySteering()
        {
            float speedFactor = Mathf.Clamp01(CurrentSpeedKmh / maxSpeedKmh);
            float steerLimit = Mathf.Lerp(maxSteerAngle, highSpeedSteerAngle, speedFactor);
            float currentSteer = currentSteerInput * steerLimit;

            if (frontLeftWheel.collider != null && frontLeftWheel.isSteer)
                frontLeftWheel.collider.steerAngle = currentSteer;

            if (frontRightWheel.collider != null && frontRightWheel.isSteer)
                frontRightWheel.collider.steerAngle = currentSteer;
        }

        private void ApplyAntiRoll(WheelCollider left, WheelCollider right)
        {
            if (left == null || right == null) return;

            float travelL = 1f;
            float travelR = 1f;

            WheelHit hit;
            bool groundedL = left.GetGroundHit(out hit);
            if (groundedL)
                travelL = (-left.transform.InverseTransformPoint(hit.point).y - left.radius) / left.suspensionDistance;

            bool groundedR = right.GetGroundHit(out hit);
            if (groundedR)
                travelR = (-right.transform.InverseTransformPoint(hit.point).y - right.radius) / right.suspensionDistance;

            float antiRollForce = (travelL - travelR) * antiRollStiffness;

            if (groundedL)
                rb.AddForceAtPosition(left.transform.up * -antiRollForce, left.transform.position);

            if (groundedR)
                rb.AddForceAtPosition(right.transform.up * antiRollForce, right.transform.position);
        }

        private void ApplyDownforce()
        {
            if (rb != null)
            {
                rb.AddForce(-transform.up * (downforce * rb.linearVelocity.magnitude));
            }
        }

        private void UpdateWheelMeshes()
        {
            UpdateWheelMesh(frontLeftWheel);
            UpdateWheelMesh(frontRightWheel);
            UpdateWheelMesh(rearLeftWheel);
            UpdateWheelMesh(rearRightWheel);
        }

        private void UpdateWheelMesh(WheelInfo wheel)
        {
            if (wheel.collider == null || wheel.visualMesh == null) return;

            wheel.collider.GetWorldPose(out Vector3 pos, out Quaternion rot);
            wheel.visualMesh.position = pos;
            wheel.visualMesh.rotation = rot;
        }

        /// <summary>Resets velocity and repositions car to a spawn point.</summary>
        public void ResetToPose(Vector3 position, Quaternion rotation)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            transform.position = position;
            transform.rotation = rotation;
        }
    }
}
