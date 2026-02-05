using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 조인트 체인을 링크별 LineRenderer와 Gizmos로 시각화합니다. | Visualizes the joint chain with per-link LineRenderers and Gizmos.
/// </summary>
[ExecuteAlways]
public sealed class JointChainVisualizer : MonoBehaviour
{
    private const int LINK_COUNT = 6;
    private const float DEFAULT_MIN_WIDTH = 0.005f;
    private const float DEFAULT_MAX_WIDTH = 0.02f;
    private const float GIZMO_SPHERE_RADIUS = 0.015f;

    private static readonly Color DEFAULT_COLOR = new Color(0.25f, 0.9f, 1f, 1f);
    private static readonly Color GIZMO_NODE_COLOR = new Color(1f, 0.9f, 0.2f, 1f);

    [Header("Joint Transforms")]
    [SerializeField] private Transform _j1;
    [SerializeField] private Transform _j2;
    [SerializeField] private Transform _j3;
    [SerializeField] private Transform _j4;
    [SerializeField] private Transform _j5;
    [SerializeField] private Transform _j6;
    [SerializeField] private Transform _tcp;

    [Header("Link Styles")]
    [SerializeField] private Color[] _linkColors = new Color[LINK_COUNT];
    [SerializeField] private float[] _linkWidths = new float[LINK_COUNT];
    [SerializeField] private Material _sharedMaterial;
#if UNITY_EDITOR
    [Header("Debug (Editor Only)")]
    [SerializeField] private bool _logLink0PositionsOnce;
#endif

    private LineRenderer[] _linkRenderers;
    private Material[] _linkMaterials;
    private bool[] _missingWarnings;
    private bool _needsRebuild;
    private bool _rebuildScheduled;
    private bool _isDestroyed;
    private bool _missingColorPropertyLogged;
    private Material _lastSharedMaterial;
#if UNITY_EDITOR
    private bool _isEditorUpdateSubscribed;
#endif

    /// <summary>
    /// 라인 렌더러와 스타일 기본값을 준비합니다. | Prepares line renderers and default styles.
    /// </summary>
    private void Awake()
    {
        EnsureStyleDefaults();
        EnsureSharedMaterial();
        RequestRebuildRuntime();
    }

    /// <summary>
    /// 활성화 시 런타임 재구성을 보장합니다. | Ensures runtime rebuild on enable.
    /// </summary>
    private void OnEnable()
    {
        _isDestroyed = false;
        RequestRebuildRuntime();
#if UNITY_EDITOR
        SubscribeEditorUpdate();
#endif
    }

    /// <summary>
    /// 비활성화 시 예약된 재구성을 취소합니다. | Cancels scheduled rebuilds on disable.
    /// </summary>
    private void OnDisable()
    {
        _rebuildScheduled = false;
        _needsRebuild = false;
#if UNITY_EDITOR
        UnsubscribeEditorUpdate();
#endif
    }

    /// <summary>
    /// FK/IK 갱신 이후 최종 포즈에 맞춰 라인을 갱신합니다. | Updates link lines after FK/IK so they match the final pose.
    /// </summary>
    private void LateUpdate()
    {
        // FK/IK가 Update에서 적용될 수 있어 LateUpdate에서 최종 포즈를 사용합니다. | FK/IK may run in Update, so use LateUpdate for the final pose.
        if (_needsRebuild)
        {
            RebuildIfNeeded();
        }
        UpdateLinkPositions();
    }

    /// <summary>
    /// 인스펙터 값 변경 시 스타일을 즉시 반영합니다. | Applies styles immediately when inspector values change.
    /// </summary>
    private void OnValidate()
    {
        EnsureStyleDefaults();
        _needsRebuild = true;
        ScheduleEditorRebuild();
    }

    /// <summary>
    /// 에디터 씬 뷰에서 링크 색으로 Gizmos 라인을 표시합니다. | Draws Gizmos lines using per-link colors in the Scene view.
    /// </summary>
    private void OnDrawGizmos()
    {
        DrawGizmoLink(_j1, _j2, 0);
        DrawGizmoLink(_j2, _j3, 1);
        DrawGizmoLink(_j3, _j4, 2);
        DrawGizmoLink(_j4, _j5, 3);
        DrawGizmoLink(_j5, _j6, 4);
        DrawGizmoLink(_j6, _tcp, 5);

        Gizmos.color = GIZMO_NODE_COLOR;
        DrawGizmoNode(_j1);
        DrawGizmoNode(_j2);
        DrawGizmoNode(_j3);
        DrawGizmoNode(_j4);
        DrawGizmoNode(_j5);
        DrawGizmoNode(_j6);
        DrawGizmoNode(_tcp);
    }

