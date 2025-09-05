using UnityEngine;
using System.Collections.Generic;

public class MissionManager : MonoBehaviour
{
    [Header("Mission Settings")]
    public bool enableMissionSystem = true;
    
    [Header("Player Reference")]
    public Transform playerTransform;
    
    // Current mission data
    private MissionData currentMission;
    private List<Objective> currentObjectives;
    private MissionStatus missionStatus;
    private float missionStartTime;
    
    // Mission tracking
    private Dictionary<string, ObjectiveStatus> objectiveStatuses;
    
    void Start()
    {
        Debug.Log("MissionManager Start() called!");
        
        // Auto-find player if not assigned
        if (playerTransform == null)
        {
            playerTransform = transform; // Use this object's transform
            Debug.Log("Player transform auto-assigned to: " + playerTransform.name);
        }
        
        InitializeMissionSystem();
    }
    
    void InitializeMissionSystem()
    {
        if (!enableMissionSystem) return;
        
        objectiveStatuses = new Dictionary<string, ObjectiveStatus>();
        missionStatus = MissionStatus.NotStarted;
        
        // Create a test mission
        CreateTestMission();
    }
    
    void CreateTestMission()
    {
        Debug.Log("CreateTestMission() called!");
        currentMission = new MissionData
        {
            missionId = "test_mission_001",
            missionName = "Test Reconnaissance",
            description = "Reach the target location and collect the intel",
            difficulty = MissionDifficulty.Rookie,
            baseReward = 5.00f,
            timeLimit = 300f,
            objectives = new List<Objective>
            {
                new Objective
                {
                    objectiveId = "reach_target",
                    type = ObjectiveType.ReachLocation,
                    description = "Reach the target location",
                    targetPosition = new Vector3(10, 0, 10),
                    completionRadius = 2.0f,
                    reward = 3.00f
                },
                new Objective
                {
                    objectiveId = "collect_intel",
                    type = ObjectiveType.CollectItem,
                    description = "Collect the intel package",
                    targetPosition = new Vector3(10, 1, 10),
                    reward = 2.00f
                }
            }
        };
        
        currentObjectives = new List<Objective>(currentMission.objectives);
        missionStartTime = Time.time;
        missionStatus = MissionStatus.InProgress;
        
        // Initialize objective statuses
        objectiveStatuses.Clear();
        foreach (var objective in currentObjectives)
        {
            objectiveStatuses[objective.objectiveId] = ObjectiveStatus.NotStarted;
        }
        
        Debug.Log($"Mission started: {currentMission.missionName}");
    }
    
    void Update()
    {
        if (!enableMissionSystem || currentMission == null) return;
        
        if (missionStatus == MissionStatus.InProgress)
        {
            UpdateMissionProgress();
            CheckMissionCompletion();
        }
    }
    
    void UpdateMissionProgress()
    {
        // Safety check
        if (playerTransform == null || currentObjectives == null)
        {
            Debug.LogWarning("MissionManager: playerTransform or currentObjectives is null!");
            return;
        }
        
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
    }
    
    bool CheckObjectiveCompletion(Objective objective)
    {
        switch (objective.type)
        {
            case ObjectiveType.ReachLocation:
                return CheckReachLocationObjective(objective);
            case ObjectiveType.CollectItem:
                return CheckCollectItemObjective(objective);
            default:
                return false;
        }
    }
    
    bool CheckReachLocationObjective(Objective objective)
    {
        Vector3 playerPosition = playerTransform.position;
        Vector3 targetPosition = objective.targetPosition;
        float distance = Vector3.Distance(playerPosition, targetPosition);
        
        return distance <= objective.completionRadius;
    }
    
    bool CheckCollectItemObjective(Objective objective)
    {
        // For now, just check if player is close to the item
        Vector3 playerPosition = playerTransform.position;
        Vector3 targetPosition = objective.targetPosition;
        float distance = Vector3.Distance(playerPosition, targetPosition);
        
        return distance <= objective.completionRadius;
    }
    
    void CompleteObjective(Objective objective)
    {
        objectiveStatuses[objective.objectiveId] = ObjectiveStatus.Completed;
        
        Debug.Log($"Objective completed: {objective.description} - Reward: ${objective.reward}");
        
        // Award reward (we'll implement this later)
        // rewardSystem?.AwardReward(objective.reward, $"Objective: {objective.description}");
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
    
    void CompleteMission()
    {
        missionStatus = MissionStatus.Completed;
        float completionTime = Time.time - missionStartTime;
        
        Debug.Log($"Mission completed in {completionTime:F1} seconds!");
        
        // Calculate total reward
        float totalReward = 0f;
        foreach (var objective in currentObjectives)
        {
            if (objectiveStatuses[objective.objectiveId] == ObjectiveStatus.Completed)
            {
                totalReward += objective.reward;
            }
        }
        
        Debug.Log($"Total reward earned: ${totalReward}");
    }
}

// Data structures
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

public enum MissionDifficulty
{
    Rookie,
    Professional,
    Elite
}

[System.Serializable]
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

[System.Serializable]
public class Objective
{
    public string objectiveId;
    public string description;
    public ObjectiveType type;
    public Vector3 targetPosition;
    public float completionRadius;
    public float timeLimit;
    public float reward;
}