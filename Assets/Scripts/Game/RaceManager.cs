using System;
using System.Collections;
using System.Collections.Generic;
using CarRush.Car;
using CarRush.Level;
using CarRush.Save;
using UnityEngine;

namespace CarRush.Game
{
    public enum RaceState
    {
        Countdown,
        Racing,
        Paused,
        Completed,
        TimeUp
    }

    /// <summary>
    /// Master coordinator for race flow, countdown, timing, checkpoints,
    /// time-limit enforcement, off-track / fall distance thresholds,
    /// start reverse prevention, respawns, and level completion.
    /// </summary>
    public class RaceManager : MonoBehaviour
    {
        public static RaceManager Instance { get; private set; }

        [Header("Level Config")]
        [SerializeField] private LevelData levelData;

        [Header("Car & Track References")]
        [SerializeField] private CarController playerCar;
        [SerializeField] private Transform startSpawnPoint;
        [SerializeField] private List<Checkpoint> checkpoints = new List<Checkpoint>();
        [SerializeField] private FinishLine finishLine;

        [Header("Respawn & Fall Thresholds")]
        [Tooltip("Absolute Y below which car always respawns.")]
        [SerializeField] private float fallThresholdY = -40f;

        [Tooltip("Max vertical drop below last checkpoint height before auto-respawning.")]
        [SerializeField] private float maxVerticalDropBelowCheckpoint = 50f;

        [Tooltip("Time in seconds upside-down before auto-respawning.")]
        [SerializeField] private float upsideDownRespawnTime = 2.5f;

        // State
        public RaceState CurrentState { get; private set; } = RaceState.Countdown;
        public float CurrentRaceTime { get; private set; } = 0f;
        public float RemainingTime => levelData != null ? Mathf.Max(0f, levelData.timeLimit - CurrentRaceTime) : 0f;
        public int CurrentCheckpointIndex { get; private set; } = 0;
        public int TotalCheckpoints => checkpoints.Count;
        public string CountdownText { get; private set; } = "";
        public LevelData LevelData => levelData;
        public CarController PlayerCar => playerCar;

        // Events
        public event Action<RaceState> OnStateChanged;
        public event Action<int, int> OnCheckpointReached; // current, total
        public event Action<float, bool> OnRaceFinished; // time, isNewBest
        public event Action OnTimeUp;

        private Vector3 lastRespawnPosition;
        private Quaternion lastRespawnRotation;
        private Vector3 startSpawnPosition;
        private Vector3 startSpawnForward;
        private float upsideDownTimer = 0f;
        private bool isFinished = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            isFinished = false;
            if (startSpawnPoint != null)
            {
                startSpawnPosition = startSpawnPoint.position;
                startSpawnForward = startSpawnPoint.forward;
                lastRespawnPosition = startSpawnPoint.position;
                lastRespawnRotation = startSpawnPoint.rotation;
            }
            else if (playerCar != null)
            {
                startSpawnPosition = playerCar.transform.position;
                startSpawnForward = playerCar.transform.forward;
                lastRespawnPosition = playerCar.transform.position;
                lastRespawnRotation = playerCar.transform.rotation;
            }

            SetupCheckpoints();
            StartCoroutine(CountdownSequence());
        }

        private void SetupCheckpoints()
        {
            for (int i = 0; i < checkpoints.Count; i++)
            {
                checkpoints[i].CheckpointIndex = i;
                checkpoints[i].OnCarEnterCheckpoint += HandleCheckpointReached;
                checkpoints[i].SetPassed(false);
            }

            if (finishLine != null)
            {
                finishLine.OnCarCrossFinishLine += HandleFinishLineCrossed;
            }
        }

        private IEnumerator CountdownSequence()
        {
            SetState(RaceState.Countdown);

            // Freeze car physics during countdown
            if (playerCar != null && playerCar.Rigidbody != null)
                playerCar.Rigidbody.isKinematic = true;

            CountdownText = "3";
            yield return new WaitForSeconds(1f);

            CountdownText = "2";
            yield return new WaitForSeconds(1f);

            CountdownText = "1";
            yield return new WaitForSeconds(1f);

            CountdownText = "GO!";
            if (playerCar != null && playerCar.Rigidbody != null)
                playerCar.Rigidbody.isKinematic = false;

            SetState(RaceState.Racing);
            yield return new WaitForSeconds(0.8f);
            CountdownText = "";
        }

