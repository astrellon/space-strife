using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

#nullable enable

namespace Orbits
{
    public class PlayerTankController : MonoBehaviour
    {
        #region Fields
        private static readonly Plane WorldPlane = new(Vector3.up, Vector3.zero);

        public int MaxTanks = 1;
        public List<Tank?> Tanks;
        public float PlanetRadius = 5.0f;
        public GameObject Planet;
        public ITankContainer TankContainer;
        public Vector2 ZoomLevels = new(-30, 30);

        public IReadOnlyList<Tank> ValidTanks { get; private set; } = new List<Tank>();
        private bool mouseDown = false;
        private bool toggledTankFire = false;
        private bool gameOptionToggleTankFire = false;
        #endregion

        #region Unity Methods
        private void Start()
        {
            this.UpdateValidTanks();

            this.gameOptionToggleTankFire = GameOptions.ToggleTankFire;
            GameOptions.OnChange += this.OnGameOptionsChange;
        }

        private void OnDestroy()
        {
            GameOptions.OnChange -= this.OnGameOptionsChange;
        }

        private void Update()
        {
            if (!GameManager.Instance.GameActive)
            {
                return;
            }

            var scrollWheel = Input.GetAxis("Mouse ScrollWheel") * 5.0f;
            GameManager.Instance.Camera.PlayerZoomOffset = Mathf.Clamp(GameManager.Instance.Camera.PlayerZoomOffset + scrollWheel, this.ZoomLevels.x, this.ZoomLevels.y);

            this.HandleBarrelPointing();
            if (this.TankContainer.HandleMovementInUpdate == true)
            {
                this.HandleMovement();
            }

            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                this.mouseDown = true;
                if (this.gameOptionToggleTankFire)
                {
                    this.toggledTankFire = !this.toggledTankFire;
                }
            }
            else if (!Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                this.mouseDown = false;
            }

            if (GameManager.Instance.CurrentLevel != null)
            {
                var activities = GameManager.Instance.CurrentLevel.ActiveAbilities;
                if (Input.GetKeyDown(KeyCode.Alpha1) && activities.Count > 0)
                {
                    activities[0].Execute(GameManager.Instance.CurrentLevel);
                }
                if (Input.GetKeyDown(KeyCode.Alpha2) && activities.Count > 1)
                {
                    activities[1].Execute(GameManager.Instance.CurrentLevel);
                }
                if (Input.GetKeyDown(KeyCode.Alpha3) && activities.Count > 2)
                {
                    activities[2].Execute(GameManager.Instance.CurrentLevel);
                }
            }

            for (var i = 0; i < this.Tanks.Count; i++)
            {
                var tank = this.Tanks[i];
                if (tank == null)
                {
                    continue;
                }

                this.TryCalculateGlobalPosition(i, 0.0f, out var globalPosition, out var globalRotation);
                tank.transform.SetPositionAndRotation(globalPosition, globalRotation);
            }
        }

        private void FixedUpdate()
        {
            if (!GameManager.Instance.GameActive)
            {
                return;
            }

            if (this.TankContainer.HandleMovementInUpdate == false)
            {
                this.HandleMovement();
            }

            if (this.ValidTanks.Count == 0)
            {
                return;
            }

            var inputFire = false;
            if (GameManager.Instance.InputMethod == GameManager.InputMethodType.Touch)
            {
                if (GameManager.Instance.PointJoystick.Direction.magnitude > 0.01f ||
                    GameManager.Instance.CombinedJoystick.Direction.magnitude > 0.01f)
                {
                    inputFire = true;
                }
            }
            else
            {
                var pointX = Input.GetAxis("Point Horizontal");
                var pointY = Input.GetAxis("Point Vertical");
                if (pointX != 0.0f || pointY != 0.0f)
                {
                    inputFire = true;
                }
                else if (this.mouseDown)
                {
                    inputFire = true;
                }
            }

            if (this.gameOptionToggleTankFire && GameManager.Instance.InputMethod == GameManager.InputMethodType.Mouse)
            {
                inputFire = this.toggledTankFire;
            }

            if (inputFire && UIManager.Instance.InDialogue)
            {
                inputFire = false;
                this.toggledTankFire = false;
            }

            foreach (var tank in this.ValidTanks)
            {
                tank.InputFire = inputFire;
            }
        }

        private void LateUpdate()
        {
            if (GameManager.Instance.CurrentLevelContainer != null)
            {
                var container = GameManager.Instance.CurrentLevelContainer;
                var ship = container.Ship;
                if (ship != null)
                {
                    var boundary = GameManager.Instance.CurrentBoundary;
                    if (boundary != null && boundary.BoundaryMaterial != null)
                    {
                        boundary.BoundaryMaterial.SetVector("_ShowNear", ship.transform.position);
                    }
                }
            }
        }
        #endregion

        #region Methods
        public void UpdatePlanetRadius(float radius)
        {
            this.PlanetRadius = radius;
            if (this.TankContainer is TankPlanet tankPlanet)
            {
                tankPlanet.PlanetRadius = radius;
            }
        }

        public void SetTank(Tank tank, int index)
        {
            this.TankContainer.SetTank(tank, index);

            for (var i = this.Tanks.Count; i <= index; i++)
            {
                this.Tanks.Add(null);
            }

            this.Tanks[index] = tank;
            this.UpdateValidTanks();
        }

