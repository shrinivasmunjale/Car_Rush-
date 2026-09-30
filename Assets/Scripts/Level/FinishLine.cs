using UnityEngine;
using System;

namespace CarRush.Level
{
    /// <summary>
    /// Track finish line trigger.
    /// Ends the race when all required checkpoints have been passed.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class FinishLine : MonoBehaviour
    {
        public event Action<Collider> OnCarCrossFinishLine;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponentInParent<Car.CarController>() != null)
            {
                OnCarCrossFinishLine?.Invoke(other);
            }
        }
    }
}
