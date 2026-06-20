using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using System.Collections;

namespace GreySkies
{
    public enum WeaponType
    {
        Firearm,
        CricketBat
    }

    public class PlayerWeaponController : NetworkBehaviour
    {
        [Header("Weapon Settings")]
        [Tooltip("Active weapon type")]
        public WeaponType CurrentWeapon = WeaponType.Firearm;

        private NetworkPlayerController _playerController;
        private GameObject CinemachineCameraTarget => _playerController != null ? _playerController.CinemachineCameraTarget : null;

        // Input Actions
        private InputAction _fireAction;
        private InputAction _switchWeapon1;
        private InputAction _switchWeapon2;

        // Camera Shake / Recoil States
        private Vector3 _shakeRotation;
        private Vector3 _shakePosition;
        private Vector3 _defaultCameraLocalPos;
        private bool _cameraPosInitialized = false;

        private void Awake()
        {
            _playerController = GetComponent<NetworkPlayerController>();

            // Setup input bindings
            _fireAction = new InputAction("Fire", binding: "<Mouse>/leftButton");
            _switchWeapon1 = new InputAction("SwitchWeapon1", binding: "<Keyboard>/1");
            _switchWeapon2 = new InputAction("SwitchWeapon2", binding: "<Keyboard>/2");
        }

        private void Start()
        {
            if (CinemachineCameraTarget != null)
            {
                _defaultCameraLocalPos = CinemachineCameraTarget.transform.localPosition;
                _cameraPosInitialized = true;
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                _fireAction.Enable();
                _switchWeapon1.Enable();
                _switchWeapon2.Enable();
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                _fireAction.Disable();
                _switchWeapon1.Disable();
                _switchWeapon2.Disable();
            }
        }

        private void Update()
        {
            if (!IsOwner) return;

            // Handle Weapon Switching
            if (_switchWeapon1.WasPressedThisFrame())
            {
                CurrentWeapon = WeaponType.Firearm;
                Debug.Log("Switched to Firearm");
            }
            if (_switchWeapon2.WasPressedThisFrame())
            {
                CurrentWeapon = WeaponType.CricketBat;
                Debug.Log("Switched to Cricket Bat");
            }

            // Handle Firing/Attacking
            if (_fireAction.WasPressedThisFrame())
            {
                FireWeaponOrMelee();
            }
        }

        private void LateUpdate()
        {
            if (!IsOwner) return;

            if (CinemachineCameraTarget != null)
            {
                if (!_cameraPosInitialized)
                {
                    _defaultCameraLocalPos = CinemachineCameraTarget.transform.localPosition;
                    _cameraPosInitialized = true;
                }

                // Apply rotational and positional shake to the camera target
                CinemachineCameraTarget.transform.localPosition = _defaultCameraLocalPos + _shakePosition;
                CinemachineCameraTarget.transform.localRotation *= Quaternion.Euler(_shakeRotation);
            }
        }

