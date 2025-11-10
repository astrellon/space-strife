using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#nullable enable

namespace Orbits
{
    [System.Flags]
    public enum TargetFlags
    {
        None = 0,
        FlashWhenHit = 1 << 0,
        CanBeTargettedByHoming = 1 << 1,
        ShowDamageIndicator = 1 << 2,
        Invulnerable = 1 << 3,
    }

    [System.Flags]
    public enum TargetEventType
    {
        DamageTaken, Destroyed
    }

    public class Target : MonoBehaviour, IReset
    {
        #region Fields
        public const int PlayerTeam = 0;
        public const int EnemyTeam = 1;
        private static int Counter;

        public int Id;
        public int Team = 0;
        public float MaxHealth = 100.0f;
        public float CurrentHealth = 100.0f;
        public float HealthPercent => Mathf.Clamp01(this.CurrentHealth / this.MaxHealth);
        public float HitHealth = 0.0f;
        public float HitTimeout = 0.0f;
        public GameObject? ShipExplodePrefab;
        public float Size = 0.2f;
        public float DestroyDelay = 0.0f;
        public TargetFlags Flags;
        public TargetType TargetType;
        public List<Renderer> Renderers = new();
        private readonly List<Color> originalColours = new();

        public bool IsAlive => this.CurrentHealth > 0.0001f;

        private float flashCooldown = 0.0f;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (this.Flags.HasFlag(TargetFlags.FlashWhenHit))
            {
                if (!this.Renderers.Any())
                {
                    this.Renderers = this.GetComponentsInChildren<Renderer>()
                        .Where(n => n.name != "HealthBar" && !n.name.Contains("Quad")).ToList();
                }

                if (this.Flags.HasFlag(TargetFlags.FlashWhenHit))
                {
                    foreach (var renderer in this.Renderers)
                    {
                        this.originalColours.Add(renderer.material.color);
                    }
                }
            }
        }

        private void OnEnable()
        {
            this.UpdateId();
            this.CurrentHealth = this.MaxHealth;
            ProjectileManager.Instance.RegisterTarget(this);
        }

        private void OnDisable()
        {
            this.flashCooldown = 0.0f;
            for (var i = 0; i < this.Renderers.Count; i++)
            {
                this.Renderers[i].material.color = this.originalColours[i];
            }
            ProjectileManager.Instance.DeregisterTarget(this);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.grey;
            Gizmos.DrawWireSphere(this.transform.position, this.Size);
        }
        #endregion

        #region Methods
        // Should only be called for level targets!
        public void UpdateId()
        {
            this.Id = ++Counter;
        }

        public void Reset()
        {
            this.CurrentHealth = this.MaxHealth;
            this.flashCooldown = 0.0f;
        }
        public void ManagedUpdate(float dt)
        {
            this.flashCooldown -= dt;
            for (var i = 0; i < this.Renderers.Count; i++)
            {
                var colour = this.flashCooldown < 0.1f ? this.originalColours[i] : Color.white;
                this.Renderers[i].material.color = colour;
            }

            this.HitTimeout -= dt;
            if (this.HitTimeout < 0.0f)
            {
                this.HitHealth -= this.MaxHealth * dt;
            }
        }

        public bool DealDamage(float damage, ProjectileWeapon? source)
        {
            var prevHealth = this.CurrentHealth;
            if (this.HitTimeout < 0.0f)
            {
                this.HitHealth = 0.0f;
            }

            if (this.flashCooldown < 0.0f)
            {
                this.flashCooldown = 0.3f;
            }

            this.HitHealth += damage;
            this.HitTimeout = 0.3f;
            this.CurrentHealth = Mathf.Max(this.CurrentHealth - damage, 0.0f);

            ProjectileManager.Instance.TriggerTargetDamaged(this, damage, source);

            if (this.Flags.HasFlag(TargetFlags.Invulnerable))
            {
                this.CurrentHealth = this.MaxHealth;
            }

            if (prevHealth > 0.0f && this.CurrentHealth <= 0.0f)
            {
                this.DestroyTarget(triggerExplode: true, immediate: false);
                return true;
            }

            return false;
        }

        public void DestroyTarget(bool triggerExplode, bool immediate)
        {
            if (triggerExplode && this.ShipExplodePrefab != null)
            {
                var spawned = GameObjectPools.Instance.Spawn(this.ShipExplodePrefab);
                spawned.transform.position = this.transform.position;
                if (spawned.TryGetComponent<TargetExplosion>(out var explosion))
                {
                    explosion.Target = this;
                }
            }

            ProjectileManager.Instance.TriggerTargetDestroyed(this);
            if (this.DestroyDelay > 0.0f && !immediate)
            {
                StartCoroutine(this.DoDestroy());
            }
            else if (this.DestroyDelay > -0.01f)
            {
                this.gameObject.SetActive(false);
            }
        }

        public void SetHealthBar(Renderer? renderer)
        {
            if (renderer != null)
            {
                renderer.material.SetFloat("_HealthPercent", this.CurrentHealth / this.MaxHealth);
                renderer.material.SetFloat("_HitPercent", this.HitHealth / this.MaxHealth);
            }
        }

        private IEnumerator DoDestroy()
        {
            yield return new WaitForSeconds(this.DestroyDelay);
            this.gameObject.SetActive(false);
        }

        public static void ResetCounter()
        {
            Counter = 0;
        }
        #endregion
    }
}