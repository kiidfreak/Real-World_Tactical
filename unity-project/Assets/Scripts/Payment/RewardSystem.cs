using UnityEngine;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace RealWorldTactical.Payment
{
    public class RewardSystem : MonoBehaviour
    {
        [Header("Reward Settings")]
        public bool enableRewards = true;
        public float minimumPayout = 0.01f;
        public string currency = "USDC";
        
        // Wallet integration
        private WalletManager walletManager;
        private TransactionManager transactionManager;
        
        // Reward tracking
        private List<RewardTransaction> rewardHistory;
        private float totalEarned;
        private float pendingRewards;
        
        // Player data
        private string playerId;
        private string playerWalletAddress;
        
        void Start()
        {
            InitializeRewardSystem();
        }
        
        void InitializeRewardSystem()
        {
            if (!enableRewards) return;
            
            rewardHistory = new List<RewardTransaction>();
            totalEarned = 0f;
            pendingRewards = 0f;
            
            // Get components
            walletManager = FindObjectOfType<WalletManager>();
            transactionManager = FindObjectOfType<TransactionManager>();
            
            // Get player ID
            playerId = GetPlayerId();
            
            // Load reward history
            LoadRewardHistory();
        }
        
        public void AwardReward(float amount, string reason)
        {
            if (!enableRewards || amount <= 0) return;
            
            // Create reward transaction
            var reward = new RewardTransaction
            {
                transactionId = Guid.NewGuid().ToString(),
                playerId = playerId,
                amount = amount,
                currency = currency,
                reason = reason,
                timestamp = DateTime.UtcNow,
                status = TransactionStatus.Pending
            };
            
            // Add to history
            rewardHistory.Add(reward);
            pendingRewards += amount;
            
            // Log reward
            Debug.Log($"Reward awarded: {amount} {currency} for {reason}");
            
            // Show UI notification
            ShowRewardNotification(amount, reason);
            
            // Process payment
            ProcessRewardPayment(reward);
        }
        
        void ProcessRewardPayment(RewardTransaction reward)
        {
            if (walletManager != null && walletManager.IsWalletConnected())
            {
                // Process through blockchain
                ProcessBlockchainPayment(reward);
            }
            else
            {
                // Store for later processing
                StorePendingReward(reward);
            }
        }
        
        async void ProcessBlockchainPayment(RewardTransaction reward)
        {
            try
            {
                // Get player wallet address
                playerWalletAddress = await walletManager.GetWalletAddress();
                
                if (string.IsNullOrEmpty(playerWalletAddress))
                {
                    Debug.LogError("No wallet address available");
                    reward.status = TransactionStatus.Failed;
                    return;
                }
                
                // Create transaction
                var transaction = new TransactionData
                {
                    transactionId = reward.transactionId,
                    fromAddress = GetContractAddress(),
                    toAddress = playerWalletAddress,
                    amount = reward.amount,
                    currency = reward.currency,
                    gasLimit = 21000,
                    gasPrice = await GetGasPrice()
                };
                
                // Send transaction
                string txHash = await transactionManager.SendTransaction(transaction);
                
                if (!string.IsNullOrEmpty(txHash))
                {
                    reward.status = TransactionStatus.Completed;
                    reward.transactionHash = txHash;
                    totalEarned += reward.amount;
                    pendingRewards -= reward.amount;
                    
                    Debug.Log($"Reward paid: {reward.amount} {reward.currency} (TX: {txHash})");
                }
                else
                {
                    reward.status = TransactionStatus.Failed;
                    Debug.LogError("Failed to send reward transaction");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error processing reward payment: {e.Message}");
                reward.status = TransactionStatus.Failed;
            }
            
            // Save updated history
            SaveRewardHistory();
        }
        
        void StorePendingReward(RewardTransaction reward)
        {
            // Store in local storage for later processing
            string pendingRewardsJson = PlayerPrefs.GetString("PendingRewards", "[]");
            var pendingRewardsList = JsonConvert.DeserializeObject<List<RewardTransaction>>(pendingRewardsJson) ?? new List<RewardTransaction>();
            
            pendingRewardsList.Add(reward);
            
            string updatedJson = JsonConvert.SerializeObject(pendingRewardsList);
            PlayerPrefs.SetString("PendingRewards", updatedJson);
            
            Debug.Log($"Reward stored for later processing: {reward.amount} {reward.currency}");
        }
        
        public void ProcessPendingRewards()
        {
            if (!walletManager.IsWalletConnected()) return;
            
            string pendingRewardsJson = PlayerPrefs.GetString("PendingRewards", "[]");
            var pendingRewardsList = JsonConvert.DeserializeObject<List<RewardTransaction>>(pendingRewardsJson) ?? new List<RewardTransaction>();
            
            foreach (var reward in pendingRewardsList)
            {
                if (reward.status == TransactionStatus.Pending)
                {
                    ProcessBlockchainPayment(reward);
                }
            }
            
            // Clear processed rewards
            var unprocessedRewards = pendingRewardsList.FindAll(r => r.status == TransactionStatus.Pending);
            string updatedJson = JsonConvert.SerializeObject(unprocessedRewards);
            PlayerPrefs.SetString("PendingRewards", updatedJson);
        }
        
        void ShowRewardNotification(float amount, string reason)
        {
            // Create UI notification
            var notification = new RewardNotification
            {
                amount = amount,
                currency = currency,
                reason = reason,
                timestamp = DateTime.UtcNow
            };
            
            // Show in UI
            var uiManager = FindObjectOfType<UIManager>();
            uiManager?.ShowRewardNotification(notification);
        }
        
        string GetPlayerId()
        {
            string playerId = PlayerPrefs.GetString("PlayerID", "");
            if (string.IsNullOrEmpty(playerId))
            {
                playerId = Guid.NewGuid().ToString();
                PlayerPrefs.SetString("PlayerID", playerId);
            }
            return playerId;
        }
        
        string GetContractAddress()
        {
            // Return the smart contract address for rewards
            // This would be configured based on your deployment
            return "0x1234567890123456789012345678901234567890"; // Placeholder
        }
        
        async System.Threading.Tasks.Task<float> GetGasPrice()
        {
            // Get current gas price from blockchain
            // This would be implemented based on your blockchain provider
            return 0.00000002f; // Placeholder
        }
        
        void LoadRewardHistory()
        {
            string historyJson = PlayerPrefs.GetString("RewardHistory", "[]");
            rewardHistory = JsonConvert.DeserializeObject<List<RewardTransaction>>(historyJson) ?? new List<RewardTransaction>();
            
            // Calculate totals
            totalEarned = 0f;
            pendingRewards = 0f;
            
            foreach (var reward in rewardHistory)
            {
                if (reward.status == TransactionStatus.Completed)
                {
                    totalEarned += reward.amount;
                }
                else if (reward.status == TransactionStatus.Pending)
                {
                    pendingRewards += reward.amount;
                }
            }
        }
        
        void SaveRewardHistory()
        {
            string historyJson = JsonConvert.SerializeObject(rewardHistory);
            PlayerPrefs.SetString("RewardHistory", historyJson);
        }
        
        // Public getters
        public float GetTotalEarned() => totalEarned;
        public float GetPendingRewards() => pendingRewards;
        public List<RewardTransaction> GetRewardHistory() => new List<RewardTransaction>(rewardHistory);
        public string GetPlayerId() => playerId;
        public string GetCurrency() => currency;
    }
    
    // Data structures
    [Serializable]
    public class RewardTransaction
    {
        public string transactionId;
        public string playerId;
        public float amount;
        public string currency;
        public string reason;
        public DateTime timestamp;
        public TransactionStatus status;
        public string transactionHash;
    }
    
    [Serializable]
    public class RewardNotification
    {
        public float amount;
        public string currency;
        public string reason;
        public DateTime timestamp;
    }
    
    [Serializable]
    public class TransactionData
    {
        public string transactionId;
        public string fromAddress;
        public string toAddress;
        public float amount;
        public string currency;
        public int gasLimit;
        public float gasPrice;
    }
    
    public enum TransactionStatus
    {
        Pending,
        Processing,
        Completed,
        Failed
    }
}
