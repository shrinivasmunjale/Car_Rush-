using CarRush.Game;
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
        [SerializeField] private float maxSteerAngle = 38f;

        [Tooltip("Steering angle at top speed (prevents oversteering).")]
        [SerializeField] private float highSpeedSteerAngle = 14f;

        [Header("Physics Stability")]
        [Tooltip("Center of mass offset (Y should be negative to prevent rollovers).")]
        [SerializeField] private Vector3 centerOfMassOffset = new Vector3(0f, -0.45f, 0.1f);

        [Tooltip("Aerodynamic downforce multiplier for high-speed grip.")]
        [SerializeField] private float downforce = 60f;

        [Tooltip("Anti-roll bar stiffness to minimize body roll.")]
        [SerializeField] private float antiRollStiffness = 5000f;

        private Rigidbody rb;
        private float currentSteerInput;
        private float _targetSteerInput;    // Raw steer from ReadInput(), smoothed in FixedUpdate
        private float currentThrottleInput;
        private bool isHandbraking;
        private bool isControlEnabled = true;
        private bool isStopped = false;

        public float CurrentSpeedKmh => rb != null ? rb.linearVelocity.magnitude * 3.6f : 0f;
        public float ThrottleInput => currentThrottleInput;
        public float SteerInput => currentSteerInput;
        public bool IsBraking => currentThrottleInput < 0f || isHandbraking || isStopped;
        public bool IsControlEnabled => isControlEnabled;
        public bool IsStopped => isStopped;
        public Rigidbody Rigidbody => rb;
        public WheelCollider FrontLeftCollider => frontLeftWheel.collider;
        public WheelCollider FrontRightCollider => frontRightWheel.collider;
        public WheelCollider RearLeftCollider => rearLeftWheel.collider;
        public WheelCollider RearRightCollider => rearRightWheel.collider;

        /// <summary>
        /// Enables or disables user control over the car.
        /// </summary>
        public void SetControlEnabled(bool enabled)
        {
            isControlEnabled = enabled;
            if (!enabled)
            {
                currentSteerInput = 0f;
                _targetSteerInput = 0f;
                currentThrottleInput = 0f;
                isHandbraking = false;
            }
            else
            {
                isStopped = false;
            }
        }

        /// <summary>
        /// Emergency stops the car and permanently disables user control (e.g. when time expires).
        /// </summary>
        public void StopAndDisableControl()
        {
            isControlEnabled = false;
            isStopped = true;
            currentSteerInput = 0f;
            currentThrottleInput = 0f;
            isHandbraking = true;

            ApplyTorqueToWheel(frontLeftWheel, 0f, brakeTorque);
            ApplyTorqueToWheel(frontRightWheel, 0f, brakeTorque);
            ApplyTorqueToWheel(rearLeftWheel, 0f, handbrakeTorque);
            ApplyTorqueToWheel(rearRightWheel, 0f, handbrakeTorque);
        }

        /// <summary>
        /// Gently stops the car (e.g. after crossing finish line).
        /// </summary>
        public void StopCar()
        {
            isStopped = true;
            currentThrottleInput = 0f;
            currentSteerInput = 0f;
            isHandbraking = true;
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.centerOfMass = centerOfMassOffset;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // Automatically attach TireMarks component if missing
            if (GetComponent<TireMarks>() == null)
            {
                gameObject.AddComponent<TireMarks>();
            }
        }

        private void Update()
        {
            ReadInput();
        }

        private void LateUpdate()
        {
            // Must run AFTER physics (FixedUpdate) has resolved for the frame.
            // Calling this in Update() causes a 1-frame visual lag on wheel positions
            // that manifests as jitter/stutter at speed.
            UpdateWheelMeshes();
        }

        private void FixedUpdate()
        {
            // Apply steering smoothing at the physics rate so it is frame-rate-independent.
            // 26 units/s gives fast, responsive steering with no input lag.
            currentSteerInput = Mathf.MoveTowards(currentSteerInput, _targetSteerInput, Time.fixedDeltaTime * 26f);

            // If time is up or emergency stop is engaged, swiftly bring the car to a full stop
            bool timeExpired = RaceManager.Instance != null && RaceManager.Instance.CurrentState == RaceState.TimeUp;
            if (isStopped || timeExpired)
            {
                isStopped = true;
                isControlEnabled = false;
                isHandbraking = true;

                if (rb != null)
                {
                    rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 35f);
                    rb.angularVelocity = Vector3.MoveTowards(rb.angularVelocity, Vector3.zero, Time.fixedDeltaTime * 25f);
                    if (rb.linearVelocity.sqrMagnitude < 0.05f)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                }
            }

            ApplyMotorAndBrakes();
            ApplySteering();
            ApplyAntiRoll(frontLeftWheel.collider, frontRightWheel.collider);
            ApplyAntiRoll(rearLeftWheel.collider, rearRightWheel.collider);
            ApplyDownforce();
        }

        private void ReadInput()
        {
            // Reject any input if controls are disabled or race is over / time is up
            bool timeExpired = RaceManager.Instance != null && RaceManager.Instance.CurrentState == RaceState.TimeUp;
            if (!isControlEnabled || timeExpired)
            {
                currentSteerInput = 0f;
                _targetSteerInput = 0f;
                currentThrottleInput = 0f;
                isHandbraking = isStopped || timeExpired;
                return;
            }

            float targetSteer = 0f;
            float targetThrottle = 0f;
            bool handbrake = false;

#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;

            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) targetThrottle += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) targetThrottle -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) targetSteer += 1.5f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) targetSteer -= 1.5f;
                if (keyboard.spaceKey.isPressed) handbrake = true;
            }

            if (gamepad != null)
            {
                targetThrottle += gamepad.rightTrigger.ReadValue() - gamepad.leftTrigger.ReadValue();
                targetSteer += gamepad.leftStick.x.ReadValue();
                if (gamepad.buttonSouth.isPressed) handbrake = true;
            }
