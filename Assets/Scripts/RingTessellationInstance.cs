using UnityEngine;

[DisallowMultipleComponent]
[ExecuteAlways]
public sealed class RingTessellationInstance : MonoBehaviour
{
    [SerializeField] RingTessellationInstancedGroup _group;
    [SerializeField] float _radius = 0.5f;
    [SerializeField] float _ringWidth = 0.1f;
    [Range(1f, 64f)] [SerializeField] float _tess = 16f;
    [SerializeField] RingTessellationTessMode _tessMode = RingTessellationTessMode.LogDistance;
    [SerializeField] RingTessellationDebugVis _debugVis = RingTessellationDebugVis.Off;
    [SerializeField] Color _color = Color.white;

    RingTessellationInstancedGroup _registeredWith;

    public float Radius { get => _radius; set => _radius = value; }
    public float RingWidth { get => _ringWidth; set => _ringWidth = value; }
    public float Tess { get => _tess; set => _tess = value; }
    public RingTessellationTessMode TessMode { get => _tessMode; set => _tessMode = value; }
    public RingTessellationDebugVis DebugVis { get => _debugVis; set => _debugVis = value; }
    public Color Color { get => _color; set => _color = value; }

    public RingTessellationInstanceData InstanceData => new RingTessellationInstanceData
    {
        radius = _radius,
        ringWidth = _ringWidth,
        tess = _tess,
        tessMode = (float)_tessMode,
        debugVis = (float)_debugVis,
        pad = 0f,
        color = _color
    };

    void OnEnable()
    {
        var g = _group != null ? _group : GetComponentInParent<RingTessellationInstancedGroup>();
        if (g == null)
        {
            Debug.LogWarning($"{nameof(RingTessellationInstance)}: no {nameof(RingTessellationInstancedGroup)} assigned or in parents.", this);
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