    /// <summary>
    /// 런타임에서 즉시 재구성을 요청합니다. | Requests immediate rebuild at runtime.
    /// </summary>
    private void RequestRebuildRuntime()
    {
        _needsRebuild = true;
        RebuildIfNeeded();
    }

    /// <summary>
    /// 필요 시 라인 렌더러를 재구성합니다. | Rebuilds line renderers when needed.
    /// </summary>
    private void RebuildIfNeeded()
    {
        if (!_needsRebuild)
        {
            return;
        }

        EnsureSharedMaterial();
        EnsureLinkRenderers();
        EnsureLinkMaterials();
        ApplyStylesToRenderers();
        UpdateLinkPositions();
        _needsRebuild = false;
        _rebuildScheduled = false;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터 지연 재구성을 예약합니다. | Schedules a delayed rebuild in the editor.
    /// </summary>
    private void ScheduleEditorRebuild()
    {
        if (_rebuildScheduled)
        {
            return;
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        _rebuildScheduled = true;
        EditorApplication.delayCall += HandleEditorDelayRebuild;
    }

    /// <summary>
    /// 다음 에디터 틱에서 재구성을 수행합니다. | Performs rebuild on the next editor tick.
    /// </summary>
    private void HandleEditorDelayRebuild()
    {
        _rebuildScheduled = false;
        if (_isDestroyed || this == null || gameObject == null)
        {
            return;
        }

        if (!isActiveAndEnabled)
        {
            return;
        }

        RebuildIfNeeded();
    }
#endif

    /// <summary>
    /// 링크 렌더러 배열과 자식 오브젝트를 보장합니다. | Ensures link renderer array and child objects.
    /// </summary>
    private void EnsureLinkRenderers()
    {
        if (_linkRenderers == null || _linkRenderers.Length != LINK_COUNT)
        {
            _linkRenderers = new LineRenderer[LINK_COUNT];
        }

        if (_missingWarnings == null || _missingWarnings.Length != LINK_COUNT)
        {
            _missingWarnings = new bool[LINK_COUNT];
        }

        for (int i = 0; i < LINK_COUNT; i++)
        {
            string childName = $"BoneLine_{i}";
            Transform child = transform.Find(childName);
            if (child == null)
            {
                GameObject childObject = new GameObject(childName);
                childObject.transform.SetParent(transform, false);
                child = childObject.transform;
            }

            LineRenderer renderer = child.GetComponent<LineRenderer>();
            if (renderer == null)
            {
                renderer = child.gameObject.AddComponent<LineRenderer>();
            }

            _linkRenderers[i] = renderer;
            ConfigureRendererDefaults(renderer);
        }
    }

    /// <summary>
    /// 라인 렌더러 공통 기본값을 설정합니다. | Configures shared LineRenderer defaults.
    /// </summary>
    private void ConfigureRendererDefaults(LineRenderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        renderer.useWorldSpace = true;
        renderer.positionCount = 2;
    }

    /// <summary>
    /// 스타일 배열의 기본값을 보장합니다. | Ensures default values for style arrays.
    /// </summary>
    private void EnsureStyleDefaults()
    {
        if (_linkColors == null || _linkColors.Length != LINK_COUNT)
        {
            _linkColors = new Color[LINK_COUNT];
        }

        if (_linkWidths == null || _linkWidths.Length != LINK_COUNT)
        {
            _linkWidths = new float[LINK_COUNT];
        }

        for (int i = 0; i < LINK_COUNT; i++)
        {
            if (_linkColors[i] == default)
            {
                _linkColors[i] = DEFAULT_COLOR;
            }

            if (_linkWidths[i] <= 0f)
            {
                _linkWidths[i] = Mathf.Lerp(DEFAULT_MIN_WIDTH, DEFAULT_MAX_WIDTH, 0.4f);
            }
        }
    }

    /// <summary>
    /// URP 호환 기본 머티리얼을 보장합니다. | Ensures a URP-compatible default material.
    /// </summary>
    private void EnsureSharedMaterial()
    {
        if (_sharedMaterial != null)
        {
            return;
        }

        Shader selectedShader = FindColorCapableShader();
        if (selectedShader != null)
        {
            _sharedMaterial = new Material(selectedShader);
            return;
        }

        Debug.LogWarning("사용 가능한 색상 셰이더를 찾지 못했습니다 | No suitable color-capable shader found.", this);
    }

    /// <summary>
    /// 링크별 머티리얼 인스턴스를 준비합니다. | Prepares per-link material instances.
    /// </summary>
    private void EnsureLinkMaterials()
    {
        if (_sharedMaterial == null)
        {
            return;
        }

        if (_linkMaterials == null || _linkMaterials.Length != LINK_COUNT || _lastSharedMaterial != _sharedMaterial)
        {
            ReleaseLinkMaterials();
            _linkMaterials = new Material[LINK_COUNT];
            _lastSharedMaterial = _sharedMaterial;
        }

        for (int i = 0; i < LINK_COUNT; i++)
        {
            if (_linkMaterials[i] == null)
            {
                _linkMaterials[i] = new Material(_sharedMaterial);
            }
        }
    }

    /// <summary>
    /// 링크 머티리얼을 해제합니다. | Releases link materials.
    /// </summary>
    private void ReleaseLinkMaterials()
    {
        if (_linkMaterials == null)
        {
            return;
        }

        for (int i = 0; i < _linkMaterials.Length; i++)
        {
            if (_linkMaterials[i] == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(_linkMaterials[i]);
            }
            else
            {
                DestroyImmediate(_linkMaterials[i]);
            }

            _linkMaterials[i] = null;
        }
    }

    /// <summary>
    /// 링크별 스타일을 LineRenderer에 적용합니다. | Applies per-link styles to LineRenderers.
    /// </summary>
    private void ApplyStylesToRenderers()
    {
        if (_linkRenderers == null)
        {
            return;
        }

        for (int i = 0; i < LINK_COUNT; i++)
        {
            LineRenderer renderer = _linkRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            Color color = GetLinkColor(i);
            float width = GetClampedWidth(i);

            renderer.startColor = color;
            renderer.endColor = color;
            renderer.startWidth = width;
            renderer.endWidth = width;

            ApplyMaterialColor(renderer, i, color);
        }
    }

    /// <summary>
    /// 링크 위치를 갱신합니다. | Updates link positions.
    /// </summary>
    private void UpdateLinkPositions()
    {
        UpdateLinkPosition(0, _j1, _j2);
        UpdateLinkPosition(1, _j2, _j3);
        UpdateLinkPosition(2, _j3, _j4);
        UpdateLinkPosition(3, _j4, _j5);
        UpdateLinkPosition(4, _j5, _j6);
        UpdateLinkPosition(5, _j6, _tcp);
    }

    /// <summary>
    /// 단일 링크의 위치를 갱신합니다. | Updates a single link position.
    /// </summary>
    private void UpdateLinkPosition(int index, Transform from, Transform to)
    {
        LineRenderer renderer = GetRenderer(index);
        if (renderer == null)
        {
            return;
        }

        if (from == null || to == null)
        {
            renderer.enabled = false;
            LogMissingTransformOnce(index, from, to);
            return;
        }

        renderer.enabled = true;
        renderer.SetPosition(0, from.position);
        renderer.SetPosition(1, to.position);
        _missingWarnings[index] = false;
#if UNITY_EDITOR
        LogLink0PositionsOnce(index, renderer, from, to);
#endif
    }

    /// <summary>
    /// 누락된 Transform 경고를 링크당 1회만 출력합니다. | Logs missing Transform warning once per link.
    /// </summary>
    private void LogMissingTransformOnce(int index, Transform from, Transform to)
    {
        if (_missingWarnings == null || index < 0 || index >= _missingWarnings.Length)
        {
            return;
        }

        if (_missingWarnings[index])
        {
            return;
        }

        string fromName = from != null ? from.name : "(null)";
        string toName = to != null ? to.name : "(null)";
        Debug.LogWarning($"필수 Transform 누락: {fromName} -> {toName} | Missing Transform: {fromName} -> {toName}", this);
        _missingWarnings[index] = true;
    }

    /// <summary>
    /// 링크 색상을 안전하게 반환합니다. | Returns a safe link color.
    /// </summary>
    private Color GetLinkColor(int index)
    {
        if (_linkColors == null || index < 0 || index >= _linkColors.Length)
        {
            return DEFAULT_COLOR;
        }

        return _linkColors[index];
    }

    /// <summary>
    /// 링크 폭을 안전 범위로 보정합니다. | Clamps link width to a safe range.
    /// </summary>
    private float GetClampedWidth(int index)
    {
        if (_linkWidths == null || index < 0 || index >= _linkWidths.Length)
        {
            return DEFAULT_MIN_WIDTH;
        }

        float width = _linkWidths[index];
        if (width <= 0f)
        {
            width = DEFAULT_MIN_WIDTH;
        }

        return Mathf.Clamp(width, DEFAULT_MIN_WIDTH, DEFAULT_MAX_WIDTH);
    }

    /// <summary>
    /// 링크 렌더러를 안전하게 가져옵니다. | Gets a link renderer safely.
    /// </summary>
    private LineRenderer GetRenderer(int index)
    {
        if (_linkRenderers == null || index < 0 || index >= _linkRenderers.Length)
        {
            return null;
        }

        return _linkRenderers[index];
    }

    /// <summary>
    /// Gizmos 선을 그립니다. | Draws a Gizmos line.
    /// </summary>
    private void DrawGizmoLink(Transform from, Transform to, int index)
    {
        if (from == null || to == null)
        {
            return;
        }

        Gizmos.color = GetLinkColor(index);
        Gizmos.DrawLine(from.position, to.position);
    }

    /// <summary>
    /// Gizmos 노드를 그립니다. | Draws a Gizmos node.
    /// </summary>
    private static void DrawGizmoNode(Transform node)
    {
        if (node == null)
        {
            return;
        }

        Gizmos.DrawSphere(node.position, GIZMO_SPHERE_RADIUS);
    }

    /// <summary>
    /// 파괴 시 상태를 정리합니다. | Cleans up state on destroy.
    /// </summary>
    private void OnDestroy()
    {
        _isDestroyed = true;
        _rebuildScheduled = false;
        ReleaseLinkMaterials();
#if UNITY_EDITOR
        UnsubscribeEditorUpdate();
#endif
    }

    /// <summary>
    /// 링크 머티리얼 색상을 적용합니다. | Applies link material color.
    /// </summary>
    private void ApplyMaterialColor(LineRenderer renderer, int index, Color color)
    {
        if (_linkMaterials != null && index >= 0 && index < _linkMaterials.Length && _linkMaterials[index] != null)
        {
            renderer.sharedMaterial = _linkMaterials[index];
            SetMaterialColor(_linkMaterials[index], color);
            return;
        }

        if (_sharedMaterial != null)
        {
            renderer.sharedMaterial = _sharedMaterial;
            SetMaterialColor(_sharedMaterial, color);
        }
    }

    /// <summary>
    /// 머티리얼 컬러 프로퍼티를 설정합니다. | Sets material color property.
    /// </summary>
    private void SetMaterialColor(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
            return;
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
            return;
        }

        if (!_missingColorPropertyLogged)
        {
            Debug.LogWarning("머티리얼에 _BaseColor/_Color 프로퍼티가 없습니다 | Material has no _BaseColor/_Color property.", this);
            _missingColorPropertyLogged = true;
        }
    }

    /// <summary>
    /// 색상 적용 가능한 셰이더를 찾습니다. | Finds a color-capable shader.
    /// </summary>
    private Shader FindColorCapableShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader != null)
        {
            return shader;
        }

