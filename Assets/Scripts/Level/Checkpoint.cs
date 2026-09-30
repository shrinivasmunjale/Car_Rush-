using UnityEngine;
using System;

namespace CarRush.Level
{
    /// <summary>
    /// Track checkpoint trigger.
    /// Tracks car progress along the circuit.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : MonoBehaviour
    {
        public int CheckpointIndex { get; set; }

        public event Action<Checkpoint, Collider> OnCarEnterCheckpoint;

        [Header("Visuals")]
        [SerializeField] private MeshRenderer visualIndicator;
        [SerializeField] private Color normalColor = new Color(0.2f, 0.6f, 1f, 0.4f);
        [SerializeField] private Color passedColor = new Color(0.2f, 0.9f, 0.3f, 0.4f);

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponentInParent<Car.CarController>() != null)
            {
                OnCarEnterCheckpoint?.Invoke(this, other);
            }
        }

        public void SetPassed(bool passed)
        {
            if (visualIndicator != null)
            {
                visualIndicator.material.color = passed ? passedColor : normalColor;
            }
        }
    }
}