        public bool SellTank(int index, out int returnedMoney)
        {
            if (index < 0 || index >= this.Tanks.Count)
            {
                returnedMoney = 0;
                return false;
            }

            var tank = this.Tanks[index];
            if (tank == null)
            {
                returnedMoney = 0;
                return false;
            }

            var level = tank.GetWeaponLevel(tank.CurrentWeaponType);
            var cost = EquipmentManager.Instance.GetCostOfTank(tank.CurrentWeaponType, level);
            tank.DoEffect(EquipmentManager.Instance.SellEffect);
            Destroy(tank.gameObject);

            this.Tanks[index] = null;
            this.UpdateValidTanks();

            returnedMoney = cost;
            return true;
        }

        private void UpdateValidTanks()
        {
            this.ValidTanks = this.Tanks.Where(t => t != null).Cast<Tank>().ToList();
        }

        public bool TryCalculateLocalPosition(int tankIndex, float radiusOffset, out Vector3 localPosition, out Quaternion localRotation)
        {
            if (tankIndex < 0 || tankIndex >= this.MaxTanks)
            {
                localPosition = Vector3.zero;
                localRotation = Quaternion.identity;
                return false;
            }

            return this.TankContainer.TryCalculateLocalPosition(tankIndex, radiusOffset, out localPosition, out localRotation);
        }
        public bool TryCalculateLocalPosition(int tankIndex, float radiusOffset, out Vector3 localPosition)
        {
            if (tankIndex < 0 || tankIndex >= this.MaxTanks)
            {
                localPosition = Vector3.zero;
                return false;
            }

            return this.TankContainer.TryCalculateLocalPosition(tankIndex, radiusOffset, out localPosition);
        }

        public bool TryCalculateGlobalPosition(int tankIndex, float radiusOffset, out Vector3 globalPosition, out Quaternion globalRotation)
        {
            if (tankIndex < 0 || tankIndex >= this.MaxTanks)
            {
                globalPosition = Vector3.zero;
                globalRotation = Quaternion.identity;
                return false;
            }

            return this.TankContainer.TryCalculateGlobalPosition(tankIndex, radiusOffset, out globalPosition, out globalRotation);
        }
        public bool TryCalculateGlobalPosition(int tankIndex, float radiusOffset, out Vector3 globalPosition)
        {
            if (tankIndex < 0 || tankIndex >= this.MaxTanks)
            {
                globalPosition = Vector3.zero;
                return false;
            }

            return this.TankContainer.TryCalculateGlobalPosition(tankIndex, radiusOffset, out globalPosition);
        }

        private void PointTankBarrelsAt(Vector3 worldPos)
        {
            foreach (var tank in this.ValidTanks)
            {
                tank.FireAt = worldPos;
            }
        }

        private void HandleBarrelPointing()
        {
            this.TankContainer.TryCalculateGlobalPosition(0, 0, out var globalMainTankPos);
            if (GameManager.Instance.InputMethod == GameManager.InputMethodType.Touch)
            {
                var fireInput = GameManager.Instance.JoystickPointInput;
                if (fireInput.magnitude > 0.01f)
                {
                    var worldPos = globalMainTankPos + Utils.FromXZ(fireInput) * 100.0f;
                    this.PointTankBarrelsAt(worldPos);
                }
            }
            else if (GameManager.Instance.InputMethod == GameManager.InputMethodType.Controller)
            {
                var pointX = Input.GetAxis("Point Horizontal");
                var pointY = Input.GetAxis("Point Vertical");
                if (pointX != 0.0f || pointY != 0.0f)
                {
                    var worldPos = globalMainTankPos + new Vector3(pointX, 0, -pointY) * 100.0f;
                    this.PointTankBarrelsAt(worldPos);
                }
            }
            else if (GameManager.Instance.InputMethod == GameManager.InputMethodType.Mouse)
            {
                var mousePos = Input.mousePosition;
                if (Input.touchCount > 0)
                {
                    mousePos = Input.GetTouch(0).position;
                }

                var ray = Camera.main.ScreenPointToRay(mousePos);
                if (WorldPlane.Raycast(ray, out var dist))
                {
                    var worldPos = ray.GetPoint(dist);
                    this.PointTankBarrelsAt(worldPos);
                }
            }
        }

        private void HandleMovement()
        {
            this.TankContainer.TryCalculateLocalPosition(0, 0, out var localMainTankPos);
            if (GameManager.Instance.InputMethod == GameManager.InputMethodType.Touch)
            {
                var moveTo = GameManager.Instance.JoystickMoveInput;
                this.TankContainer.MoveTanksByJoystick(this, moveTo, localMainTankPos);
            }
            else if (GameManager.Instance.InputMethod == GameManager.InputMethodType.Controller)
            {
                var moveX = Input.GetAxis("Joystick Horizontal");
                var moveY = Input.GetAxis("Joystick Vertical");
                var moveTo = new Vector2(moveX, moveY);
                this.TankContainer.MoveTanksByJoystick(this, moveTo, localMainTankPos);
            }
            else if (GameManager.Instance.InputMethod == GameManager.InputMethodType.Mouse)
            {
                var moveX = -Input.GetAxis("Horizontal");
                var moveY = -Input.GetAxis("Vertical");
                this.TankContainer.MoveTanksByKeyboard(this, new Vector2(moveX, moveY));
            }
        }

        private void OnGameOptionsChange(SettingType type)
        {
            if (type == SettingType.ToggleTankFire)
            {
                this.toggledTankFire = false;
                this.gameOptionToggleTankFire = GameOptions.ToggleTankFire;
            }
        }
        #endregion
    }
}
