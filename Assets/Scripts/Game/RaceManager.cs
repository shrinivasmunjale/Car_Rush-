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
        [SerializeField] private float maxVerticalDropBelowCheckpoint = 15f;

        [Tooltip("Time in seconds upside-down before auto-respawning.")]
        [SerializeField] private float upsideDownRespawnTime = 2.5f;

        // State
        public RaceState CurrentState { get; private set; } = RaceState.Countdown;
        public float CurrentRaceTime { get; private set; } = 0f;
        public float RemainingTime => (levelData != null && levelData.timeLimit > 0f) ? Mathf.Max(0f, levelData.timeLimit - CurrentRaceTime) : Mathf.Max(0f, 60f - CurrentRaceTime);
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
        private bool isRespawning = false;

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
                startSpawnForward  = startSpawnPoint.forward;
                lastRespawnPosition = startSpawnPoint.position;
                lastRespawnRotation = startSpawnPoint.rotation;
            }
            else if (playerCar != null)
            {
                startSpawnPosition  = playerCar.transform.position;
                startSpawnForward   = playerCar.transform.forward;
                lastRespawnPosition = playerCar.transform.position;
                lastRespawnRotation = playerCar.transform.rotation;
            }

            // Auto-add TireMarks to the car if not already present
            if (playerCar != null && playerCar.GetComponent<CarRush.Car.TireMarks>() == null)
            {
                playerCar.gameObject.AddComponent<CarRush.Car.TireMarks>();
                Debug.Log("[RaceManager] TireMarks component auto-added to car.");
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

                // Manual respawn (R key) — uses new Input System to match project settings
#if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Keyboard.current != null &&
                    UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
#else
                if (Input.GetKeyDown(KeyCode.R))
#endif
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

        /// <summary>
        /// Checks if car has driven outside the road boundaries, fallen off the track, or rolled upside-down.
        /// </summary>
        private void CheckFallAndOffTrack()
        {
            if (isFinished || playerCar == null || isRespawning) return;

            Vector3 carPos = playerCar.transform.position;

            // 1. Extreme vertical drop below absolute bottom floor
            if (carPos.y < fallThresholdY)
            {
                Debug.Log("[RaceManager] Car fell off the track (below threshold). Instant respawn at last checkpoint...");
                RespawnCar();
                return;
            }

            // 1b. Drop below last checkpoint height (detects falling before hitting bottom floor)
            if (carPos.y < lastRespawnPosition.y - maxVerticalDropBelowCheckpoint)
            {
                Debug.Log("[RaceManager] Car dropped too far below last checkpoint. Respawning...");
                RespawnCar();
                return;
            }

            // 2. Downward road surface raycast check (excluding car's own colliders & triggers)
            bool isOverRoad = false;
            Vector3 rayOrigin = carPos + Vector3.up * 0.5f;
            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, 5.0f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider col = hits[i].collider;
                if (col == null || col.isTrigger) continue;
                // Exclude the player car's own body and wheels
                if (col.transform.root == playerCar.transform.root) continue;
                if (col.attachedRigidbody == playerCar.Rigidbody) continue;

                isOverRoad = true;
                break;
            }

            // If car leaves the road completely (flies off or drives outside into void)
            if (!isOverRoad)
            {
                offRoadTimer += Time.deltaTime;
                // Fast 0.45s tolerance before respawn (avoids infinite falling loop)
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

            // 3. Upside down / rolled over on roof
            if (Vector3.Dot(playerCar.transform.up, Vector3.up) < 0.15f)
            {
                upsideDownTimer += Time.deltaTime;
                if (upsideDownTimer >= upsideDownRespawnTime)
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

                // Find the exact solid road surface under the checkpoint for safe respawn
                Vector3 roadSurface = cp.transform.position;
                if (Physics.Raycast(cp.transform.position + Vector3.up * 6f, Vector3.down, out RaycastHit hit, 20f, ~0, QueryTriggerInteraction.Ignore))
                {
                    roadSurface = hit.point + Vector3.up * 0.45f;
                }
                else
                {
                    roadSurface = cp.transform.position;
                }

                lastRespawnPosition = roadSurface;
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
            // Guard: never re-enter while already respawning (prevents the fall loop)
            if (playerCar == null || isFinished || isRespawning) return;

            isRespawning = true;
            offRoadTimer    = 0f;
            upsideDownTimer = 0f;

            // Pick a safe respawn position on top of the road surface
            Vector3 safePos = lastRespawnPosition;
            Quaternion safeRot = lastRespawnRotation;

            // Raycast down from above the target position to lock onto the actual road surface
            if (Physics.Raycast(safePos + Vector3.up * 8f, Vector3.down, out RaycastHit hit, 25f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.root != playerCar.transform.root)
                {
                    safePos = hit.point + Vector3.up * 0.45f;
                }
            }
            else if (safePos.y <= fallThresholdY || (startSpawnPoint != null && Vector3.Distance(safePos, startSpawnPosition) < 0.1f))
            {
                // Fallback to start spawn if last position has no road under it
                safePos = startSpawnPosition;
                safeRot = startSpawnPoint != null ? startSpawnPoint.rotation : Quaternion.identity;
                Debug.LogWarning("[RaceManager] Respawn position had no road underneath — falling back to start spawn.");
            }

            playerCar.ResetToPose(safePos, safeRot);
            Debug.Log("[RaceManager] Car safely respawned on road at last checkpoint.");

            StartCoroutine(RespawnCooldownRoutine());
        }

        /// <summary>
        /// Stabilizes the car physics for 1.5s after respawning so the car lands smoothly
        /// and does not immediately re-trigger fall/off-road checks.
        /// </summary>
        private IEnumerator RespawnCooldownRoutine()
        {
            offRoadTimer = 0f;
            upsideDownTimer = 0f;

            Rigidbody rb = playerCar != null ? playerCar.Rigidbody : null;

            float timer = 1.5f;
            while (timer > 0f)
            {
                timer -= Time.unscaledDeltaTime;

                // Dampen velocities during initial frame settlement
                if (timer > 1.25f && rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                yield return null;
            }

            offRoadTimer = 0f;
            upsideDownTimer = 0f;
            isRespawning = false;
        }

        public void SetState(RaceState newState)
        {
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }
    }
}
