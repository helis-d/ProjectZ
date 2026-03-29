using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

namespace ProjectZ.Player
{
    /// <summary>
    /// Records the player's position every few fractions of a second.
    /// Used by abilities like Lagrange's Temporal Rewind.
    /// </summary>
    public class TemporalTracker : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _recordInterval = 0.5f; // Seconds between capturing position
        [SerializeField] private float _maxHistorySeconds = 10.0f; // Max duration to keep

        // Queue to store position history
        private Queue<Vector3> _positionHistory = new Queue<Vector3>();
        private int _maxQueueSize;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _maxQueueSize = Mathf.CeilToInt(_maxHistorySeconds / _recordInterval);
            StartCoroutine(RecordRoutine());
        }

        private IEnumerator RecordRoutine()
        {
            while (true)
            {
                // Add current position to rear of queue
                _positionHistory.Enqueue(transform.position);

                // If queue exceeds max size, remove oldest position from front
                if (_positionHistory.Count > _maxQueueSize)
                {
                    _positionHistory.Dequeue();
                }

                yield return new WaitForSeconds(_recordInterval);
            }
        }

        /// <summary>
        /// Returns the oldest recorded position in history (up to _maxHistorySeconds ago).
        /// If no history exists, returns current position.
        /// </summary>
        [Server]
        public Vector3 GetOldestPosition()
        {
            if (_positionHistory.Count == 0) return transform.position;
            return _positionHistory.Peek(); // Oldest is at the front of the queue
        }
    }
}
