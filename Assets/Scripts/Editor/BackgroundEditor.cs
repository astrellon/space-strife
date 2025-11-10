using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

#nullable enable

namespace Orbits
{
    public class BackgroundEditor : EditorWindow
    {
        #region Fields
        private IntegerField? numColumns;
        private IntegerField? numRows;
        private List<ObjectField> prefabs = new(4);
        private Vector3Field? rotationField;
        private Vector3Field? spacingField;
        private Vector3Field? offsetField;
        private FloatField? scaleField;
        private Button? createButton;
        #endregion

        #region Constructor
        public BackgroundEditor()
        {

        }
        #endregion

        #region Methods
        public void CreateGUI()
        {
            this.rootVisualElement.Add(new Label("Create a set of background quads"));

            this.numColumns = new IntegerField("Columns", 64) { value = 4 };
            this.rootVisualElement.Add(this.numColumns);

            this.numRows = new IntegerField("Rows", 64) { value = 3 };
            this.rootVisualElement.Add(this.numRows);

            for (var i = 0; i < this.prefabs.Capacity; i++)
            {
                var field = new ObjectField($"Prefab: {i + 1}") { objectType = typeof(GameObject) };
                this.prefabs.Add(field);
                this.rootVisualElement.Add(field);
            }

            this.rotationField = new Vector3Field("Rotation Euler") { value = new Vector3(90, 0, 0) };
            this.rootVisualElement.Add(this.rotationField);

            this.spacingField = new Vector3Field("Spacing") { value = new Vector3(15, 0, 15) };
            this.rootVisualElement.Add(this.spacingField);

            this.offsetField = new Vector3Field("Offset") { value = new Vector3(0, -350, 0) };
            this.rootVisualElement.Add(this.offsetField);

            this.scaleField = new FloatField("Scale") { value = 5.0f };
            this.rootVisualElement.Add(this.scaleField);

            this.createButton = new Button(this.OnCreate) { text = "Create" };
            this.rootVisualElement.Add(this.createButton);
        }

        private void OnCreate()
        {
            if (this.numColumns == null ||
                this.numRows == null ||
                this.spacingField == null ||
                this.rotationField == null ||
                this.scaleField == null ||
                this.offsetField == null)
            {
                return;
            }

            var spacing = this.spacingField.value;
            var baseRotation = this.rotationField.value;
            var scale = Vector3.one * this.scaleField.value;
            var offset = this.offsetField.value;

            var validPrefabs = this.prefabs.Where(r => r != null)
                .Select(r => r.value as GameObject)
                .Where(r => r != null).ToList();
            if (validPrefabs.Count == 0)
            {
                Debug.Log($"No valid prefabs");
                return;
            }

            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            var target = prefabStage != null ? prefabStage.prefabContentsRoot.transform : null;
            for (var x = 0; x < this.numColumns.value; x++)
            for (var z = 0; z < this.numRows.value; z++)
            {
                var randomPick = validPrefabs[UnityEngine.Random.Range(0, validPrefabs.Count)];
                if (randomPick == null)
                {
                    Debug.LogError($"Invalid prefab, not a game object");
                    continue;
                }

                var gameObject = Instantiate(randomPick);
                if (gameObject == null)
                {
                    Debug.LogError($"Unable to instantiate prefab: {randomPick.name}");
                    continue;
                }

                gameObject.name = $"Background {x} {z}";
                if (target != null)
                {
                    gameObject.transform.SetParent(target);
                }

                var randomOffsetX = UnityEngine.Random.Range(-4, 4);
                var randomOffsetZ = UnityEngine.Random.Range(-4, 4);
                var position = new Vector3(x * spacing.x + randomOffsetX, 0, z * spacing.z + randomOffsetZ) + offset;

                var rotation = Quaternion.Euler(baseRotation.x, baseRotation.y + UnityEngine.Random.Range(0, 360), baseRotation.z);
                gameObject.transform.localScale = scale;
                gameObject.transform.SetPositionAndRotation(position, rotation);
            }
        }

        [MenuItem("Tools/Background Editor")]
        public static void ShowEditor()
        {
            var window = GetWindow<BackgroundEditor>();
            window.titleContent = new GUIContent("Background Editor");
        }
        #endregion
    }
}