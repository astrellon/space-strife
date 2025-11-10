using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LysitheaVM;
using UnityEngine;
using UnityEngine.AI;

#nullable enable

namespace Orbits
{

    public class AlienShip : MonoBehaviour, IReset, ILevelStart, IObjectValue
    {
        public enum MoveStateType
        {
            Idle, MoveTowardsTarget, NavMeshTowardsTarget, FollowTarget
        }

        #region Fields
        public static readonly IReadOnlyList<string> Keys = new []
        {
            "visible", "moveTo", "moveThroughPortal", "numberOfBits", "portableTarget", "respawnBits", "show", "isAlive"
        };

        public static AlienShip? Instance;

        public int NumBits = 6;
        public float BitAngle = 0.0f;
        public float BitAngleRotate = 10.0f;
        public float BitDistance = 5.0f;
        public float LinearForce = 100.0f;
        public float NavMeshUpdateTimeout = 1.0f;
        public BitShipController BitPrefab;
        public Rigidbody Rigidbody;
        public PortalableTarget PortalableTarget;
        public AnimatePlanet AnimatePlanet;

        public Target Target;
        public TrailRenderer? ShipTrail;

        public List<BitShipController> Bits = new();

        public int BitFireIndex = 0;
        private float bitFireCooldown;

        public bool HasMovedThroughPortal = false;

        public Transform? Follow;
        public float FollowDistance;
        public float FollowAngle;
        public IWorldTarget MoveTarget;
        public float MovePastTargetAmount;
        public MoveStateType MoveState;

        public IReadOnlyList<string> ObjectKeys => Keys;
        public string TypeName => "alienShip";

        private NavMeshPath? navMeshPath;
        private float navMeshUpdateCounter = 0.0f;
        private int navPathIndex = 0;

        [Range(0.0f, 1.0f)]
        public float Emotion;
        public List<Renderer> EmotionRenderers = new();
        public bool EmotionAsDamage;

        public Light Light;
        [GradientUsage(hdr: true)]
        public Gradient LightColours;

        public List<Renderer> EmissionRenderers = new();
        [GradientUsage(hdr: true)]
        public Gradient EmissionColours;
        public ParticleSystem Trail;

        public bool EnableSpawnFreeFlying = false;
        private List<AlienShipSpawnInstance> spawnInstances = new();

        #endregion

        #region Unity Methods
        void Start()
        {
            PortalManager.Instance.OnMovedThroughPortal += this.OnMovedThroughPortal;
            this.navMeshPath = new();
        }

        void OnEnable()
        {
            Instance = this;
            ProjectileManager.Instance.RegisterTargetDestroyedHandler(this.Target, this.OnSelfDestroyed);
        }

        void OnDisable()
        {
            Instance = null;
            ProjectileManager.Instance.DeregisterTargetDestroyedHandler(this.Target, this.OnSelfDestroyed);
        }

        void OnDestroy()
        {
            PortalManager.Instance.OnMovedThroughPortal -= this.OnMovedThroughPortal;
        }

        void Update()
        {
            if (!GameManager.Instance.GameActive)
            {
                return;
            }

            this.BitAngle += this.BitAngleRotate * Time.deltaTime;

            Vector3? fireAt = null;
            if (GameManager.Instance.TryGetPlayerShip(out var playerShip))
            {
                fireAt = playerShip.transform.position;
            }

            if (this.EmotionAsDamage)
            {
                this.Emotion = 1.0f - this.Target.HealthPercent;
            }

            foreach (var emotionRenderer in this.EmotionRenderers)
            {
                emotionRenderer.material.SetFloat("_Emotion", this.Emotion);
            }

            var emission = this.EmissionColours.Evaluate(this.Emotion);
            foreach (var emissionRenderer in this.EmissionRenderers)
            {
                emissionRenderer.material.SetColor("_Emission", emission);
            }

            var main = this.Trail.main;
            main.startColor = emission;

            var lightColour = this.LightColours.Evaluate(this.Emotion);
            this.Light.color = lightColour;

            foreach (var bit in this.Bits)
            {
                bit.Ship.HasFireAtPoint = fireAt.HasValue;
                if (fireAt.HasValue)
                {
                    bit.Ship.FireAt = fireAt.Value;
                }

                bit.UpdateAngle(this.BitAngle);
                bit.UpdateBit(Time.deltaTime);
            }
        }

