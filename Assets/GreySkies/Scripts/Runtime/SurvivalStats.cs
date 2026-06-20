using UnityEngine;
using Unity.Netcode;

namespace GreySkies
{
    public enum DamageType
    {
        Physical,
        Bullet,
        Melee,
        Bleeding,
        Hunger,
        Thirst,
        Cold
    }

    [RequireComponent(typeof(NetworkObject))]
    public class SurvivalStats : NetworkBehaviour
    {
        [Header("Stat Network Variables")]
        public NetworkVariable<float> Health = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<float> Stamina = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<float> Hunger = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<float> Thirst = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<float> Temperature = new NetworkVariable<float>(36.6f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<int> BleedingStacks = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        [Header("Survival Settings (Seconds per unit decay)")]
        public float HungerDecayRate = 0.05f; // hunger lost per second
        public float ThirstDecayRate = 0.08f; // thirst lost per second (dehydrates 1.5x faster than hunger)
        public float BleedingDamageRate = 2.0f; // health lost per bleeding stack per second
        public float StarvationDamageRate = 1.0f; // health lost per second when starving (hunger = 0)
        public float DehydrationDamageRate = 1.5f; // health lost per second when dehydrated (thirst = 0)

        [Header("Stamina Settings")]
        public float StaminaSprintDrain = 15.0f; // stamina lost per second while sprinting
        public float StaminaRegenRate = 20.0f; // stamina recovered per second when idle/walking
        public float JumpStaminaCost = 10.0f;

        private NetworkPlayerController _playerController;

        private void Awake()
        {
            _playerController = GetComponent<NetworkPlayerController>();
        }

        private void Update()
        {
            if (!IsServer) return;

            UpdateDecays();
            UpdateStamina();
            UpdateHealthDamages();
        }

        private void UpdateDecays()
        {
            // Authoritative hunger/thirst decay
            float newHunger = Mathf.Clamp(Hunger.Value - HungerDecayRate * Time.deltaTime, 0f, 100f);
            float newThirst = Mathf.Clamp(Thirst.Value - ThirstDecayRate * Time.deltaTime, 0f, 100f);

            Hunger.Value = newHunger;
            Thirst.Value = newThirst;

            // Environmental temperature simulation
            // Simple model: temperature decreases slightly if player is cold/wet, and stabilizes towards 36.6f
            float targetTemp = 36.6f;
            // Let's assume if player is in wet areas (or outside in cold) it might drop
            if (BleedingStacks.Value > 2)
            {
                targetTemp = 34.5f; // severe blood loss causes hypothermia
            }
            Temperature.Value = Mathf.Lerp(Temperature.Value, targetTemp, Time.deltaTime * 0.02f);
        }

        private void UpdateStamina()
        {
            bool isSprinting = false;
            bool isMoving = false;

            if (_playerController != null)
            {
                isSprinting = _playerController.IsSprinting();
                isMoving = _playerController.IsMoving();
            }

            float currentStamina = Stamina.Value;
            if (isSprinting && isMoving)
            {
                currentStamina = Mathf.Clamp(currentStamina - StaminaSprintDrain * Time.deltaTime, 0f, 100f);
            }
            else
            {
                currentStamina = Mathf.Clamp(currentStamina + StaminaRegenRate * Time.deltaTime, 0f, 100f);
            }

            Stamina.Value = currentStamina;
        }

        private void UpdateHealthDamages()
        {
            float healthDamage = 0f;

            // 1. Bleeding damage
            if (BleedingStacks.Value > 0)
            {
                healthDamage += BleedingStacks.Value * BleedingDamageRate * Time.deltaTime;
            }

            // 2. Starvation damage
            if (Hunger.Value <= 0.01f)
            {
                healthDamage += StarvationDamageRate * Time.deltaTime;
            }

            // 3. Dehydration damage
            if (Thirst.Value <= 0.01f)
            {
                healthDamage += DehydrationDamageRate * Time.deltaTime;
            }

            if (healthDamage > 0f)
            {
                TakeDamage(healthDamage, DamageType.Bleeding);
            }
        }

        public void TakeDamage(float amount, DamageType type)
        {
            if (!IsServer) return;

            float newHealth = Mathf.Clamp(Health.Value - amount, 0f, 100f);
            Health.Value = newHealth;

            if (newHealth <= 0f)
            {
                OnDeath();
            }
        }

        private void OnDeath()
        {
            // Server triggers client-side death sequences
            TriggerDeathRpc();
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void TriggerDeathRpc()
        {
            // Let local SomaticFeedbackManager handle death screen and effects
            var feedback = FindAnyObjectByType<SomaticFeedbackManager>();
            if (feedback != null)
            {
                feedback.PlayDeathSequence();
            }
        }

        // Server RPC Actions
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ApplyJumpStaminaDrainServerRpc()
        {
            Stamina.Value = Mathf.Max(0f, Stamina.Value - JumpStaminaCost);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ApplyDamageServerRpc(float amount, DamageType type)
        {
            TakeDamage(amount, type);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void AddBleedingStackServerRpc(int amount)
        {
            BleedingStacks.Value = Mathf.Clamp(BleedingStacks.Value + amount, 0, 5);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void BandageWoundServerRpc()
        {
            BleedingStacks.Value = Mathf.Max(0, BleedingStacks.Value - 1);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ConsumeFoodServerRpc(float hungerAmount)
        {
            Hunger.Value = Mathf.Clamp(Hunger.Value + hungerAmount, 0f, 100f);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ConsumeWaterServerRpc(float thirstAmount)
        {
            Thirst.Value = Mathf.Clamp(Thirst.Value + thirstAmount, 0f, 100f);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ConsumeMedicationServerRpc(float healthAmount)
        {
            Health.Value = Mathf.Clamp(Health.Value + healthAmount, 0f, 100f);
        }
    }
}