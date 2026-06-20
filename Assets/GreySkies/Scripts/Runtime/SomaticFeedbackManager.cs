using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

namespace GreySkies
{
    public class SomaticFeedbackManager : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The URP Volume component to modify. If null, will try to find or add one.")]
        public Volume PostProcessVolume;
        
        [Tooltip("The camera to apply procedural motion, jitter, and death collapse to.")]
        public Camera PlayerCamera;

        [Header("Bleeding Pulse Settings")]
        public float BleedingPulseFrequency = 1.2f; // 1.2Hz = 72 BPM
        public Color BleedingVignetteColor = new Color(0.45f, 0.05f, 0.05f, 1f);

        [Header("Freezing Jitter Settings")]
        public float FreezingShakingFrequency = 12.0f; // Jitter rate
        public float FreezingShakingMagnitude = 0.02f; // Shiver intensity
        public Color FreezingVignetteColor = new Color(0.05f, 0.15f, 0.35f, 1f);

        private SurvivalStats _trackedStats;
        private Vector3 _cameraBaseLocalPos;
        private Quaternion _cameraBaseLocalRot;
        
        // Runtime URP overrides
        private Vignette _vignette;
        private ColorAdjustments _colorAdjustments;
        
        private float _bleedingPulseTimer;
        private bool _isDead;

        private void Start()
        {
            // Set default references if missing
            if (PlayerCamera == null)
            {
                PlayerCamera = Camera.main;
            }

            if (PlayerCamera != null)
            {
                _cameraBaseLocalPos = PlayerCamera.transform.localPosition;
                _cameraBaseLocalRot = PlayerCamera.transform.localRotation;
            }

            if (PostProcessVolume == null)
            {
                PostProcessVolume = FindAnyObjectByType<Volume>();
                if (PostProcessVolume == null)
                {
                    GameObject volObj = new GameObject("SomaticVolume");
                    PostProcessVolume = volObj.AddComponent<Volume>();
                    PostProcessVolume.isGlobal = true;
                    PostProcessVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
                }
            }

            // Extract overrides
            if (PostProcessVolume != null && PostProcessVolume.profile != null)
            {
                if (!PostProcessVolume.profile.TryGet(out _vignette))
                {
                    _vignette = PostProcessVolume.profile.Add<Vignette>(true);
                }
                if (!PostProcessVolume.profile.TryGet(out _colorAdjustments))
                {
                    _colorAdjustments = PostProcessVolume.profile.Add<ColorAdjustments>(true);
                }
            }
        }

        private void Update()
        {
            if (_isDead) return;

            // Locate local player's survival stats if not already tracked
            if (_trackedStats == null)
            {
                var localPlayer = FindAnyObjectByType<NetworkPlayerController>();
                if (localPlayer != null && localPlayer.IsOwner)
                {
                    _trackedStats = localPlayer.GetComponent<SurvivalStats>();
                    if (localPlayer.CinemachineCameraTarget != null && PlayerCamera == null)
                    {
                        PlayerCamera = Camera.main;
                    }
                }
            }

            if (_trackedStats == null) return;

            HandleBleedingFeedback();
            HandleFreezingFeedback();
            HandleLowHealthFeedback();
        }

        private void HandleBleedingFeedback()
        {
            int bleedingWounds = _trackedStats.BleedingStacks.Value;
            if (bleedingWounds <= 0) return;

            _bleedingPulseTimer += Time.deltaTime * BleedingPulseFrequency * Mathf.PI * 2f;
            float pulse = (Mathf.Sin(_bleedingPulseTimer) + 1.0f) * 0.5f; // 0 to 1

            // Dynamic vignette pulse
            if (_vignette != null)
            {
                _vignette.active = true;
                _vignette.color.Override(Color.Lerp(Color.black, BleedingVignetteColor, 0.6f));
                _vignette.intensity.Override(0.2f + pulse * 0.25f * bleedingWounds);
            }

            // Procedural camera pulse (vertical heartbeat thud)
            if (PlayerCamera != null)
            {
                float verticalOffset = -pulse * 0.05f * bleedingWounds;
                PlayerCamera.transform.localPosition = _cameraBaseLocalPos + new Vector3(0f, verticalOffset, 0f);
            }
        }