        shader = Shader.Find("Universal Render Pipeline/Particles/Lit");
        if (shader != null)
        {
            return shader;
        }

        shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            return shader;
        }

        shader = Shader.Find("Unlit/Color");
        if (shader != null)
        {
            Debug.LogWarning("URP 셰이더를 찾지 못해 Unlit/Color로 대체합니다 | URP shader not found; fallback to Unlit/Color.", this);
        }

        return shader;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터 업데이트를 구독합니다. | Subscribes to editor updates.
    /// </summary>
    private void SubscribeEditorUpdate()
    {
        if (_isEditorUpdateSubscribed)
        {
            return;
        }

        if (Application.isPlaying)
        {
            return;
        }

        EditorApplication.update += HandleEditorUpdate;
        _isEditorUpdateSubscribed = true;
    }

    /// <summary>
    /// 에디터 업데이트 구독을 해제합니다. | Unsubscribes from editor updates.
    /// </summary>
    private void UnsubscribeEditorUpdate()
    {
        if (!_isEditorUpdateSubscribed)
        {
            return;
        }

        EditorApplication.update -= HandleEditorUpdate;
        _isEditorUpdateSubscribed = false;
    }

    /// <summary>
    /// 에디터 프레임에서 링크 위치를 갱신합니다. | Updates link positions on editor frames.
    /// </summary>
    private void HandleEditorUpdate()
    {
        if (_isDestroyed || this == null || gameObject == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            return;
        }

        if (_needsRebuild)
        {
            RebuildIfNeeded();
        }
        else
        {
            UpdateLinkPositions();
        }
    }

    /// <summary>
    /// 링크 0 위치 디버그 로그를 1회 출력합니다. | Logs link 0 positions once for debugging.
    /// </summary>
    private void LogLink0PositionsOnce(int index, LineRenderer renderer, Transform from, Transform to)
    {
        if (index != 0 || !_logLink0PositionsOnce)
        {
            return;
        }

        Vector3 lineStart = renderer.GetPosition(0);
        Vector3 lineEnd = renderer.GetPosition(1);
        Debug.Log(
            $"Link0 From={from.position} To={to.position} LineStart={lineStart} LineEnd={lineEnd} | Link0 From/To and Line positions",
            this);
        _logLink0PositionsOnce = false;
    }
#endif
}
