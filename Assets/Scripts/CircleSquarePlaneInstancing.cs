using UnityEngine;

/// <summary>
/// 辺の長さ <see cref="_squareSize"/> の正方形（ローカル XY 面、Z=0）上に <see cref="_count"/> 個の円を
/// <see cref="CircleInstancedRenderer"/> で描画する。位置・色の配列は <see cref="OnEnable"/> で一度だけ構築する。
/// 行列インスタンシングの 511 個/バッチと StructuredBuffer の
/// オフセットは <see cref="InstancedRendererBase{TData}"/> 側で揃えられているが、
/// インスタンス番号に応じて色相を変え、ドロー境界や不整合があれば色のつながりで分かりやすくする。
/// </summary>
[ExecuteAlways]
[DefaultExecutionOrder(500)]
public sealed class CircleSquarePlaneInstancing : MonoBehaviour
{
    [SerializeField] CircleInstancedRenderer _renderer;
    [SerializeField] [Min(0)] int _count = 600;
    [SerializeField] [Min(1e-5f)] float _squareSize = 10f;
    [SerializeField] [Min(1e-5f)] float _radius = 0.08f;
    [Range(1f, 64f)] [SerializeField] float _tess = 12f;
    [SerializeField] CircleTessMode _tessMode = CircleTessMode.Fixed;
    [SerializeField] CircleDebugVis _debugVis = CircleDebugVis.Off;

    [Header("Color (HSV)")]
    [SerializeField] [Range(0f, 1f)] float _baseHue;
    [Tooltip("0..1 を超えると色相が周回。全インスタンスにわたる変化量の目安。")]
    [SerializeField] float _hueSpan = 1f;
    [SerializeField] [Range(0f, 1f)] float _saturation = 0.85f;
    [SerializeField] [Range(0f, 1f)] float _value = 1f;

    public enum LayoutMode
    {
        Grid,
        UniformRandom
    }

    [SerializeField] LayoutMode _layout = LayoutMode.Grid;
    [SerializeField] int _randomSeed = 1;

    [System.NonSerialized] Matrix4x4[] _matrices;
    [System.NonSerialized] CircleInstanceData[] _data;
    [System.NonSerialized] int _instanceCount;

    void OnEnable() => RebuildInstances();

    void Update()
    {
        if (_renderer == null || _instanceCount <= 0)
            return;
        _renderer.ClearFrameInstances();
        _renderer.AddInstances(_matrices, _data, _instanceCount, 0);
    }

    void RebuildInstances()
    {
        _instanceCount = _count;
        if (_instanceCount <= 0)
        {
            _matrices = null;
            _data = null;
            return;
        }

        InstancedGroupScratch.EnsurePair(ref _matrices, ref _data, _instanceCount);

        var trs = transform.localToWorldMatrix;
        var rng = new System.Random(_randomSeed);
        float half = 0.5f * _squareSize;

        int cols = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(_instanceCount)));

        for (int i = 0; i < _instanceCount; i++)
        {
            Vector2 p;
            if (_layout == LayoutMode.Grid)
            {
                int row = i / cols;
                int col = i - row * cols;
                int rows = Mathf.CeilToInt(_instanceCount / (float)cols);
                float cellW = _squareSize / cols;
                float cellH = _squareSize / rows;
                float x = -half + (col + 0.5f) * cellW;
                float y = -half + (row + 0.5f) * cellH;
                p = new Vector2(x, y);
            }
            else
            {
                p = new Vector2(
                    (float)(rng.NextDouble() * _squareSize - half),
                    (float)(rng.NextDouble() * _squareSize - half));
            }

            var pos = new Vector3(p.x, p.y, 0f);
            _matrices[i] = trs * Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);

            float hueT = _instanceCount > 1 ? i / (float)(_instanceCount - 1) : 0f;
            float h = _baseHue + hueT * _hueSpan;
            h -= Mathf.Floor(h);
            Color rgb = Color.HSVToRGB(h, _saturation, _value);

            _data[i] = new CircleInstanceData
            {
                radius = _radius,
                tess = _tess,
                debugVis = (float)_debugVis,
                tessMode = (float)_tessMode,
                color = rgb
            };
        }
    }
}