        void FixedUpdate()
        {
            if (!this.Target.IsAlive)
            {
                this.Rigidbody.drag = 4.0f;
                return;
            }

            if (this.MoveState == MoveStateType.MoveTowardsTarget)
            {
                this.DoMoveTo(this.MoveTarget.WorldPosition);
            }
            else if (this.MoveState == MoveStateType.NavMeshTowardsTarget)
            {
                this.DoNavMeshMove(updateMoveState: true);
            }
            else if (this.MoveState == MoveStateType.FollowTarget)
            {
                this.DoFollowTarget();
            }

            this.EnableSpawnFreeFlying = this.MoveState == MoveStateType.FollowTarget;

            if (this.EnableSpawnFreeFlying)
            {
                foreach (var inst in this.spawnInstances)
                {
                    inst.Update();
                }
            }

            this.bitFireCooldown -= Time.deltaTime;
            if (this.bitFireCooldown > 0)
            {
                return;
            }

            var incrementBitFireIndex = false;
            for (var i = 0; i < this.Bits.Count; i++)
            {
                var bit = this.Bits[i];
                var currentFireIndex = this.ShouldBitIndexFire(i);
                bit.Ship.InputFire = currentFireIndex;

                if (bit.Ship != null && bit.Ship.CurrentWeapon != null && bit.Ship.CurrentWeapon.DidJustFire == true)
                {
                    incrementBitFireIndex = true;
                }
            }

            if (incrementBitFireIndex)
            {
                this.BitFireIndex = (this.BitFireIndex + 1) % this.Bits.Count;
                this.bitFireCooldown = this.Bits[0].Ship.CurrentWeapon.MaxCooldown;
            }

        }

        void OnDrawGizmos()
        {
            if (this.MoveState == MoveStateType.MoveTowardsTarget)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(this.transform.position, this.MoveTarget.WorldPosition);
            }
            else if (this.MoveState == MoveStateType.NavMeshTowardsTarget && this.navMeshPath != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(this.MoveTarget.WorldPosition, 1.0f);

                Gizmos.color = Color.yellow;
                var fromPos = this.transform.position;
                foreach (var pos in this.navMeshPath.corners)
                {
                    Gizmos.DrawLine(fromPos, pos);
                    fromPos = pos;
                }
            }

        }
        #endregion

        #region Methods
        public bool ShouldBitIndexFire(int index)
        {
            return this.BitFireIndex == index;
        }

        public void SetSpawnInfo(IReadOnlyList<AlienShipSpawnInfo> spawnInfo)
        {
            foreach (var inst in this.spawnInstances)
            {
                inst.ClearAll();
            }

            this.spawnInstances = spawnInfo.Select(info => new AlienShipSpawnInstance(info, this)).ToList();
        }

        public void Reset()
        {
            if (this.ShipTrail != null)
            {
                this.ShipTrail.Clear();
            }

            foreach (var bit in this.Bits.ToList())
            {
                Destroy(bit.gameObject);
            }
            this.Bits.Clear();
            this.BitAngle = 0.0f;

            this.MoveTarget = WorldStaticTarget.Zero;
            this.MovePastTargetAmount = 0.0f;
            this.MoveState = MoveStateType.Idle;
            if (this.navMeshPath != null)
            {
                this.navMeshPath.ClearCorners();
            }

            foreach (var inst in this.spawnInstances)
            {
                inst.ClearAll();
            }
            this.spawnInstances.Clear();

            this.AnimatePlanet.SetShow(AnimatePlanet.ShowType.Hide, 1.0f);
        }

        public void LevelStart(LevelContainer levelContainer)
        {
            this.SpawnBits(this.NumBits);
        }

        public void SpawnBits(int numBits)
        {
            var angle = 2.0f * Mathf.PI / (float)numBits;
            for (var i = 0; i < numBits; i++)
            {
                var bit = this.SpawnBit();
                ProjectileManager.Instance.RegisterTargetDestroyedHandler(bit.Target, this.OnBitDestroyed);
                bit.name = $"{this.name} Bit {i}";
                bit.Init(this.transform, angle * i, this.BitDistance);
                this.Bits.Add(bit);
            }
        }

        private BitShipController SpawnBit()
        {
            return Instantiate(this.BitPrefab);
        }

        private void DoMoveTo(Vector3 target)
        {
            var toTarget = target - this.transform.position;
            var distance = toTarget.magnitude;

            if (distance < 0.5f)
            {
                this.MoveState = MoveStateType.Idle;
                return;
            }

            var toTargetNorm = toTarget.normalized;
            this.Rigidbody.AddForce(toTargetNorm * this.LinearForce);
        }

