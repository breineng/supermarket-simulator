using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window that rescales the selected GameObject to match real‑world dimensions in metres.
/// Drop this file into an "Editor" folder and open it via Tools ▸ Scale To Real Size.
/// 
/// ▶ Новое: можно задать одну целевую размерность (X, Y или Z) — остальные стороны изменятся пропорционально.
/// Скрипт всегда опирается на *текущий* «истинный» размер объекта в метрах (из BoundingBox всех Renderer'ов в world‑space).
/// </summary>
public class ScaleToRealSizeWindow : EditorWindow
{
    private enum Axis { X, Y, Z }

    private GameObject _target;

    // Полный режим (задаём сразу 3 стороны)
    private Vector3 _desiredSizeMetres = Vector3.one;

    // Униформ‑режим (одна ось + пропорционально)
    private bool _uniformScale = true;
    private Axis _uniformAxis = Axis.Y;
    private float _uniformDesired = 1f;

    private Bounds _currentBounds;

    [MenuItem("Tools/Scale To Real Size")]
    private static void ShowWindow()
    {
        var window = GetWindow<ScaleToRealSizeWindow>();
        window.titleContent = new GUIContent("Scale To Real Size");
        window.minSize = new Vector2(320, 220);
        window.RefreshSelection();
    }

    private void OnSelectionChange()
    {
        RefreshSelection();
        Repaint();
    }

    private void RefreshSelection()
    {
        if (Selection.activeGameObject == _target) return;
        _target = Selection.activeGameObject;
        CalculateCurrentBounds();
        // Авто‑подставляем текущий размер в поле «Desired» для удобства
        UpdateUniformDesiredFromCurrent();
    }

    /// <summary> Пересчитываем world‑space Bounds. </summary>
    private void CalculateCurrentBounds()
    {
        _currentBounds = new Bounds();
        if (!_target) return;
        var renderers = _target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        _currentBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            _currentBounds.Encapsulate(renderers[i].bounds);
    }

    private void UpdateUniformDesiredFromCurrent()
    {
        if (!_uniformScale) return;
        _uniformDesired = GetAxisValue(_currentBounds.size, _uniformAxis);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);
        _target = (GameObject)EditorGUILayout.ObjectField(_target, typeof(GameObject), true);

        if (!_target)
        {
            EditorGUILayout.HelpBox("Выберите объект, содержащий Renderer.", MessageType.Info);
            return;
        }

        if (GUILayout.Button("↻ Recalculate Current Size"))
        {
            CalculateCurrentBounds();
            UpdateUniformDesiredFromCurrent();
        }

        Vector3 currentSize = _currentBounds.size;
        EditorGUILayout.LabelField("Current Size (m):", $"{currentSize.x:F3} × {currentSize.y:F3} × {currentSize.z:F3}");

        EditorGUILayout.Space();

        // Выбор режима
        bool newUniform = EditorGUILayout.ToggleLeft("Uniform Scale (keep proportions)", _uniformScale);
        if (newUniform != _uniformScale)
        {
            _uniformScale = newUniform;
            UpdateUniformDesiredFromCurrent();
        }

        if (_uniformScale)
        {
            EditorGUI.BeginChangeCheck();
            Axis previousAxis = _uniformAxis;
            _uniformAxis = (Axis)EditorGUILayout.EnumPopup("Reference Axis", _uniformAxis);
            if (EditorGUI.EndChangeCheck())
            {
                // Если пользователь сменил ось — подставляем актуальный размер по новой оси.
                UpdateUniformDesiredFromCurrent();
            }
            _uniformDesired = EditorGUILayout.FloatField("Desired Size (m)", _uniformDesired);

            float factorPreview = GetAxisValue(currentSize, _uniformAxis) > 0f
                ? _uniformDesired / GetAxisValue(currentSize, _uniformAxis)
                : 1f;
            EditorGUILayout.LabelField("Scale Factor", factorPreview.ToString("F3"));
        }
        else
        {
            _desiredSizeMetres = EditorGUILayout.Vector3Field("Desired Size (m)", _desiredSizeMetres);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Apply Scale"))
            ApplyScale(currentSize);
    }

    private void ApplyScale(Vector3 currentSize)
    {
        if (currentSize == Vector3.zero) return;

        Undo.RegisterCompleteObjectUndo(_target.transform, "Scale To Real Size");
        Vector3 newLocalScale = _target.transform.localScale;

        if (_uniformScale)
        {
            float currentAxisSize = GetAxisValue(currentSize, _uniformAxis);
            if (currentAxisSize <= 0f) return;
            float factor = _uniformDesired / currentAxisSize; // главное — отношение «желаемый/текущий» в метрах
            newLocalScale *= factor;
        }
        else
        {
            // Масштабируем каждую сторону относительно её текущего размера (м)
            if (currentSize.x > 0f) newLocalScale.x *= _desiredSizeMetres.x / currentSize.x;
            if (currentSize.y > 0f) newLocalScale.y *= _desiredSizeMetres.y / currentSize.y;
            if (currentSize.z > 0f) newLocalScale.z *= _desiredSizeMetres.z / currentSize.z;
        }

        _target.transform.localScale = newLocalScale;
        CalculateCurrentBounds();
    }

    private static float GetAxisValue(Vector3 v, Axis axis)
    {
        return axis switch
        {
            Axis.X => v.x,
            Axis.Y => v.y,
            Axis.Z => v.z,
            _ => v.y,
        };
    }
}
