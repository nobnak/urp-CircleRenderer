using UnityEngine;

/// <summary>
/// <see cref="CircleTessellationInstancedRenderer"/> に登録され、Transform とパラメータから円インスタンスを描画する。
/// </summary>
[DisallowMultipleComponent]
[ExecuteAlways]
public sealed class CircleTessellationInstance : MonoBehaviour
{
    [SerializeField] CircleTessellationInstancedRenderer _renderer;
    [SerializeField] float _radius = 0.5f;
    [Range(1f, 64f)] [SerializeField] float _tess = 16f;
    [SerializeField] CircleTessellationDebugVis _debugVis;
    [SerializeField] CircleTessellationTessMode _tessMode;
    [SerializeField] Color _color = Color.white;

    CircleTessellationInstancedRenderer _registeredWith;

    public float Radius { get => _radius; set => _radius = value; }
    public float Tess { get => _tess; set => _tess = value; }
    public CircleTessellationDebugVis DebugVis { get => _debugVis; set => _debugVis = value; }
    public CircleTessellationTessMode TessMode { get => _tessMode; set => _tessMode = value; }
    public Color Color { get => _color; set => _color = value; }

    public CircleTessellationInstanceData InstanceData => new CircleTessellationInstanceData
    {
        radius = _radius,
        tess = _tess,
        debugVis = (float)_debugVis,
        tessMode = (float)_tessMode,
        color = _color
    };

    void OnEnable()
    {
        var r = _renderer != null ? _renderer : GetComponentInParent<CircleTessellationInstancedRenderer>();
        if (r == null)
        {
            Debug.LogWarning($"{nameof(CircleTessellationInstance)}: no {nameof(CircleTessellationInstancedRenderer)} assigned or in parents.", this);
            return;
        }
        r.RegisterInstance(this);
        _registeredWith = r;
    }

    void OnDisable()
    {
        if (_registeredWith != null)
        {
            _registeredWith.UnregisterInstance(this);
            _registeredWith = null;
        }
    }
}