        private void DoNavMeshMove(bool updateMoveState)
        {
            if (this.navMeshPath == null)
            {
                Debug.LogWarning($"Attempting to do nav mesh path when it's null");
                return;
            }

            var pos = this.transform.position;

            var toTarget = this.MoveTarget.WorldPosition - pos;
            var distance = toTarget.magnitude;

            if (updateMoveState && distance < 10 && this.MovePastTargetAmount > 0.0f)
            {
                this.MoveState = MoveStateType.MoveTowardsTarget;
            }

            if (updateMoveState && distance < 0.5f)
            {
                this.MoveState = MoveStateType.Idle;
                return;
            }

            this.navMeshUpdateCounter -= Time.deltaTime;
            if (this.navMeshUpdateCounter < 0.0f)
            {
                this.UpdateNavMeshPath(this.MoveTarget.WorldPosition, updateMoveState);
                if (this.MoveState == MoveStateType.Idle)
                {
                    return;
                }
            }

            var toNextPoint = toTarget;
            if (this.navPathIndex < this.navMeshPath.corners.Length)
            {
                var nextPoint = this.navMeshPath.corners[this.navPathIndex];
                toNextPoint = nextPoint - pos;
                var toNextPointLength = toNextPoint.magnitude;
                if (toNextPointLength < 2.0f)
                {
                    if (this.navPathIndex + 1 < this.navMeshPath.corners.Length && this.navMeshPath.corners.Length > 0)
                    {
                        nextPoint = this.navMeshPath.corners[++this.navPathIndex];
                        toNextPoint = nextPoint - pos;
                    }
                    else
                    {
                        toNextPoint = toTarget;
                    }
                }
            }

            this.Rigidbody.AddForce(toNextPoint.normalized * this.LinearForce);
        }

        private void DoFollowTarget()
        {
            if (this.Follow == null)
            {
                return;
            }

            this.FollowAngle += Time.deltaTime * (360.0f / 30.0f);
            this.FollowDistance = Mathf.Sin(this.FollowAngle + 30.0f) * 6.0f + 20.0f;

            var followPos = this.Follow.position;
            var offsetX = Mathf.Sin(this.FollowAngle);
            var offsetZ = Mathf.Cos(this.FollowAngle);

            var target = followPos + new Vector3(offsetX * this.FollowDistance, 0, offsetZ * this.FollowDistance);
            this.MoveTarget = new WorldStaticTarget(target);

            this.DoNavMeshMove(updateMoveState: false);
        }

        private void OnBitDestroyed(Target target, WaveShipController? shipController)
        {
            var bitShipController = target.GetComponent<BitShipController>();
            this.Bits.Remove(bitShipController);
        }

        private void OnSelfDestroyed(Target target, WaveShipController? shipController)
        {
            var bitList = this.Bits.ToList();
            foreach (var bit in bitList)
            {
                bit.Target.DestroyTarget(true, true);
            }

            foreach (var inst in this.spawnInstances)
            {
                inst.DestroyAll();
            }
            this.spawnInstances.Clear();
        }

        private void UpdateNavMeshPath(Vector3 target, bool updateMoveState)
        {
            if (this.navMeshPath == null)
            {
                Debug.LogWarning("Attempting to update nav mesh path when it's null");
                return;
            }

            this.navMeshUpdateCounter = this.NavMeshUpdateTimeout;
            NavMesh.CalculatePath(this.transform.position, target, NavMesh.AllAreas, this.navMeshPath);

            this.navPathIndex = 0;
            if (updateMoveState && this.navMeshPath.corners.Length < 1)
            {
                this.MoveState = MoveStateType.Idle;
            }
        }

        /*
        private void OnPortalChange(PortalManager manager, Portal? oldPortal1, Portal? oldPortal2)
        {
            if (!this.HasMovedThroughPortal && manager.Portal1 != null)
            {
                this.HasMovedThroughPortal = true;
                var portalPos = manager.Portal1.transform.position;
                var toPortal = (portalPos - this.transform.position).normalized;
                this.MoveTarget = portalPos + toPortal * 15.0f;
                this.MoveToTarget = true;
            }
        }
        */

        private void OnMovedThroughPortal(PortalableTarget target, Portal atPortal, Vector3 moveDiff)
        {
            if (target != this.PortalableTarget)
            {
                return;
            }

            var velocity = this.Rigidbody.velocity;
            if (velocity.magnitude > 0.1f)
            {
                var velocityNorm = velocity.normalized;
                this.MoveTarget = this.MoveTarget.WithOffset(moveDiff + velocityNorm * 15.0f);
                this.MoveState = MoveStateType.NavMeshTowardsTarget;
                this.MovePastTargetAmount = 0.0f;

                this.UpdateNavMeshPath(this.MoveTarget.WorldPosition, updateMoveState: false);
            }
        }

