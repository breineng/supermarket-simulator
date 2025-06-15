using UnityEditor;
using UnityEngine;

public static class PivotBottomCenterTool
{
    // --- MENU ----------------------------------------------------------------

    [MenuItem("Tools/Set Pivot/Bottom Center", validate = true)]
    private static bool ValidateMenu() => GetTargetMesh() != null;

    [MenuItem("Tools/Set Pivot/Bottom Center")]
    private static void SetPivotBottomCenter()
    {
        Mesh mesh = GetTargetMesh();
        if (mesh == null)
        {
            EditorUtility.DisplayDialog("Pivot Tool",
                "Выберите .asset-меш или GameObject, который его использует.", "OK");
            return;
        }

        string path = AssetDatabase.GetAssetPath(mesh);
        if (string.IsNullOrEmpty(path) || !path.EndsWith(".asset"))
        {
            EditorUtility.DisplayDialog("Pivot Tool",
                "Скрипт работает только с Mesh, сохранёнными в отдельном .asset-файле.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Переместить pivot?",
            $"Будет изменён файл:\n{path}\n\nРекомендуется сделать резервную копию.", "Изменить", "Отмена"))
            return;

        Undo.RegisterCompleteObjectUndo(mesh, "Pivot Bottom Center");

        // --- СМЕЩАЕМ ВЕРШИНЫ --------------------------------------------------

        Vector3[] verts   = mesh.vertices;
        Vector3[] normals = mesh.normals;   // сохраняем ориентацию
        Vector4[] tangs   = mesh.tangents;

        Bounds b = mesh.bounds;
        Vector3 bottomCenter = new Vector3(
            (b.min.x + b.max.x) * 0.5f,
            b.min.y,
            (b.min.z + b.max.z) * 0.5f);

        for (int i = 0; i < verts.Length; i++)
            verts[i] -= bottomCenter;

        mesh.vertices = verts;

        if (normals != null && normals.Length == verts.Length)
            mesh.normals = normals;
        if (tangs != null && tangs.Length == verts.Length)
            mesh.tangents = tangs;

        mesh.RecalculateBounds();           // только AABB

        EditorUtility.SetDirty(mesh);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Готово",
            "Pivot перенесён в нижнюю-центральную точку без изменения нормалей.", "OK");
    }

    // --- ВСПОМОГАТЕЛЬНОЕ ------------------------------------------------------

    /// <summary>Возвращает Mesh из выделения (.asset или sharedMesh на объекте).</summary>
    private static Mesh GetTargetMesh()
    {
        Object sel = Selection.activeObject;

        if (sel is Mesh assetMesh)
            return assetMesh;

        if (sel is GameObject go)
        {
            var mf  = go.GetComponent<MeshFilter>();
            if (mf && mf.sharedMesh) return mf.sharedMesh;

            var smr = go.GetComponent<SkinnedMeshRenderer>();
            if (smr && smr.sharedMesh) return smr.sharedMesh;
        }
        return null;
    }
}