        private void Update()
        {
            if (CurrentState == RaceState.Racing)
            {
                CurrentRaceTime += Time.deltaTime;

                // Check time limit
                if (levelData != null && CurrentRaceTime >= levelData.timeLimit)
                {
                    HandleTimeUp();
                    return;
                }

                if (playerCar != null)
                {
                    CheckStartReverseBarrier();
                    CheckFallAndOffTrack();
                }

                // Manual respawn (R key)
                if (Input.GetKeyDown(KeyCode.R))
                {
                    RespawnCar();
                }
            }
        }

        /// <summary>
        /// Prevents reversing backwards past the start line during race start.
        /// </summary>
        private void CheckStartReverseBarrier()
        {
            // Only active at the very start before reaching any checkpoint
            if (CurrentCheckpointIndex == 0 && startSpawnForward.sqrMagnitude > 0.01f)
            {
                Vector3 toCar = playerCar.transform.position - startSpawnPosition;
                float forwardDot = Vector3.Dot(toCar, startSpawnForward);

                // If player attempts to reverse backwards past the start line
                if (forwardDot < -4.0f)
                {
                    Rigidbody rb = playerCar.Rigidbody;
                    if (rb != null)
                    {
                        float velocityAlongForward = Vector3.Dot(rb.linearVelocity, startSpawnForward);
                        if (velocityAlongForward < 0f)
                        {
                            rb.linearVelocity = Vector3.zero;
                            rb.angularVelocity = Vector3.zero;
                        }
                    }
                }
            }
        }

        private float offRoadTimer = 0f;
        private float respawnGraceTimer = 0f;

        /// <summary>
        /// Checks if car has driven outside the road boundaries, fallen off the track, or rolled upside-down.
        /// </summary>
        private void CheckFallAndOffTrack()
        {
            if (isFinished || playerCar == null) return;

            // During respawn grace period (1.2s), pause all off-road and fall checks so car settles smoothly on the road
            if (respawnGraceTimer > 0f)
            {
                respawnGraceTimer -= Time.deltaTime;
                offRoadTimer = 0f;
                upsideDownTimer = 0f;
                return;
            }

            Vector3 carPos = playerCar.transform.position;

            // 1. Extreme vertical drop below absolute bottom floor or checkpoint height
            if (carPos.y < fallThresholdY || carPos.y < lastRespawnPosition.y - 15f)
            {
                Debug.Log("[RaceManager] Car fell off the track. Instant respawn at last checkpoint...");
                RespawnCar();
                return;
            }

            // 2. Downward road surface raycast check
            bool isOverRoad = false;
            Vector3 rayOrigin = carPos + Vector3.up * 0.8f;
            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, 4.5f);
            foreach (var h in hits)
            {
                if (h.collider != null && !h.collider.isTrigger)
                {
                    string colName = h.collider.name.ToLowerInvariant();
                    // Road segments, curbs, roadblocks belong to track
                    if (colName.Contains("road") || colName.Contains("curb") || colName.Contains("track") ||
                        (h.collider.transform.parent != null && h.collider.transform.parent.name.ToLowerInvariant().Contains("track")))
                    {
                        isOverRoad = true;
                        break;
                    }
                }
            }

            // If car leaves the road completely (flies off or drives outside into void)
            if (!isOverRoad)
            {
                offRoadTimer += Time.deltaTime;
                // Fast 0.45s response: respawns immediately without long fall delay
                if (offRoadTimer >= 0.45f)
                {
                    Debug.Log("[RaceManager] Car went outside the road! Instant respawn at last checkpoint...");
                    RespawnCar();
                    return;
                }
            }
            else
            {
                offRoadTimer = 0f;
            }

