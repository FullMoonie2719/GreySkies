using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace GreySkies
{
    public class BallisticsSystem : NetworkBehaviour
    {
        public static BallisticsSystem Instance { get; private set; }

        private class Bullet
        {
            public Vector3 Position;
            public Vector3 Velocity;
            public float Drag;
            public ulong OwnerId;
            public float Damage;
        }

        private List<Bullet> _activeBullets = new List<Bullet>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void SpawnBullet(Vector3 position, Vector3 direction, float muzzleVelocity, float dragCoefficient, ulong ownerId)
        {
            if (!IsServer) return;

            Bullet bullet = new Bullet
            {
                Position = position,
                Velocity = direction.normalized * muzzleVelocity,
                Drag = dragCoefficient,
                OwnerId = ownerId,
                Damage = 40f
            };

            _activeBullets.Add(bullet);
        }

        private void Update()
        {
            if (!IsServer) return;

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f) return;

            Vector3 gravity = Physics.gravity;

            for (int i = _activeBullets.Count - 1; i >= 0; i--)
            {
                Bullet bullet = _activeBullets[i];
                Vector3 oldPosition = bullet.Position;

                // New velocity: Velocity += Gravity * Time.deltaTime - Drag * Velocity.normalized * Velocity.sqrMagnitude * Time.deltaTime
                Vector3 dragForce = bullet.Drag * bullet.Velocity.normalized * bullet.Velocity.sqrMagnitude;
                bullet.Velocity += (gravity - dragForce) * deltaTime;

                // New position: Position + Velocity * Time.deltaTime
                bullet.Position += bullet.Velocity * deltaTime;

                // Raycast from old position to new position
                Vector3 direction = bullet.Position - oldPosition;
                float distance = direction.magnitude;

                if (distance > 0.0001f)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(oldPosition, direction.normalized, out hit, distance))
                    {
                        HandleBulletHit(bullet, hit);
                        _activeBullets.RemoveAt(i);
                        continue;
                    }
                }

                // Cleanup out-of-bounds or old bullets
                if (bullet.Position.y < -100f || bullet.Position.magnitude > 10000f)
                {
                    _activeBullets.RemoveAt(i);
                }
            }
        }

        private void HandleBulletHit(Bullet bullet, RaycastHit hit)
        {
            bool hitPlayer = false;

            SurvivalStats targetStats = hit.collider.GetComponentInParent<SurvivalStats>();
            if (targetStats != null)
            {
                hitPlayer = true;
                targetStats.TakeDamage(bullet.Damage, DamageType.Bullet);
            }

            TriggerImpactRpc(hit.point, hit.normal, hitPlayer);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void TriggerImpactRpc(Vector3 position, Vector3 normal, bool hitPlayer)
        {
            if (hitPlayer)
            {
                CreateBloodMist(position, normal);
                PlayProceduralThwack(position);
            }
            else
            {
                CreateSparks(position, normal);
                PlayProceduralClang(position);
            }
        }

        private void CreateBloodMist(Vector3 position, Vector3 normal)
        {
            GameObject go = new GameObject("BulletBloodMist");
            go.transform.position = position;
            if (normal != Vector3.zero) go.transform.rotation = Quaternion.LookRotation(normal);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.15f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            main.startColor = new Color(0.5f, 0.05f, 0.05f, 0.8f);
            main.stopAction = ParticleSystemStopAction.Destroy;
            
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.burstCount = 1;
            emission.SetBurst(0, new ParticleSystem.Burst(0, 25));
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 20f;
            shape.radius = 0.08f;
            
            ps.Play();
        }

        private void CreateSparks(Vector3 position, Vector3 normal)
        {
            GameObject go = new GameObject("BulletSparks");
            go.transform.position = position;
            if (normal != Vector3.zero) go.transform.rotation = Quaternion.LookRotation(normal);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.duration = 0.3f;
            main.loop = false;
            main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.08f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 6.0f);
            main.startColor = new Color(1.0f, 0.5f, 0.0f, 1f);
            main.stopAction = ParticleSystemStopAction.Destroy;
            
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.burstCount = 1;
            emission.SetBurst(0, new ParticleSystem.Burst(0, 15));
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 30f;
            shape.radius = 0.04f;
            
            ps.Play();
        }

        private void PlayProceduralThwack(Vector3 position)
        {
            int sampleRate = 44100;
            float duration = 0.12f;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-40f * t);
                float noise = Random.Range(-1.0f, 1.0f);
                float sine = Mathf.Sin(2 * Mathf.PI * 110f * t);
                samples[i] = (noise * 0.35f + sine * 0.65f) * envelope;
            }
            AudioClip clip = AudioClip.Create("BulletThwack", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            AudioSource.PlayClipAtPoint(clip, position, 0.8f);
        }

        private void PlayProceduralClang(Vector3 position)
        {
            int sampleRate = 44100;
            float duration = 0.25f;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-18f * t);
                float sine1 = Mathf.Sin(2 * Mathf.PI * 1300f * t);
                float sine2 = Mathf.Sin(2 * Mathf.PI * 1900f * t);
                float noise = Random.Range(-1.0f, 1.0f);
                samples[i] = (sine1 * 0.45f + sine2 * 0.35f + noise * 0.1f) * envelope;
            }
            AudioClip clip = AudioClip.Create("BulletClang", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            AudioSource.PlayClipAtPoint(clip, position, 0.7f);
        }
    }
}