        private void FireWeaponOrMelee()
        {
            Transform camTransform = Camera.main != null ? Camera.main.transform : (CinemachineCameraTarget != null ? CinemachineCameraTarget.transform : transform);
            Vector3 origin = camTransform.position;
            Vector3 direction = camTransform.forward;

            if (CurrentWeapon == WeaponType.Firearm)
            {
                // Immediate predicted client feedback
                ApplyRecoilPunch();
                PlayLocalFireSound(origin);

                // Authoritative server spawn
                FireWeaponServerRpc(origin, direction);
            }
            else if (CurrentWeapon == WeaponType.CricketBat)
            {
                // Immediate predicted swing sound
                PlayLocalSwingSound(origin);

                // Authoritative server melee hit detection
                MeleeSwingServerRpc(origin, direction);
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void FireWeaponServerRpc(Vector3 origin, Vector3 direction)
        {
            if (BallisticsSystem.Instance == null)
            {
                GameObject go = new GameObject("BallisticsSystem");
                go.AddComponent<NetworkObject>();
                go.AddComponent<BallisticsSystem>();
                go.GetComponent<NetworkObject>().Spawn();
            }

            // Muzzle velocity = 800m/s, Drag coefficient = 0.004f, OwnerClientId
            BallisticsSystem.Instance.SpawnBullet(origin, direction, 800f, 0.004f, OwnerClientId);
            
            // Broadcast muzzle effects to other clients
            PlayMuzzleEffectsClientRpc(origin);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void PlayMuzzleEffectsClientRpc(Vector3 origin)
        {
            if (!IsOwner)
            {
                PlayProceduralMuzzleFlash(origin);
                PlayLocalFireSound(origin);
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void MeleeSwingServerRpc(Vector3 origin, Vector3 direction)
        {
            Collider[] selfColliders = GetComponentsInChildren<Collider>();
            RaycastHit[] hits = Physics.SphereCastAll(origin, 0.5f, direction, 2.0f);

            bool hitSomething = false;
            foreach (var hit in hits)
            {
                // Ignore self-collision
                bool isSelf = false;
                foreach (var col in selfColliders)
                {
                    if (hit.collider == col)
                    {
                        isSelf = true;
                        break;
                    }
                }
                if (isSelf) continue;

                hitSomething = true;
                bool isPlayer = false;
                SurvivalStats targetStats = hit.collider.GetComponentInParent<SurvivalStats>();
                if (targetStats != null)
                {
                    isPlayer = true;
                    // Melee damage = 35 HP
                    targetStats.TakeDamage(35f, DamageType.Melee);
                }

                TriggerMeleeHitFeedbackClientRpc(hit.point, hit.normal, isPlayer, direction);
                break;
            }

            if (!hitSomething)
            {
                PlayMeleeSwooshClientRpc(origin);
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void PlayMeleeSwooshClientRpc(Vector3 origin)
        {
            if (!IsOwner)
            {
                PlayLocalSwingSound(origin);
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void TriggerMeleeHitFeedbackClientRpc(Vector3 position, Vector3 normal, bool hitPlayer, Vector3 swingDirection)
        {
            if (hitPlayer)
            {
                CreateMeleeBloodMist(position, normal);
                PlayMeleeFleshSound(position);

                if (IsOwner)
                {
                    StartCoroutine(HitStopCoroutine(0.04f)); // 0.04s local hitstop for flesh
                    ApplyMeleeShake(swingDirection);
                }
            }
            else
            {
                CreateMeleeSparks(position, normal);
                PlayMeleeDeflectSound(position);

                if (IsOwner)
                {
                    StartCoroutine(HitStopCoroutine(0.05f)); // 0.05s local hitstop for deflect
                    ApplyMeleeShake(swingDirection);
                }
            }
        }

        private IEnumerator HitStopCoroutine(float duration)
        {
            float originalTimeScale = Time.timeScale;
            Time.timeScale = 0.0f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = originalTimeScale;
        }

        private void ApplyMeleeShake(Vector3 swingDirection)
        {
            // Rotate camera punch in the direction of the swing and start exponential decay
            _shakeRotation = swingDirection * 12f;
            StartCoroutine(DecayMeleeShake());
        }

        private IEnumerator DecayMeleeShake()
        {
            float duration = 0.25f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime; // Decay during hitstop
                float t = elapsed / duration;
                float strength = Mathf.Exp(-8f * t);
                _shakeRotation = Vector3.Lerp(_shakeRotation, Vector3.zero, t * 10f);
                _shakePosition = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f)
                ) * 0.04f * strength;
                yield return null;
            }
            _shakeRotation = Vector3.zero;
            _shakePosition = Vector3.zero;
        }

        private void ApplyRecoilPunch()
        {
            // High-frequency, sharp camera recoil punch
            _shakeRotation = new Vector3(-Random.Range(3f, 5f), Random.Range(-1.5f, 1.5f), 0f);
            StartCoroutine(DecayBulletPunch());
        }

        private IEnumerator DecayBulletPunch()
        {
            float duration = 0.1f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float strength = Mathf.Exp(-12f * t);
                _shakeRotation = Vector3.Lerp(_shakeRotation, Vector3.zero, t * 15f);
                _shakePosition = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f)
                ) * 0.02f * strength;
                yield return null;
            }
            _shakeRotation = Vector3.zero;
            _shakePosition = Vector3.zero;
        }

        // Procedural Audio & Particles Helpers

        private void PlayLocalFireSound(Vector3 position)
        {
            int sampleRate = 44100;
            float duration = 0.2f;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-20f * t);
                float noise = Random.Range(-1.0f, 1.0f);
                samples[i] = noise * envelope * 0.7f;
            }
            AudioClip clip = AudioClip.Create("GunFire", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            AudioSource.PlayClipAtPoint(clip, position, 1.0f);
        }

        private void PlayLocalSwingSound(Vector3 position)
        {
            int sampleRate = 44100;
            float duration = 0.25f;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Sin(Mathf.PI * (t / duration)) * Mathf.Exp(-6f * t);
                float sine = Mathf.Sin(2 * Mathf.PI * (150f - 50f * (t / duration)) * t);
                float noise = Random.Range(-0.2f, 0.2f);
                samples[i] = (sine + noise) * envelope * 0.5f;
            }
            AudioClip clip = AudioClip.Create("SwingSwoosh", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            AudioSource.PlayClipAtPoint(clip, position, 0.8f);
        }

        private void PlayProceduralMuzzleFlash(Vector3 position)
        {
            GameObject go = new GameObject("MuzzleFlash");
            go.transform.position = position;
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.duration = 0.1f;
            main.loop = false;
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.startSpeed = 1f;
            main.startColor = new Color(1f, 0.85f, 0.4f, 1f);
            main.stopAction = ParticleSystemStopAction.Destroy;
            
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.burstCount = 1;
            emission.SetBurst(0, new ParticleSystem.Burst(0, 10));
            
            ps.Play();
        }

        private void CreateMeleeBloodMist(Vector3 position, Vector3 normal)
        {
            GameObject go = new GameObject("MeleeBloodMist");
            go.transform.position = position;
            if (normal != Vector3.zero) go.transform.rotation = Quaternion.LookRotation(normal);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.0f, 3.0f);
            main.startColor = new Color(0.5f, 0.05f, 0.05f, 0.8f);
            main.stopAction = ParticleSystemStopAction.Destroy;
            
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.burstCount = 1;
            emission.SetBurst(0, new ParticleSystem.Burst(0, 30));
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.1f;
            
            ps.Play();
        }

        private void CreateMeleeSparks(Vector3 position, Vector3 normal)
        {
            GameObject go = new GameObject("MeleeSparks");
            go.transform.position = position;
            if (normal != Vector3.zero) go.transform.rotation = Quaternion.LookRotation(normal);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.duration = 0.4f;
            main.loop = false;
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 5.0f);
            main.startColor = new Color(1.0f, 0.5f, 0.0f, 1f);
            main.stopAction = ParticleSystemStopAction.Destroy;
            
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.burstCount = 1;
            emission.SetBurst(0, new ParticleSystem.Burst(0, 20));
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.05f;
            
            ps.Play();
        }

