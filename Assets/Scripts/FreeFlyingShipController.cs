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
    public class FreeFlyingShipController : MonoBehaviour
    {
        public enum MoveStateType
        {
            Idle, MoveTowardsTarget, NavMeshTowardsTarget, FollowTarget
        }

        #region Fields
        public float LinearForce = 100.0f;
        public float NavMeshUpdateTimeout = 1.0f;
        public Rigidbody Rigidbody;
        public Ship? Ship;
        public bool PointAtPlayer = false;

        public Target Target;

        public Transform? Follow;
        public float FollowDistance;
        public float FollowAngle;
        public IWorldTarget MoveTarget;
        public float MovePastTargetAmount;
        public MoveStateType MoveState;
        public Renderer? HealthBar;
        public FreeFlyingShipController FromPrefab;

        public GameObject SpawnEffect;

        private NavMeshPath navMeshPath;
        private float navMeshUpdateCounter = 0.0f;
        private int navPathIndex = 0;
        #endregion

        #region Unity Methods
        void Start()
        {
            this.navMeshPath = new();
        }

        void Update()
        {
            if (!GameManager.Instance.GameActive)
            {
                return;
            }

            this.Target.ManagedUpdate(Time.deltaTime);
            this.Target.SetHealthBar(this.HealthBar);

            if (this.Ship != null)
            {
                this.Ship.InputFire = true;
                if (this.PointAtPlayer)
                {
                    var fireAt = GameManager.Instance.CurrentLevelContainer?.Ship?.transform.position;
                    this.Ship.HasFireAtPoint = fireAt.HasValue;
                    if (fireAt.HasValue)
                    {
                        this.Ship.FireAt = fireAt.Value;
                    }
                }
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
        }

        void OnDrawGizmos()
        {
            if (this.MoveState == MoveStateType.MoveTowardsTarget)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(this.transform.position, this.MoveTarget.WorldPosition);
            }
            else if (this.MoveState == MoveStateType.NavMeshTowardsTarget)
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
        public void Init(Transform? follow, FreeFlyingShipController prefab)
        {
            this.FromPrefab = prefab;

            if (follow != null)
            {
                this.Follow = follow;
                this.MoveState = MoveStateType.FollowTarget;
            }

            if (this.SpawnEffect != null)
            {
                var effect = GameObjectPools.Instance.Spawn(this.SpawnEffect);
                effect.transform.position = this.transform.position;
            }
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

        private void UpdateNavMeshPath(Vector3 target, bool updateMoveState)
        {
            this.navMeshUpdateCounter = this.NavMeshUpdateTimeout;
            NavMesh.CalculatePath(this.transform.position, target, NavMesh.AllAreas, this.navMeshPath);

            this.navPathIndex = 0;
            if (updateMoveState && this.navMeshPath.corners.Length < 1)
            {
                this.MoveState = MoveStateType.Idle;
            }
        }
        #endregion
    }
}