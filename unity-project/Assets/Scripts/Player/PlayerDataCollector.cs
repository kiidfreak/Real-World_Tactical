using UnityEngine;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace RealWorldTactical.Player
{
    public class PlayerDataCollector : MonoBehaviour
    {
        [Header("Data Collection Settings")]
        public bool enableDataCollection = true;
        public float dataUpdateInterval = 0.1f;
        public int maxDataPoints = 10000;
        
        // Data structures
        private PlayerSessionData sessionData;
        private List<PlayerEvent> eventLog;
        private List<MovementData> movementLog;
        private List<InteractionData> interactionLog;
        private PerformanceMetrics performanceMetrics;
        
        // Timing
        private float lastDataUpdate;
        private float sessionStartTime;
        
        // Mission tracking
        private string currentMissionId;
        private string currentEnvironmentId;
        
        void Start()
        {
            InitializeDataCollection();
        }
        
        void InitializeDataCollection()
        {
            if (!enableDataCollection) return;
            
            sessionStartTime = Time.time;
            eventLog = new List<PlayerEvent>();
            movementLog = new List<MovementData>();
            interactionLog = new List<InteractionData>();
            
            sessionData = new PlayerSessionData
            {
                sessionId = Guid.NewGuid().ToString(),
                playerId = GetPlayerId(),
                startTime = DateTime.UtcNow,
                environmentId = currentEnvironmentId,
                missionId = currentMissionId
            };
            
            performanceMetrics = new PerformanceMetrics();
            
            // Log session start
            LogEvent("session_start", new Dictionary<string, object>
            {
                {"session_id", sessionData.sessionId},
                {"player_id", sessionData.playerId},
                {"environment_id", currentEnvironmentId},
                {"mission_id", currentMissionId}
            });
        }
        
        void Update()
        {
            if (!enableDataCollection) return;
            
            if (Time.time - lastDataUpdate > dataUpdateInterval)
            {
                UpdatePerformanceMetrics();
                lastDataUpdate = Time.time;
            }
        }
        
        public void LogEvent(string eventType, Dictionary<string, object> eventData)
        {
            if (!enableDataCollection) return;
            
            var playerEvent = new PlayerEvent
            {
                eventId = Guid.NewGuid().ToString(),
                eventType = eventType,
                timestamp = DateTime.UtcNow,
                gameTime = Time.time - sessionStartTime,
                position = transform.position,
                rotation = transform.rotation,
                eventData = eventData
            };
            
            eventLog.Add(playerEvent);
            
            // Limit log size
            if (eventLog.Count > maxDataPoints)
            {
                eventLog.RemoveAt(0);
            }
        }
        
        public void LogMovement(Vector3 position, float speed, bool isRunning)
        {
            if (!enableDataCollection) return;
            
            var movementData = new MovementData
            {
                timestamp = DateTime.UtcNow,
                gameTime = Time.time - sessionStartTime,
                position = position,
                speed = speed,
                isRunning = isRunning,
                isGrounded = GetComponent<CharacterController>()?.isGrounded ?? false
            };
            
            movementLog.Add(movementData);
            
            // Limit log size
            if (movementLog.Count > maxDataPoints)
            {
                movementLog.RemoveAt(0);
            }
        }
        
        public void LogInteraction(string objectName, Vector3 targetPosition, string interactionType)
        {
            if (!enableDataCollection) return;
            
            var interactionData = new InteractionData
            {
                timestamp = DateTime.UtcNow,
                gameTime = Time.time - sessionStartTime,
                playerPosition = transform.position,
                targetPosition = targetPosition,
                objectName = objectName,
                interactionType = interactionType
            };
            
            interactionLog.Add(interactionData);
            
            // Limit log size
            if (interactionLog.Count > maxDataPoints)
            {
                interactionLog.RemoveAt(0);
            }
        }
        
        void UpdatePerformanceMetrics()
        {
            performanceMetrics.frameRate = 1.0f / Time.deltaTime;
            performanceMetrics.memoryUsage = System.GC.GetTotalMemory(false);
            performanceMetrics.cpuUsage = Time.deltaTime * 1000f; // Convert to milliseconds
        }
        
        public void SetMissionContext(string missionId, string environmentId)
        {
            currentMissionId = missionId;
            currentEnvironmentId = environmentId;
            
            if (sessionData != null)
            {
                sessionData.missionId = missionId;
                sessionData.environmentId = environmentId;
            }
        }
        
        public string GetSessionDataJson()
        {
            if (!enableDataCollection) return null;
            
            var completeData = new CompleteSessionData
            {
                sessionData = sessionData,
                events = eventLog,
                movements = movementLog,
                interactions = interactionLog,
                performance = performanceMetrics,
                endTime = DateTime.UtcNow,
                totalPlayTime = Time.time - sessionStartTime
            };
            
            return JsonConvert.SerializeObject(completeData, Formatting.Indented);
        }
        
        public void SaveSessionData()
        {
            if (!enableDataCollection) return;
            
            string jsonData = GetSessionDataJson();
            string fileName = $"session_{sessionData.sessionId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, "SessionData", fileName);
            
            // Ensure directory exists
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath));
            
            // Save to file
            System.IO.File.WriteAllText(filePath, jsonData);
            
            Debug.Log($"Session data saved to: {filePath}");
        }
        
        string GetPlayerId()
        {
            // In a real implementation, this would come from authentication
            // For now, use a combination of device ID and player prefs
            string deviceId = SystemInfo.deviceUniqueIdentifier;
            string playerId = PlayerPrefs.GetString("PlayerID", "");
            
            if (string.IsNullOrEmpty(playerId))
            {
                playerId = Guid.NewGuid().ToString();
                PlayerPrefs.SetString("PlayerID", playerId);
            }
            
            return playerId;
        }
        
        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveSessionData();
            }
        }
        
        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                SaveSessionData();
            }
        }
        
        void OnDestroy()
        {
            if (enableDataCollection)
            {
                SaveSessionData();
            }
        }
    }
    
    // Data structures for serialization
    [Serializable]
    public class PlayerSessionData
    {
        public string sessionId;
        public string playerId;
        public DateTime startTime;
        public string environmentId;
        public string missionId;
    }
    
    [Serializable]
    public class PlayerEvent
    {
        public string eventId;
        public string eventType;
        public DateTime timestamp;
        public float gameTime;
        public Vector3 position;
        public Quaternion rotation;
        public Dictionary<string, object> eventData;
    }
    
    [Serializable]
    public class MovementData
    {
        public DateTime timestamp;
        public float gameTime;
        public Vector3 position;
        public float speed;
        public bool isRunning;
        public bool isGrounded;
    }
    
    [Serializable]
    public class InteractionData
    {
        public DateTime timestamp;
        public float gameTime;
        public Vector3 playerPosition;
        public Vector3 targetPosition;
        public string objectName;
        public string interactionType;
    }
    
    [Serializable]
    public class PerformanceMetrics
    {
        public float frameRate;
        public long memoryUsage;
        public float cpuUsage;
    }
    
    [Serializable]
    public class CompleteSessionData
    {
        public PlayerSessionData sessionData;
        public List<PlayerEvent> events;
        public List<MovementData> movements;
        public List<InteractionData> interactions;
        public PerformanceMetrics performance;
        public DateTime endTime;
        public float totalPlayTime;
    }
}
