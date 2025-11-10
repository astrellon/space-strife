using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

#nullable enable

namespace Orbits
{
    [Serializable]
    public class AlienShipSpawnInfo
    {
        public FreeFlyingShipController Prefab;
        public int MaxToSpawn;
        public float TimeBetweenSpawn;
        public Vector2 SpawnRadius = new(5.0f, 10.0f);
    }

    public class AlienShipSpawnInstance
    {
        public readonly AlienShipSpawnInfo Info;
        public readonly AlienShip Parent;
        public readonly List<FreeFlyingShipController> CurrentlySpawned = new();
        public float Counter = 0.0f;

        public AlienShipSpawnInstance(AlienShipSpawnInfo info, AlienShip parent)
        {
            this.Info = info;
            this.Parent = parent;
            this.Counter = info.TimeBetweenSpawn;
        }

        public void DestroyAll()
        {
            foreach (var spawned in this.CurrentlySpawned.ToList())
            {
                spawned.Target.DestroyTarget(true, true);
            }
        }

        public void ClearAll()
        {
            foreach (var spawned in this.CurrentlySpawned.ToList())
            {
                GameObject.Destroy(spawned.gameObject);
            }
        }

        public void Update()
        {
            this.Counter -= Time.deltaTime;
            if (this.Counter < 0.0f)
            {
                this.Counter += this.Info.TimeBetweenSpawn;
                this.Spawn();
            }
        }

        public void Spawn()
        {
            var angle = UnityEngine.Random.value * 2.0f * Mathf.PI;
            var distance = UnityEngine.Random.value * 5.0f + 5.0f;

            var x = Mathf.Cos(angle) * distance;
            var z = Mathf.Sin(angle) * distance;
            var vec = new Vector3(x, 0, z);

            var position = this.Parent.transform.position + vec;
            if (!NavMesh.SamplePosition(position, out var hit, 10, NavMesh.AllAreas))
            {
                Debug.LogWarning($"Unable to find safe place to spawn free flying: {position}");
                return;
            }

            var spawned = GameObjectPools.Instance.Spawn(this.Info.Prefab);
            spawned.transform.position = position;
            spawned.Init(this.Parent.Follow, this.Info.Prefab);

            ProjectileManager.Instance.RegisterTargetDestroyedHandler(spawned.Target, this.OnDestroyed);

            this.CurrentlySpawned.Add(spawned);

            return;
        }

        private void OnDestroyed(Target target, WaveShipController? shipController)
        {
            if (target.TryGetComponent<FreeFlyingShipController>(out var freeFlying))
            {
                this.CurrentlySpawned.Remove(freeFlying);
            }
        }
    }
}