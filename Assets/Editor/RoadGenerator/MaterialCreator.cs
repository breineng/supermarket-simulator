using UnityEngine;
using UnityEditor;

namespace SampleProject.Editor.RoadGenerator
{
    public static class MaterialCreator
    {
        [MenuItem("Tools/Road Generator/Create Default Materials")]
        public static void CreateDefaultMaterials()
        {
            CreateRoadMaterial();
            CreateCurbMaterial();
            CreatePoleMaterial();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Default road generator materials created in Assets/Editor/RoadGenerator/Materials/");
        }

        private static void CreateRoadMaterial()
        {
            var material = new Material(Shader.Find("Standard"));
            material.name = "RoadMaterial";
            
            // Dark asphalt color
            material.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.1f);
            
            // Enable tiling
            material.mainTextureScale = new Vector2(1f, 4f); // Tile along road length

            CreateMaterialAsset(material, "RoadMaterial");
        }

        private static void CreateCurbMaterial()
        {
            var material = new Material(Shader.Find("Standard"));
            material.name = "CurbMaterial";
            
            // Concrete gray color
            material.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.3f);

            CreateMaterialAsset(material, "CurbMaterial");
        }

        private static void CreatePoleMaterial()
        {
            var material = new Material(Shader.Find("Standard"));
            material.name = "PoleMaterial";
            
            // Dark metal color
            material.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            material.SetFloat("_Metallic", 0.8f);
            material.SetFloat("_Smoothness", 0.6f);

            CreateMaterialAsset(material, "PoleMaterial");
        }

        private static void CreateMaterialAsset(Material material, string name)
        {
            string folderPath = "Assets/Editor/RoadGenerator/Materials";
            
            // Create folder if it doesn't exist
            if (!System.IO.Directory.Exists(folderPath))
            {
                System.IO.Directory.CreateDirectory(folderPath);
            }

            string assetPath = $"{folderPath}/{name}.mat";
            AssetDatabase.CreateAsset(material, assetPath);
        }
    }
} 