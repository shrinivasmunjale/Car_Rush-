using CarRush.Car;
using UnityEngine;

namespace CarRush.Game
{
    public enum ObstacleType
    {
        TrafficCone,
        HazardBarrel,
        RoadBlockBarrier
    }

    /// <summary>
    /// Interactive road hazard / obstacle on the race track.
    /// Responds with physics reactions, knock-back, and brief speed penalty upon car collision.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ObstacleHazard : MonoBehaviour
    {
        [Header("Hazard Configuration")]
        [SerializeField] private ObstacleType obstacleType = ObstacleType.TrafficCone;
        [SerializeField] private float speedPenaltyFraction = 0.2f; // e.g. 0.2 = -20% speed on heavy hit
        [SerializeField] private float hitImpulse = 8f;

        private bool hasBeenHit = false;
        private Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponentInParent<CarController>() != null)
            {
                CarController car = collision.gameObject.GetComponentInParent<CarController>();
                Rigidbody carRb = collision.gameObject.GetComponentInParent<Rigidbody>();

                if (!hasBeenHit)
                {
                    hasBeenHit = true;

                    // Apply slight speed penalty if heavy barrel/barrier
                    if (obstacleType == ObstacleType.HazardBarrel && carRb != null)
                    {
                        carRb.linearVelocity *= (1f - speedPenaltyFraction);
                    }
                    else if (obstacleType == ObstacleType.RoadBlockBarrier && carRb != null)
                    {
                        carRb.linearVelocity *= (1f - (speedPenaltyFraction * 1.5f));
                    }
                }

                // Push obstacle if it has a dynamic rigidbody
                if (rb != null && !rb.isKinematic)
                {
                    Vector3 pushDir = (transform.position - collision.transform.position).normalized + Vector3.up * 0.4f;
                    rb.AddForce(pushDir * hitImpulse, ForceMode.Impulse);
                    rb.AddTorque(Random.insideUnitSphere * hitImpulse * 2f, ForceMode.Impulse);
                }
            }
        }
    }
}
