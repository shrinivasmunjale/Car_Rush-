using UnityEngine;

namespace CarRush.Car
{
    /// <summary>
    /// Smooth chase camera that follows the car with dynamic lookahead,
    /// pitch smoothing, and speed-based field-of-view zooming.
    /// </summary>
    public class CarCameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Rigidbody targetRb;

        [Header("Follow Settings")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 2.2f, -5.5f);
        [SerializeField] private float positionDamping = 10f;
        [SerializeField] private float rotationDamping = 8f;
        [SerializeField] private float lookaheadDistance = 3f;

        [Header("Dynamic FOV")]
        [SerializeField] private Camera cam;
        [SerializeField] private float minFov = 60f;
        [SerializeField] private float maxFov = 75f;
        [SerializeField] private float maxFovSpeedKmh = 140f;

        private void Awake()
        {
            if (cam == null) cam = GetComponent<Camera>();
        }

        public void SetTarget(Transform targetTransform, Rigidbody rb)
        {
            target = targetTransform;
            targetRb = rb;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Target position behind car aligned with car forward
            Vector3 desiredPosition = target.TransformPoint(offset);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * positionDamping);

            // Look slightly ahead of car
            Vector3 lookTarget = target.position + target.forward * lookaheadDistance + Vector3.up * 1f;
            Quaternion desiredRotation = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * rotationDamping);

            // Dynamic FOV
            if (cam != null && targetRb != null)
            {
                float speed = targetRb.linearVelocity.magnitude * 3.6f;
                float targetFov = Mathf.Lerp(minFov, maxFov, Mathf.Clamp01(speed / maxFovSpeedKmh));
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Time.deltaTime * 3f);
            }
        }
    }
}