            // 3. Upside down / rolled over on roof for > 1.5 seconds
            if (Vector3.Dot(playerCar.transform.up, Vector3.up) < 0.15f)
            {
                upsideDownTimer += Time.deltaTime;
                if (upsideDownTimer >= 1.5f)
                {
                    Debug.Log("[RaceManager] Car rolled upside down. Respawning at last checkpoint...");
                    RespawnCar();
                    return;
                }
            }
            else
            {
                upsideDownTimer = 0f;
            }
        }

        private void HandleCheckpointReached(Checkpoint cp, Collider carCol)
        {
            if (CurrentState != RaceState.Racing || isFinished) return;

            if (cp.CheckpointIndex == CurrentCheckpointIndex)
            {
                cp.SetPassed(true);
                // Checkpoint transform is at pos + 2.5f; place respawn directly on the road surface (0.35m above asphalt)
                lastRespawnPosition = cp.transform.position - Vector3.up * 2.15f;
                lastRespawnRotation = cp.transform.rotation;

                CurrentCheckpointIndex++;
                OnCheckpointReached?.Invoke(CurrentCheckpointIndex, TotalCheckpoints);
                Debug.Log($"[RaceManager] Checkpoint {CurrentCheckpointIndex}/{TotalCheckpoints} cleared!");

                // If no dedicated finish line exists, reaching all checkpoints finishes the race
                if (finishLine == null && CurrentCheckpointIndex >= TotalCheckpoints)
                {
                    FinishRace();
                }
            }
        }

        private void HandleFinishLineCrossed(Collider carCol)
        {
            if (CurrentState != RaceState.Racing || isFinished) return;

            // Only allow finishing if all intermediate checkpoints have been cleared!
            if (TotalCheckpoints > 0 && CurrentCheckpointIndex < TotalCheckpoints)
            {
                Debug.Log($"[RaceManager] Crossed finish line but only cleared {CurrentCheckpointIndex}/{TotalCheckpoints} checkpoints.");
                return;
            }

            FinishRace();
        }

        public void FinishRace()
        {
            if (isFinished) return;
            isFinished = true;

            SetState(RaceState.Completed);

            int currentLvl = 1;
            if (levelData != null && levelData.levelNumber > 0)
            {
                currentLvl = levelData.levelNumber;
            }
            else
            {
                string sName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (sName.StartsWith("Level") && int.TryParse(sName.Substring(5), out int parsed))
                {
                    currentLvl = parsed;
                }
            }

            bool isNewBest = false;

            float prevBest = SaveManager.GetBestTime(currentLvl);
            if (prevBest < 0f || CurrentRaceTime < prevBest)
            {
                isNewBest = true;
            }

            SaveManager.SaveBestTime(currentLvl, CurrentRaceTime);

            // Unlock next level (e.g. Completing Level 4 unlocks Level 5)
            int nextLevelToUnlock = currentLvl + 1;
            SaveManager.UnlockLevel(nextLevelToUnlock);

            // Gently brake the car
            if (playerCar != null && playerCar.Rigidbody != null)
            {
                playerCar.Rigidbody.linearDamping = 4f;
            }

            OnRaceFinished?.Invoke(CurrentRaceTime, isNewBest);
            Debug.Log($"<color=green><b>[RaceManager] Level {currentLvl} Completed! Unlocked Level {nextLevelToUnlock}. Time: {CurrentRaceTime:F2}s. New best: {isNewBest}.</b></color>");
        }

        private void HandleTimeUp()
        {
            if (isFinished) return;
            SetState(RaceState.TimeUp);
            OnTimeUp?.Invoke();
            Debug.Log("<color=red>[RaceManager] TIME UP!</color>");
        }

        public void RespawnCar()
        {
            if (playerCar != null && !isFinished)
            {
                playerCar.ResetToPose(lastRespawnPosition, lastRespawnRotation);
                offRoadTimer = 0f;
                upsideDownTimer = 0f;
                respawnGraceTimer = 1.2f;
                Debug.Log("[RaceManager] Car respawned safely on road at last checkpoint.");
            }
        }

        public void SetState(RaceState newState)
        {
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }
    }
}
