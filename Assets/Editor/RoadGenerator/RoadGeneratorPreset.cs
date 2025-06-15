using UnityEngine;

namespace SampleProject.Editor.RoadGenerator
{
    [CreateAssetMenu(fileName = "RoadGeneratorPreset", menuName = "Road Generator/Preset", order = 1)]
    public class RoadGeneratorPreset : ScriptableObject
    {
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

        public RoadGenerationSettings ToSettings()
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

        public void FromSettings(RoadGenerationSettings settings)
        {
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
        }
    }
} 