#else
            targetSteer = Input.GetAxis("Horizontal");
            targetThrottle = Input.GetAxis("Vertical");
            handbrake = Input.GetKey(KeyCode.Space);
#endif

            targetSteer = Mathf.Clamp(targetSteer, -1f, 1f);
            targetThrottle = Mathf.Clamp(targetThrottle, -1f, 1f);

            // Store raw inputs; smoothing is applied in FixedUpdate at the physics rate
            // to avoid frame-rate dependency (Update runs at render FPS, not physics FPS).
            _targetSteerInput = targetSteer;
            currentThrottleInput = targetThrottle;
            isHandbraking = handbrake;
        }

        private void ApplyMotorAndBrakes()
        {
            if (isStopped)
            {
                ApplyTorqueToWheel(frontLeftWheel, 0f, brakeTorque);
                ApplyTorqueToWheel(frontRightWheel, 0f, brakeTorque);
                ApplyTorqueToWheel(rearLeftWheel, 0f, handbrakeTorque);
                ApplyTorqueToWheel(rearRightWheel, 0f, handbrakeTorque);
                return;
            }

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
                rb.position = position;
                rb.rotation = rotation;
            }
            transform.position = position;
            transform.rotation = rotation;
            Physics.SyncTransforms();

            // Reset inputs & wheel states
            currentSteerInput = 0f;
            _targetSteerInput = 0f;
            currentThrottleInput = 0f;
            isHandbraking = false;
            ResetWheel(frontLeftWheel);
            ResetWheel(frontRightWheel);
            ResetWheel(rearLeftWheel);
            ResetWheel(rearRightWheel);
            UpdateWheelMeshes();
        }

        private void ResetWheel(WheelInfo wheel)
        {
            if (wheel.collider != null)
            {
                wheel.collider.motorTorque = 0f;
                wheel.collider.brakeTorque = 0f;
                wheel.collider.steerAngle = 0f;
            }
        }
    }
}
