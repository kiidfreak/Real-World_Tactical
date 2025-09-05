using UnityEngine;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace RealWorldTactical.Mission
{
    public class MissionManager : MonoBehaviour
    {
        [Header("Mission Settings")]
        public bool enableMissionSystem = true;
        public float missionUpdateInterval = 0.1f;
        
        // Current mission data
        private MissionData currentMission;
        private List<Objective> currentObjectives;
        private MissionStatus missionStatus;
        private float missionStartTime;
        
        // Mission tracking
        private Dictionary<string, ObjectiveStatus> objectiveStatuses;
        private List<MissionEvent> missionEvents;
        
        // UI and feedback
        private MissionUI missionUI;
        private RewardSystem rewardSystem;
        
        // Data collection
        private PlayerDataCollector dataCollector;
        
        void Start()
        {
            InitializeMissionSystem();
        }
        
        void InitializeMissionSystem()
        {
            if (!enableMissionSystem) return;
            
            objectiveStatuses = new Dictionary<string, ObjectiveStatus>();
            missionEvents = new List<MissionEvent>();
            
            // Get components
            missionUI = FindObjectOfType<MissionUI>();
            rewardSystem = FindObjectOfType<RewardSystem>();
            dataCollector = FindObjectOfType<PlayerDataCollector>();
            
            // Set initial status
            missionStatus = MissionStatus.NotStarted;
        }
        
        public void LoadMission(MissionData missionData)
        {
            if (!enableMissionSystem) return;
            
            currentMission = missionData;
            currentObjectives = new List<Objective>(missionData.objectives);
            missionStartTime = Time.time;
            missionStatus = MissionStatus.InProgress;
            
            // Initialize objective statuses
            objectiveStatuses.Clear();
            foreach (var objective in currentObjectives)
            {
                objectiveStatuses[objective.objectiveId] = ObjectiveStatus.NotStarted;
            }
            
            // Log mission start
            LogMissionEvent("mission_started", new Dictionary<string, object>
            {
                {"mission_id", currentMission.missionId},
                {"mission_name", currentMission.missionName},
                {"difficulty", currentMission.difficulty},
                {"estimated_duration", currentMission.estimatedDuration}
            });
            
            // Update UI
            missionUI?.UpdateMissionDisplay(currentMission);
            missionUI?.UpdateObjectives(currentObjectives);
        }
        
        void Update()
        {
            if (!enableMissionSystem || currentMission == null) return;
            
            if (missionStatus == MissionStatus.InProgress)
            {
                UpdateMissionProgress();
                CheckMissionCompletion();
                CheckMissionFailure();
            }
        }
        
        void UpdateMissionProgress()
        {
            // Update objective statuses
            foreach (var objective in currentObjectives)
            {
                if (objectiveStatuses[objective.objectiveId] == ObjectiveStatus.NotStarted)
                {
                    if (CheckObjectiveCompletion(objective))
                    {
                        CompleteObjective(objective);
                    }
                }
            }
            
            // Update UI
            missionUI?.UpdateObjectiveStatuses(objectiveStatuses);
        }
        
        bool CheckObjectiveCompletion(Objective objective)
        {
            switch (objective.type)
            {
                case ObjectiveType.ReachLocation:
                    return CheckReachLocationObjective(objective);
                case ObjectiveType.CollectItem:
                    return CheckCollectItemObjective(objective);
                case ObjectiveType.AvoidDetection:
                    return CheckAvoidDetectionObjective(objective);
                case ObjectiveType.CompleteInTime:
                    return CheckTimeObjective(objective);
                case ObjectiveType.InteractWithObject:
                    return CheckInteractionObjective(objective);
                default:
                    return false;
            }
        }
        
        bool CheckReachLocationObjective(Objective objective)
        {
            Vector3 playerPosition = transform.position;
            Vector3 targetPosition = objective.targetPosition;
            float distance = Vector3.Distance(playerPosition, targetPosition);
            
            return distance <= objective.completionRadius;
        }
        
        bool CheckCollectItemObjective(Objective objective)
        {
            // Check if the item has been collected
            // This would be implemented based on your item collection system
            return false; // Placeholder
        }
        
        bool CheckAvoidDetectionObjective(Objective objective)
        {
            // Check if player has been detected
            // This would be implemented based on your detection system
            return false; // Placeholder
        }
        
        bool CheckTimeObjective(Objective objective)
        {
            float elapsedTime = Time.time - missionStartTime;
            return elapsedTime <= objective.timeLimit;
        }
        
        bool CheckInteractionObjective(Objective objective)
        {
            // Check if the required interaction has been completed
            // This would be implemented based on your interaction system
            return false; // Placeholder
        }
        
        void CompleteObjective(Objective objective)
        {
            objectiveStatuses[objective.objectiveId] = ObjectiveStatus.Completed;
            
            // Log objective completion
            LogMissionEvent("objective_completed", new Dictionary<string, object>
            {
                {"objective_id", objective.objectiveId},
                {"objective_type", objective.type.ToString()},
                {"completion_time", Time.time - missionStartTime},
                {"reward", objective.reward}
            });
            
            // Award reward
            rewardSystem?.AwardReward(objective.reward, $"Objective: {objective.description}");
            
            // Update UI
            missionUI?.UpdateObjectiveStatuses(objectiveStatuses);
        }
        
        void CheckMissionCompletion()
        {
            bool allObjectivesCompleted = true;
            foreach (var status in objectiveStatuses.Values)
            {
                if (status != ObjectiveStatus.Completed)
                {
                    allObjectivesCompleted = false;
                    break;
                }
            }
            
            if (allObjectivesCompleted)
            {
                CompleteMission();
            }
        }
        
        void CheckMissionFailure()
        {
            // Check for mission failure conditions
            if (currentMission.timeLimit > 0)
            {
                float elapsedTime = Time.time - missionStartTime;
                if (elapsedTime > currentMission.timeLimit)
                {
                    FailMission("Time limit exceeded");
                }
            }
            
            // Add other failure conditions as needed
        }
        
        void CompleteMission()
        {
            missionStatus = MissionStatus.Completed;
            float completionTime = Time.time - missionStartTime;
            
            // Calculate total reward
            float totalReward = 0f;
            foreach (var objective in currentObjectives)
            {
                if (objectiveStatuses[objective.objectiveId] == ObjectiveStatus.Completed)
                {
                    totalReward += objective.reward;
                }
            }
            
            // Add completion bonus
            float completionBonus = CalculateCompletionBonus(completionTime);
            totalReward += completionBonus;
            
            // Log mission completion
            LogMissionEvent("mission_completed", new Dictionary<string, object>
            {
                {"mission_id", currentMission.missionId},
                {"completion_time", completionTime},
                {"total_reward", totalReward},
                {"completion_bonus", completionBonus}
            });
            
            // Award total reward
            rewardSystem?.AwardReward(totalReward, $"Mission: {currentMission.missionName}");
            
            // Update UI
            missionUI?.ShowMissionComplete(totalReward, completionTime);
            
            // Save mission data
            SaveMissionData();
        }
        
        void FailMission(string reason)
        {
            missionStatus = MissionStatus.Failed;
            float failureTime = Time.time - missionStartTime;
            
            // Log mission failure
            LogMissionEvent("mission_failed", new Dictionary<string, object>
            {
                {"mission_id", currentMission.missionId},
                {"failure_reason", reason},
                {"failure_time", failureTime}
            });
            
            // Update UI
            missionUI?.ShowMissionFailed(reason);
            
            // Save mission data
            SaveMissionData();
        }
        
        float CalculateCompletionBonus(float completionTime)
        {
            if (currentMission.estimatedDuration <= 0) return 0f;
            
            float timeRatio = completionTime / currentMission.estimatedDuration;
            float bonusMultiplier = Mathf.Clamp(2f - timeRatio, 0.5f, 2f);
            
            return currentMission.baseReward * bonusMultiplier;
        }
        
        void LogMissionEvent(string eventType, Dictionary<string, object> eventData)
        {
            var missionEvent = new MissionEvent
            {
                eventId = Guid.NewGuid().ToString(),
                eventType = eventType,
                timestamp = DateTime.UtcNow,
                gameTime = Time.time - missionStartTime,
                missionId = currentMission?.missionId,
                eventData = eventData
            };
            
            missionEvents.Add(missionEvent);
            
            // Also log to player data collector
            dataCollector?.LogEvent($"mission_{eventType}", eventData);
        }
        
        void SaveMissionData()
        {
            if (currentMission == null) return;
            
            var missionResult = new MissionResult
            {
                missionId = currentMission.missionId,
                missionName = currentMission.missionName,
                status = missionStatus,
                startTime = missionStartTime,
                endTime = Time.time,
                duration = Time.time - missionStartTime,
                objectives = currentObjectives,
                objectiveStatuses = objectiveStatuses,
                events = missionEvents
            };
            
            string jsonData = JsonConvert.SerializeObject(missionResult, Formatting.Indented);
            string fileName = $"mission_{currentMission.missionId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, "MissionData", fileName);
            
            // Ensure directory exists
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath));
            
            // Save to file
            System.IO.File.WriteAllText(filePath, jsonData);
            
            Debug.Log($"Mission data saved to: {filePath}");
        }
        
        // Public getters
        public MissionData GetCurrentMission() => currentMission;
        public MissionStatus GetMissionStatus() => missionStatus;
        public Dictionary<string, ObjectiveStatus> GetObjectiveStatuses() => objectiveStatuses;
        public List<Objective> GetCurrentObjectives() => currentObjectives;
    }
    
    // Enums and data structures
    public enum MissionStatus
    {
        NotStarted,
        InProgress,
        Completed,
        Failed,
        Paused
    }
    
    public enum ObjectiveStatus
    {
        NotStarted,
        InProgress,
        Completed,
        Failed
    }
    
    public enum ObjectiveType
    {
        ReachLocation,
        CollectItem,
        AvoidDetection,
        CompleteInTime,
        InteractWithObject
    }
    
    [Serializable]
    public class MissionData
    {
        public string missionId;
        public string missionName;
        public string description;
        public string environmentId;
        public string clientId;
        public MissionDifficulty difficulty;
        public float baseReward;
        public float timeLimit;
        public float estimatedDuration;
        public List<Objective> objectives;
    }
    
    [Serializable]
    public class Objective
    {
        public string objectiveId;
        public string description;
        public ObjectiveType type;
        public Vector3 targetPosition;
        public float completionRadius;
        public float timeLimit;
        public float reward;
        public Dictionary<string, object> parameters;
    }
    
    [Serializable]
    public class MissionEvent
    {
        public string eventId;
        public string eventType;
        public DateTime timestamp;
        public float gameTime;
        public string missionId;
        public Dictionary<string, object> eventData;
    }
    
    [Serializable]
    public class MissionResult
    {
        public string missionId;
        public string missionName;
        public MissionStatus status;
        public float startTime;
        public float endTime;
        public float duration;
        public List<Objective> objectives;
        public Dictionary<string, ObjectiveStatus> objectiveStatuses;
        public List<MissionEvent> events;
    }
    
    public enum MissionDifficulty
    {
        Rookie,
        Professional,
        Elite
    }
}
