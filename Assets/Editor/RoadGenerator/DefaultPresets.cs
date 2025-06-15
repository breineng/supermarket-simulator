using UnityEngine;
using UnityEditor;

namespace SampleProject.Editor.RoadGenerator
{
    public static class DefaultPresets
    {
        [MenuItem("Tools/Road Generator/Create Default Presets")]
        public static void CreateDefaultPresets()
        {
            CreateCityPreset();
            CreateSuburbanPreset();
            CreateHighwayPreset();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Default road generator presets created in Assets/Editor/RoadGenerator/Presets/");
        }

        private static void CreateCityPreset()
        {
            var preset = ScriptableObject.CreateInstance<RoadGeneratorPreset>();
            
            // City settings - dense urban grid
            preset.gridWidth = 5;
            preset.gridHeight = 5;
            preset.blockSize = 40f;
            preset.roadWidth = 6f;
            preset.roadHeight = 0.1f;
            preset.intersectionSize = 10f;
            preset.generateCurbs = true;
            preset.curbWidth = 0.3f;
            preset.curbHeight = 0.15f;
            preset.generatePoles = true;
            preset.poleHeight = 3.5f;
            preset.poleSpacing = 12f;
            preset.generateTrafficLights = true;
            preset.outputPath = "Assets/Generated/Roads/City/";
            preset.meshName = "CityRoad";

            CreatePresetAsset(preset, "CityPreset");
        }

        private static void CreateSuburbanPreset()
        {
            var preset = ScriptableObject.CreateInstance<RoadGeneratorPreset>();
            
            // Suburban settings - wider blocks, less dense
            preset.gridWidth = 3;
            preset.gridHeight = 3;
            preset.blockSize = 60f;
            preset.roadWidth = 8f;
            preset.roadHeight = 0.1f;
            preset.intersectionSize = 12f;
            preset.generateCurbs = true;
            preset.curbWidth = 0.4f;
            preset.curbHeight = 0.2f;
            preset.generatePoles = true;
            preset.poleHeight = 4f;
            preset.poleSpacing = 20f;
            preset.generateTrafficLights = false; // No traffic lights in suburbs
            preset.outputPath = "Assets/Generated/Roads/Suburban/";
            preset.meshName = "SuburbanRoad";

            CreatePresetAsset(preset, "SuburbanPreset");
        }

        private static void CreateHighwayPreset()
        {
            var preset = ScriptableObject.CreateInstance<RoadGeneratorPreset>();
            
            // Highway settings - wide roads, minimal intersections
            preset.gridWidth = 2;
            preset.gridHeight = 2;
            preset.blockSize = 100f;
            preset.roadWidth = 12f;
            preset.roadHeight = 0.15f;
            preset.intersectionSize = 20f;
            preset.generateCurbs = true;
            preset.curbWidth = 0.6f;
            preset.curbHeight = 0.3f;
            preset.generatePoles = true;
            preset.poleHeight = 5f;
            preset.poleSpacing = 30f;
            preset.generateTrafficLights = true;
            preset.outputPath = "Assets/Generated/Roads/Highway/";
            preset.meshName = "HighwayRoad";

            CreatePresetAsset(preset, "HighwayPreset");
        }

        private static void CreatePresetAsset(RoadGeneratorPreset preset, string name)
        {
            string folderPath = "Assets/Editor/RoadGenerator/Presets";
            
            // Create folder if it doesn't exist
            if (!System.IO.Directory.Exists(folderPath))
            {
                System.IO.Directory.CreateDirectory(folderPath);
            }

            string assetPath = $"{folderPath}/{name}.asset";
            AssetDatabase.CreateAsset(preset, assetPath);
        }
    }
} 