        private void HandleFreezingFeedback()
        {
            float temp = _trackedStats.Temperature.Value;
            if (temp >= 35.5f)
            {
                // Reset positional offsets if not bleeding
                if (_trackedStats.BleedingStacks.Value == 0 && PlayerCamera != null)
                {
                    PlayerCamera.transform.localPosition = _cameraBaseLocalPos;
                }
                return;
            }

            // Cold intensity increases below 35.5C
            float coldSeverity = Mathf.InverseLerp(35.5f, 33.0f, temp);

            // Dynamic blue vignette
            if (_vignette != null && _trackedStats.BleedingStacks.Value == 0)
            {
                _vignette.active = true;
                _vignette.color.Override(FreezingVignetteColor);
                _vignette.intensity.Override(0.15f + coldSeverity * 0.3f);
            }

            // Shivering camera jitter
            if (PlayerCamera != null)
            {
                float shiverX = Mathf.Sin(Time.time * FreezingShakingFrequency) * FreezingShakingMagnitude * coldSeverity;
                float shiverY = Mathf.Cos(Time.time * FreezingShakingFrequency * 0.8f) * FreezingShakingMagnitude * coldSeverity;
                
                Vector3 currentLocalPos = PlayerCamera.transform.localPosition;
                // Accumulate on top of base pos or bleeding pulse
                float basePulseY = (_trackedStats.BleedingStacks.Value > 0) ? currentLocalPos.y - _cameraBaseLocalPos.y : 0f;
                PlayerCamera.transform.localPosition = _cameraBaseLocalPos + new Vector3(shiverX, basePulseY + shiverY, 0f);
            }
        }

        private void HandleLowHealthFeedback()
        {
            float hp = _trackedStats.Health.Value;
            if (hp >= 40f)
            {
                if (_colorAdjustments != null)
                {
                    _colorAdjustments.saturation.Override(0f); // Default standard desaturation
                }
                return;
            }

            // Severe low health desaturates the environment to black and white
            float healthSeverity = Mathf.InverseLerp(40.0f, 0.0f, hp);
            float targetSaturation = -100f * healthSeverity;

            if (_colorAdjustments != null)
            {
                _colorAdjustments.active = true;
                _colorAdjustments.saturation.Override(targetSaturation);
            }
        }

        public void PlayDeathSequence()
        {
            if (_isDead) return;
            _isDead = true;
            StartCoroutine(DeathSequenceRoutine());
        }

        private IEnumerator DeathSequenceRoutine()
        {
            Debug.Log("SomaticFeedbackManager: Executing collapse sequence...");
            
            float elapsed = 0f;
            float duration = 1.5f;

            Vector3 startPos = PlayerCamera != null ? PlayerCamera.transform.position : Vector3.zero;
            Quaternion startRot = PlayerCamera != null ? PlayerCamera.transform.rotation : Quaternion.identity;

            // Target pose: camera resting near floor, tilted sideways
            Vector3 targetPos = startPos + Vector3.down * 1.5f; 
            Quaternion targetRot = startRot * Quaternion.Euler(0f, 0f, 45f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float easeOut = 1f - Mathf.Pow(1f - t, 3); // Cubic ease out

                if (PlayerCamera != null)
                {
                    PlayerCamera.transform.position = Vector3.Lerp(startPos, targetPos, easeOut);
                    PlayerCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, easeOut);
                }

                // Fade screen exposure down to -10.0 (pitch black)
                if (_colorAdjustments != null)
                {
                    _colorAdjustments.postExposure.Override(Mathf.Lerp(0f, -10f, easeOut));
                }

                yield return null;
            }

            if (PlayerCamera != null)
            {
                PlayerCamera.transform.position = targetPos;
                PlayerCamera.transform.rotation = targetRot;
            }

            if (_colorAdjustments != null)
            {
                _colorAdjustments.postExposure.Override(-10f);
            }
        }
    }
}