        private void PlayMeleeFleshSound(Vector3 position)
        {
            int sampleRate = 44100;
            float duration = 0.15f;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];
            float pitchMultiplier = Random.Range(0.9f, 1.1f); // pitch randomized by ±10% per DESIGN.md
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate * pitchMultiplier;
                float envelope = Mathf.Exp(-35f * t);
                float noise = Random.Range(-1.0f, 1.0f);
                float sine = Mathf.Sin(2 * Mathf.PI * 120f * t);
                samples[i] = (noise * 0.4f + sine * 0.6f) * envelope;
            }
            AudioClip clip = AudioClip.Create("MeleeFlesh", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            AudioSource.PlayClipAtPoint(clip, position, 1.0f);
        }

        private void PlayMeleeDeflectSound(Vector3 position)
        {
            int sampleRate = 44100;
            float duration = 0.3f;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];
            float pitchMultiplier = Random.Range(0.85f, 1.15f); // pitch randomized by ±15% per DESIGN.md
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate * pitchMultiplier;
                float envelope = Mathf.Exp(-15f * t);
                float sine1 = Mathf.Sin(2 * Mathf.PI * 1200f * t);
                float sine2 = Mathf.Sin(2 * Mathf.PI * 1800f * t);
                float noise = Random.Range(-1.0f, 1.0f);
                samples[i] = (sine1 * 0.4f + sine2 * 0.3f + noise * 0.1f) * envelope;
            }
            AudioClip clip = AudioClip.Create("MeleeDeflect", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            AudioSource.PlayClipAtPoint(clip, position, 1.0f);
        }
    }
}