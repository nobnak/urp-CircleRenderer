using UnityEngine;

[DisallowMultipleComponent]
[ExecuteAlways]
public sealed class CircleInstance : MonoBehaviour
{
    [SerializeField] CircleInstancedGroup _group;
    [SerializeField] float _radius = 0.5f;
    [Range(1f, 64f)] [SerializeField] float _tess = 16f;
    [SerializeField] CircleDebugVis _debugVis;
    [SerializeField] CircleTessMode _tessMode;
    [SerializeField] Color _color = Color.white;

    CircleInstancedGroup _registeredWith;

    public float Radius { get => _radius; set => _radius = value; }
    public float Tess { get => _tess; set => _tess = value; }
    public CircleDebugVis DebugVis { get => _debugVis; set => _debugVis = value; }
    public CircleTessMode TessMode { get => _tessMode; set => _tessMode = value; }
    public Color Color { get => _color; set => _color = value; }

    public CircleInstanceData InstanceData => new CircleInstanceData
    {
        radius = _radius,
        tess = _tess,
        debugVis = (float)_debugVis,
        tessMode = (float)_tessMode,
        color = _color
    };

    void OnEnable()
    {
        var g = _group != null ? _group : GetComponentInParent<CircleInstancedGroup>();
        if (g == null)
        {
            Debug.LogWarning($"{nameof(CircleInstance)}: no {nameof(CircleInstancedGroup)} assigned or in parents.", this);
            return;
        }
        g.RegisterInstance(this);
        _registeredWith = g;
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