        public bool TryGetKey(string key, [NotNullWhen(true)] out IValue? value)
        {
            switch (key)
            {
            case "visible":
                {
                    value = new BoolValue(this.AnimatePlanet.Show != AnimatePlanet.ShowType.Hide);
                    return true;
                }
            case "moveTo":
                {
                    value = new BuiltinFunctionValue(this.MoveToFunc, "moveTo", false);
                    return true;
                }
            case "moveThroughPortal":
                {
                    value = new BuiltinFunctionValue(this.MoveThroughPortalFunc, "moveThroughPortal", false);
                    return true;
                }
            case "follow":
                {
                    value = new BuiltinFunctionValue(this.FollowFunc, "follow", false);
                    return true;
                }
            case "numberOfBits":
                {
                    value = new NumberValue(this.Bits.Count);
                    return true;
                }
            case "portableTarget":
                {
                    value = this.PortalableTarget;
                    return true;
                }
            case "respawnBits":
                {
                    value = new BuiltinFunctionValue(this.RespawnBitsFunc, "respawnBits", false);
                    return true;
                }
            case "show":
                {
                    value = new BuiltinFunctionValue(this.ShowFunc, "show", false);
                    return true;
                }
            case "isAlive":
                {
                    value = new BoolValue(this.Target.IsAlive);
                    return true;
                }
            // case "isSpawningFreeFlying":
            //     {
            //         value = new BoolValue(this.EnableSpawnFreeFlying);
            //         return true;
            //     }
            // case "setSpawningFreeFlying":
            //     {
            //         value = new BuiltinFunctionValue(this.SetSpawningFreeFlyingFunc, "setSpawningFreeFlying", false);
            //         return true;
            //     }
            }

            value = null;
            return false;
        }

        // private void SetSpawningFreeFlyingFunc(VirtualMachine vm, ArgumentsValue args)
        // {
        //     var enable = args.GetIndexBoolean(0);
        //     this.EnableSpawnFreeFlying = enable;
        // }

        private void ShowFunc(VirtualMachine vm, ArgumentsValue args)
        {
            if (GameManager.Instance.TryGetPlayerShip(out var playerShip))
            {
                var forward = playerShip.transform.right * 70.0f;
                var pos = playerShip.transform.position + forward;
                Debug.DrawLine(pos, pos + playerShip.transform.right, Color.red, 10.0f);
                if (NavMesh.SamplePosition(pos, out var hit, 100.0f, -1))
                {
                    this.transform.position = hit.position;
                }
            }

            this.AnimatePlanet.SetShow(AnimatePlanet.ShowType.Regular, 1.0f);
        }

        private void RespawnBitsFunc(VirtualMachine vm, ArgumentsValue args)
        {
            var numBits = args.GetIndexInt(0);
            this.SpawnBits(numBits);
        }

        private void MoveToFunc(VirtualMachine vm, ArgumentsValue args)
        {
            var x = args.GetIndexFloat(0);
            var z = args.GetIndexFloat(1);

            this.MoveTarget = new WorldStaticTarget(new Vector3(x, 0, z));
            this.MoveState = MoveStateType.NavMeshTowardsTarget;
            this.MovePastTargetAmount = 0.0f;
        }

        private void MoveThroughPortalFunc(VirtualMachine vm, ArgumentsValue args)
        {
            var portalName = args.GetIndexString(0);

            if (!PortalManager.Instance.TryGetPortalPosition(portalName, out var portalPosition))
            {
                Debug.LogWarning($"Unable to find portal: {portalName}");
                return;
            }

            // var toPortal = (portalPosition - this.transform.position).normalized;
            // this.MoveTarget = portalPosition + toPortal * amount;
            this.MoveTarget = new WorldTransformTarget(portalPosition);
            this.MovePastTargetAmount = 15.0f;
            this.MoveState = MoveStateType.NavMeshTowardsTarget;
        }

        private void FollowFunc(VirtualMachine vm, ArgumentsValue args)
        {
            if (args.Length == 0)
            {
                this.Follow = null;
                this.MoveState = MoveStateType.Idle;
                return;
            }

            var target = args.GetIndex(0) as MonoBehaviour;
            if (target != null)
            {
                this.Follow = target.transform;
                this.MoveState = MoveStateType.FollowTarget;
            }
            else
            {
                Debug.LogWarning($"Unable to follow target value: {target}");
            }
        }

        public string ToStringSerialise()
        {
            throw new NotImplementedException();
        }

        public int CompareTo(IValue other)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}