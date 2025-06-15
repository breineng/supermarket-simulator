// MetallicSmoothnessMapGenerator.cs
using UnityEditor;
using UnityEngine;
using System.IO;

public class MetallicSmoothnessMapGenerator : EditorWindow
{
    Texture2D metallicTex;
    Texture2D roughnessTex;
    bool invertRoughness = true;

    [MenuItem("Tools/Generate Metallic-Smoothness Map")]
    static void Init()
    {
        var wnd = GetWindow<MetallicSmoothnessMapGenerator>("Metallic Map");
        wnd.minSize = new Vector2(320, 150);
    }

    void OnGUI()
    {
        GUILayout.Label("Исходные текстуры", EditorStyles.boldLabel);
        metallicTex  = (Texture2D)EditorGUILayout.ObjectField("Metallic (RGB)",  metallicTex,  typeof(Texture2D), false);
        roughnessTex = (Texture2D)EditorGUILayout.ObjectField("Roughness (A)", roughnessTex, typeof(Texture2D), false);
        invertRoughness = EditorGUILayout.Toggle("Инвертировать Roughness", invertRoughness);

        GUI.enabled = metallicTex && roughnessTex;
        if (GUILayout.Button("Сгенерировать и сохранить"))
            Generate();
        GUI.enabled = true;
    }

    void Generate()
    {
        if (metallicTex.width != roughnessTex.width || metallicTex.height != roughnessTex.height)
        {
            EditorUtility.DisplayDialog("Ошибка",
                "Размеры Metallic и Roughness текстур должны совпадать.", "OK");
            return;
        }

        string path = EditorUtility.SaveFilePanelInProject(
            "Сохранить карту",
            "Metallic_Smoothness_Map",
            "png",
            "Выберите место для сохранения");

        if (string.IsNullOrEmpty(path)) return;

        Texture2D metalReadable  = GetReadableCopy(metallicTex);
        Texture2D roughReadable  = GetReadableCopy(roughnessTex);

        int w = metalReadable.width, h = metalReadable.height;
        var result = new Texture2D(w, h, TextureFormat.RGBA32, false, true);

        Color[] mPixels = metalReadable.GetPixels();
        Color[] rPixels = roughReadable.GetPixels();

        for (int i = 0; i < mPixels.Length; i++)
        {
            float metal  = mPixels[i].r;             // берём R (или усредняем RGB, если нужно)
            float rough  = rPixels[i].r;             // предполагаем Roughness в R
            if (invertRoughness) rough = 1f - rough; // инверсия при необходимости

            result.SetPixel(i % w, i / w, new Color(metal, metal, metal, rough));
        }
        result.Apply();

        File.WriteAllBytes(path, result.EncodeToPNG());
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        // Настройка импорта — линейное пространство и альфа
        var importer = (TextureImporter)TextureImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = false;                       // линейный цвет
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.SaveAndReimport();

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Готово", "Текстура сохранена:\n" + path, "OK");
    }

    // Делает читаемую копию любой текстуры (на случай, если Read/Write выключен)
    Texture2D GetReadableCopy(Texture2D src)
    {
        var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        Graphics.Blit(src, rt);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        var copy = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false, true);
        copy.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        copy.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return copy;
    }
}
