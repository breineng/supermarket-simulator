using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace SampleProject.Editor.RoadGenerator
{
    public class RoadGeneratorWindow : EditorWindow
    {
        [MenuItem("Tools/Road Generator")]
        public static void ShowWindow()
        {
            GetWindow<RoadGeneratorWindow>("Road Generator");
        }

        [Header("Grid Settings")]
        public int gridWidth = 3;
        public int gridHeight = 3;
        public float blockSize = 50f;

        [Header("Road Settings")]
        public float roadWidth = 8f;
        public float roadHeight = 0.1f;
        public float intersectionSize = 12f;

        [Header("Curb Settings")]
        public bool generateCurbs = true;
        public float curbWidth = 0.5f;
        public float curbHeight = 0.2f;

        [Header("Street Furniture")]
        public bool generatePoles = true;
        public float poleHeight = 4f;
        public float poleSpacing = 15f;
        public bool generateTrafficLights = true;

        [Header("Materials")]
        public Material roadMaterial;
        public Material curbMaterial;
        public Material poleMaterial;

        [Header("Output Settings")]
        public string outputPath = "Assets/Generated/Roads/";
        public string meshName = "GeneratedRoad";

        [Header("Presets")]
        public RoadGeneratorPreset currentPreset;

        private RoadMeshGenerator meshGenerator;
        private Vector2 scrollPosition;

        private void OnEnable()
        {
            meshGenerator = new RoadMeshGenerator();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField("Road Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawGridSettings();
            EditorGUILayout.Space();

            DrawRoadSettings();
            EditorGUILayout.Space();

            DrawCurbSettings();
            EditorGUILayout.Space();

            DrawStreetFurnitureSettings();
            EditorGUILayout.Space();

            DrawMaterialSettings();
            EditorGUILayout.Space();

            DrawOutputSettings();
            EditorGUILayout.Space();

            DrawPresetSettings();
            EditorGUILayout.Space();

            DrawGenerateButton();

            EditorGUILayout.EndScrollView();
        }

        private void DrawGridSettings()
        {
            EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);
            gridWidth = EditorGUILayout.IntSlider("Grid Width", gridWidth, 1, 10);
            gridHeight = EditorGUILayout.IntSlider("Grid Height", gridHeight, 1, 10);
            blockSize = EditorGUILayout.Slider("Block Size", blockSize, 20f, 100f);
        }

        private void DrawRoadSettings()
        {
            EditorGUILayout.LabelField("Road Settings", EditorStyles.boldLabel);
            roadWidth = EditorGUILayout.Slider("Road Width", roadWidth, 4f, 20f);
            roadHeight = EditorGUILayout.Slider("Road Height", roadHeight, 0.05f, 0.5f);
            intersectionSize = EditorGUILayout.Slider("Intersection Size", intersectionSize, 8f, 30f);
        }

        private void DrawCurbSettings()
        {
            EditorGUILayout.LabelField("Curb Settings", EditorStyles.boldLabel);
            generateCurbs = EditorGUILayout.Toggle("Generate Curbs", generateCurbs);
            if (generateCurbs)
            {
                EditorGUI.indentLevel++;
                curbWidth = EditorGUILayout.Slider("Curb Width", curbWidth, 0.2f, 2f);
                curbHeight = EditorGUILayout.Slider("Curb Height", curbHeight, 0.1f, 0.5f);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawStreetFurnitureSettings()
        {
            EditorGUILayout.LabelField("Street Furniture", EditorStyles.boldLabel);
            generatePoles = EditorGUILayout.Toggle("Generate Poles", generatePoles);
            if (generatePoles)
            {
                EditorGUI.indentLevel++;
                poleHeight = EditorGUILayout.Slider("Pole Height", poleHeight, 2f, 8f);
                poleSpacing = EditorGUILayout.Slider("Pole Spacing", poleSpacing, 5f, 30f);
                EditorGUI.indentLevel--;
            }

            generateTrafficLights = EditorGUILayout.Toggle("Generate Traffic Lights", generateTrafficLights);
        }

        private void DrawMaterialSettings()
        {
            EditorGUILayout.LabelField("Materials", EditorStyles.boldLabel);
            roadMaterial = (Material)EditorGUILayout.ObjectField("Road Material", roadMaterial, typeof(Material), false);
            if (generateCurbs)
            {
                curbMaterial = (Material)EditorGUILayout.ObjectField("Curb Material", curbMaterial, typeof(Material), false);
            }
            if (generatePoles)
            {
                poleMaterial = (Material)EditorGUILayout.ObjectField("Pole Material", poleMaterial, typeof(Material), false);
            }
        }

        private void DrawOutputSettings()
        {
            EditorGUILayout.LabelField("Output Settings", EditorStyles.boldLabel);
            outputPath = EditorGUILayout.TextField("Output Path", outputPath);
            meshName = EditorGUILayout.TextField("Mesh Name", meshName);
        }

        private void DrawPresetSettings()
        {
            EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            currentPreset = (RoadGeneratorPreset)EditorGUILayout.ObjectField("Current Preset", currentPreset, typeof(RoadGeneratorPreset), false);
            
            if (GUILayout.Button("Load", GUILayout.Width(60)))
            {
                LoadFromPreset();
            }
            
            if (GUILayout.Button("Save", GUILayout.Width(60)))
            {
                SaveToPreset();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save As New Preset"))
            {
                CreateNewPreset();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawGenerateButton()
        {
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Generate Road Network", GUILayout.Height(40)))
            {
                GenerateRoadNetwork();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Clear Generated Roads", GUILayout.Height(30)))
            {
                ClearGeneratedRoads();
            }
        }

        private void GenerateRoadNetwork()
        {
            if (!ValidateSettings())
                return;

            CreateOutputDirectory();

            var settings = new RoadGenerationSettings
            {
                GridWidth = gridWidth,
                GridHeight = gridHeight,
                BlockSize = blockSize,
                RoadWidth = roadWidth,
                RoadHeight = roadHeight,
                IntersectionSize = intersectionSize,
                GenerateCurbs = generateCurbs,
                CurbWidth = curbWidth,
                CurbHeight = curbHeight,
                GeneratePoles = generatePoles,
                PoleHeight = poleHeight,
                PoleSpacing = poleSpacing,
                GenerateTrafficLights = generateTrafficLights,
                RoadMaterial = roadMaterial,
                CurbMaterial = curbMaterial,
                PoleMaterial = poleMaterial,
                OutputPath = outputPath,
                MeshName = meshName
            };

            var roadNetwork = meshGenerator.GenerateRoadNetwork(settings);
            CreateRoadGameObjects(roadNetwork, settings);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Road network generated successfully with {roadNetwork.Roads.Count} road segments and {roadNetwork.Intersections.Count} intersections");
        }

        private bool ValidateSettings()
        {
            if (string.IsNullOrEmpty(outputPath))
            {
                EditorUtility.DisplayDialog("Error", "Output path cannot be empty", "OK");
                return false;
            }

            if (string.IsNullOrEmpty(meshName))
            {
                EditorUtility.DisplayDialog("Error", "Mesh name cannot be empty", "OK");
                return false;
            }

            return true;
        }

        private void CreateOutputDirectory()
        {
            if (!System.IO.Directory.Exists(outputPath))
            {
                System.IO.Directory.CreateDirectory(outputPath);
            }
        }

        private void CreateRoadGameObjects(RoadNetwork network, RoadGenerationSettings settings)
        {
            // Create parent object
            GameObject roadParent = new GameObject($"RoadNetwork_{settings.MeshName}");
            
            // Generate roads
            for (int i = 0; i < network.Roads.Count; i++)
            {
                var road = network.Roads[i];
                CreateRoadSegmentGameObject(road, roadParent, settings, i);
            }

            // Generate intersections
            for (int i = 0; i < network.Intersections.Count; i++)
            {
                var intersection = network.Intersections[i];
                CreateIntersectionGameObject(intersection, roadParent, settings, i);
            }

            // Generate curbs if enabled
            if (settings.GenerateCurbs)
            {
                for (int i = 0; i < network.Curbs.Count; i++)
                {
                    var curb = network.Curbs[i];
                    CreateCurbGameObject(curb, roadParent, settings, i);
                }
            }

            // Generate poles if enabled
            if (settings.GeneratePoles)
            {
                for (int i = 0; i < network.Poles.Count; i++)
                {
                    var pole = network.Poles[i];
                    CreatePoleGameObject(pole, roadParent, settings, i);
                }
            }

            // Generate traffic lights if enabled
            if (settings.GenerateTrafficLights)
            {
                for (int i = 0; i < network.TrafficLights.Count; i++)
                {
                    var trafficLight = network.TrafficLights[i];
                    CreateTrafficLightGameObject(trafficLight, roadParent, settings, i);
                }
            }

            Selection.activeGameObject = roadParent;
        }

        private void CreateRoadSegmentGameObject(RoadSegment road, GameObject parent, RoadGenerationSettings settings, int index)
        {
            GameObject roadGO = new GameObject($"Road_{index}");
            roadGO.transform.SetParent(parent.transform);

            var meshFilter = roadGO.AddComponent<MeshFilter>();
            var meshRenderer = roadGO.AddComponent<MeshRenderer>();

            meshFilter.mesh = road.Mesh;
            meshRenderer.material = settings.RoadMaterial;

            // Save mesh asset
            string meshPath = $"{settings.OutputPath}Road_{settings.MeshName}_{index}.asset";
            AssetDatabase.CreateAsset(road.Mesh, meshPath);
        }

        private void CreateIntersectionGameObject(RoadIntersection intersection, GameObject parent, RoadGenerationSettings settings, int index)
        {
            GameObject intersectionGO = new GameObject($"Intersection_{index}");
            intersectionGO.transform.SetParent(parent.transform);

            var meshFilter = intersectionGO.AddComponent<MeshFilter>();
            var meshRenderer = intersectionGO.AddComponent<MeshRenderer>();

            meshFilter.mesh = intersection.Mesh;
            meshRenderer.material = settings.RoadMaterial;

            // Save mesh asset
            string meshPath = $"{settings.OutputPath}Intersection_{settings.MeshName}_{index}.asset";
            AssetDatabase.CreateAsset(intersection.Mesh, meshPath);
        }

        private void CreateCurbGameObject(CurbSegment curb, GameObject parent, RoadGenerationSettings settings, int index)
        {
            GameObject curbGO = new GameObject($"Curb_{index}");
            curbGO.transform.SetParent(parent.transform);

            var meshFilter = curbGO.AddComponent<MeshFilter>();
            var meshRenderer = curbGO.AddComponent<MeshRenderer>();

            meshFilter.mesh = curb.Mesh;
            meshRenderer.material = settings.CurbMaterial;

            // Save mesh asset
            string meshPath = $"{settings.OutputPath}Curb_{settings.MeshName}_{index}.asset";
            AssetDatabase.CreateAsset(curb.Mesh, meshPath);
        }

        private void CreatePoleGameObject(PoleObject pole, GameObject parent, RoadGenerationSettings settings, int index)
        {
            GameObject poleGO = new GameObject($"Pole_{index}");
            poleGO.transform.SetParent(parent.transform);
            poleGO.transform.position = pole.Position;

            var meshFilter = poleGO.AddComponent<MeshFilter>();
            var meshRenderer = poleGO.AddComponent<MeshRenderer>();

            meshFilter.mesh = pole.Mesh;
            meshRenderer.material = settings.PoleMaterial;

            // Save mesh asset
            string meshPath = $"{settings.OutputPath}Pole_{settings.MeshName}_{index}.asset";
            AssetDatabase.CreateAsset(pole.Mesh, meshPath);
        }

        private void CreateTrafficLightGameObject(TrafficLightObject trafficLight, GameObject parent, RoadGenerationSettings settings, int index)
        {
            GameObject trafficLightGO = new GameObject($"TrafficLight_{index}");
            trafficLightGO.transform.SetParent(parent.transform);
            trafficLightGO.transform.position = trafficLight.Position;
            trafficLightGO.transform.rotation = trafficLight.Rotation;

            var meshFilter = trafficLightGO.AddComponent<MeshFilter>();
            var meshRenderer = trafficLightGO.AddComponent<MeshRenderer>();

            meshFilter.mesh = trafficLight.Mesh;
            meshRenderer.material = settings.PoleMaterial; // Use pole material for now

            // Save mesh asset
            string meshPath = $"{settings.OutputPath}TrafficLight_{settings.MeshName}_{index}.asset";
            AssetDatabase.CreateAsset(trafficLight.Mesh, meshPath);
        }

        private void ClearGeneratedRoads()
        {
            var roadNetworks = FindObjectsOfType<GameObject>();
            for (int i = roadNetworks.Length - 1; i >= 0; i--)
            {
                if (roadNetworks[i].name.StartsWith("RoadNetwork_"))
                {
                    DestroyImmediate(roadNetworks[i]);
                }
            }

            Debug.Log("Generated roads cleared");
        }

        private void LoadFromPreset()
        {
            if (currentPreset == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a preset to load", "OK");
                return;
            }

            var settings = currentPreset.ToSettings();
            
            gridWidth = settings.GridWidth;
            gridHeight = settings.GridHeight;
            blockSize = settings.BlockSize;
            roadWidth = settings.RoadWidth;
            roadHeight = settings.RoadHeight;
            intersectionSize = settings.IntersectionSize;
            generateCurbs = settings.GenerateCurbs;
            curbWidth = settings.CurbWidth;
            curbHeight = settings.CurbHeight;
            generatePoles = settings.GeneratePoles;
            poleHeight = settings.PoleHeight;
            poleSpacing = settings.PoleSpacing;
            generateTrafficLights = settings.GenerateTrafficLights;
            roadMaterial = settings.RoadMaterial;
            curbMaterial = settings.CurbMaterial;
            poleMaterial = settings.PoleMaterial;
            outputPath = settings.OutputPath;
            meshName = settings.MeshName;

            Debug.Log($"Loaded preset: {currentPreset.name}");
        }

        private void SaveToPreset()
        {
            if (currentPreset == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a preset to save to", "OK");
                return;
            }

            var settings = GetCurrentSettings();
            currentPreset.FromSettings(settings);
            
            EditorUtility.SetDirty(currentPreset);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"Saved settings to preset: {currentPreset.name}");
        }

        private void CreateNewPreset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Road Generator Preset",
                "RoadGeneratorPreset",
                "asset",
                "Please enter a name for the new preset"
            );

            if (!string.IsNullOrEmpty(path))
            {
                var newPreset = CreateInstance<RoadGeneratorPreset>();
                var settings = GetCurrentSettings();
                newPreset.FromSettings(settings);
                
                AssetDatabase.CreateAsset(newPreset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                currentPreset = newPreset;
                
                Debug.Log($"Created new preset: {path}");
            }
        }

        private RoadGenerationSettings GetCurrentSettings()
        {
            return new RoadGenerationSettings
            {
                GridWidth = gridWidth,
                GridHeight = gridHeight,
                BlockSize = blockSize,
                RoadWidth = roadWidth,
                RoadHeight = roadHeight,
                IntersectionSize = intersectionSize,
                GenerateCurbs = generateCurbs,
                CurbWidth = curbWidth,
                CurbHeight = curbHeight,
                GeneratePoles = generatePoles,
                PoleHeight = poleHeight,
                PoleSpacing = poleSpacing,
                GenerateTrafficLights = generateTrafficLights,
                RoadMaterial = roadMaterial,
                CurbMaterial = curbMaterial,
                PoleMaterial = poleMaterial,
                OutputPath = outputPath,
                MeshName = meshName
            };
        }
    }
} 