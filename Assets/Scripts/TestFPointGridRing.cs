using EffSpace.Extensions;
using EffSpace.Models;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace EffSpace.Examples {

	/// <summary>
	/// <see cref="FPointGrid"/>（jp.nobnak.effspace）で粒子を管理し、最近傍距離を
	/// <see cref="RingInstancedRenderer"/> の円環で可視化する。
	/// シミュレーションはスクリーン座標の XY 上、メッシュはローカル XY 面上の円なので、
	/// このコンポーネントの <see cref="Transform"/> の向き・スケールがそのまま描画平面に乗る。
	/// 位置は <see cref="Camera.main"/> の正射影と同じ係数で親ローカルに書き込む（親の回転でワールドに乗る）。
	/// </summary>
	public class TestFPointGridRing : MonoBehaviour {

		public Tuner tuner = new Tuner();
		public Link link = new Link();

		Quaternion _fabLocalRotation = Quaternion.identity;
		Vector3 _fabLocalScale = Vector3.one;

		[SerializeField] RingInstancedRenderer _ringRenderer;
		[SerializeField] Color _ringColor = Color.white;
		[Tooltip("円環の太さ（ピクセル相当）。")]
		[SerializeField] float _ringWidthPixels = 2f;
		[SerializeField] [Range(3f, 64f)] float _ringTess = 20f;

		protected float2 screen;
		protected float2 pixelToWorldScale;
		protected FPointGrid grid;
		protected int2 cellCount;
		protected float2 cellSize;
		protected float2 fieldSize;

		protected float4x4 screenToWorld;
		protected List<Particle> particleList;

		void OnEnable() {
			if (link.fab == null) {
				Debug.LogWarning($"{nameof(TestFPointGridRing)}: assign {nameof(Link)}.{nameof(Link.fab)}.", this);
				enabled = false;
				return;
			}

			var fabT = link.fab.transform;
			_fabLocalRotation = fabT.localRotation;
			_fabLocalScale = fabT.localScale;

			var c = Camera.main;
			if (c == null) {
				Debug.LogWarning($"{nameof(TestFPointGridRing)}: no main camera.", this);
				enabled = false;
				return;
			}

			screen = new float2(c.pixelWidth, c.pixelHeight);

			var hSize = c.orthographicSize;
			var aspect = c.aspect;
			pixelToWorldScale = new float2(
				2f * aspect * hSize / screen.x,
				2f * hSize / screen.y);
			screenToWorld = float4x4.TRS(
				new float3(-aspect * hSize, -hSize, 0f),
				quaternion.identity,
				new float3(pixelToWorldScale.x, pixelToWorldScale.y, 1f)
				);

			RecommendGridFromExp(screen, tuner.grid, out cellCount, out cellSize);
			fieldSize = cellCount * cellSize;
			Debug.Log($"Screen: {screen}, Grid: n={cellCount}, field={fieldSize}");

			grid = new FPointGrid(cellCount, cellSize, float2.zero);

			var rand = Unity.Mathematics.Random.CreateFromIndex(31);
			particleList = new List<Particle>();
			for (var i = 0; i < tuner.count; i++) {
				var p = new Particle();
				var seed = rand.NextFloat2(float2.zero, screen);

				var go = Instantiate(link.fab, transform);
				ApplyParticleTransform(go.transform, math.transform(screenToWorld, new float3(seed, 0f)));

				var e = grid.Insert(i, seed);
				if (e < 0)
					Debug.LogWarning($"Position not on screen: {seed}");

				p.id = i;
				p.element = e;
				p.go = go;
				p.seed = seed;
				p.pos = new float3(seed, 0f);
				particleList.Add(p);
			}

			foreach (var p in particleList) p.go.SetActive(true);
		}

		void OnDisable() {
			if (particleList == null)
				return;
			for (var i = 0; i < particleList.Count; i++) {
				var p = particleList[i];
				Destroy(p.go);
			}
			particleList.Clear();
		}

		void Update() {
			var t = Time.time * tuner.freq;
			var dt = Time.deltaTime * tuner.speed;
			var counter = 0;
			for (var i = 0; i < particleList.Count; i++) {
				var p = particleList[i];
				if (p.element >= 0) grid.Remove(p.element);

				var pos = p.pos.xy;
				pos += dt * screen * new float2(
					noise.snoise(new float3(p.seed, t)),
					noise.snoise(new float3(-p.seed, t)));
				pos -= screen * math.floor(pos / screen);

				p.pos = new float3(pos, p.pos.z);
				p.go.transform.localPosition = (Vector3)math.transform(screenToWorld, p.pos);

				p.element = grid.Insert(p.id, pos);
				if (p.element >= 0) counter++;
			}

			var cdiff = particleList.Count - counter;
			if (cdiff != 0) Debug.LogWarning($"Num particles outside of grid: {cdiff}");

			DrawNeighborRingsAsInstances();
		}

		void DrawNeighborRingsAsInstances() {
			if (_ringRenderer == null) {
				if (particleList.Count > 0)
					Debug.LogWarning($"{nameof(TestFPointGridRing)}: assign {nameof(RingInstancedRenderer)}.", this);
				return;
			}

			_ringRenderer.ClearFrameInstances();

			var qrange = 0.1f * screen.yy;
			var search_limit_dist_sq = 2f * qrange.y * qrange.y;
			float pix = 0.5f * (pixelToWorldScale.x + pixelToWorldScale.y);
			float ringWidthLocal = math.max(1e-4f, _ringWidthPixels * pix);

			for (var i = 0; i < particleList.Count; i++) {
				var p = particleList[i];
				var pos = p.pos.xy;
				var min_dist_sq = float.MaxValue;
				foreach (var e in grid.Query(pos - qrange, pos + qrange)) {
					if (e == p.element) continue;

					var eq = grid.grid.elements[e];
					var q = particleList[eq.id];

					var qpos = q.pos.xy;
					var dist_sq = math.distancesq(qpos, pos);
					if (dist_sq < min_dist_sq)
						min_dist_sq = dist_sq;
				}
				if (min_dist_sq > search_limit_dist_sq) continue;

				var d = math.sqrt(min_dist_sq);
				float radiusLocal = 0.5f * d * pix;
				if (radiusLocal <= 1e-4f) continue;

				var localPos = (Vector3)math.transform(screenToWorld, p.pos);
				var matrix = transform.localToWorldMatrix * Matrix4x4.TRS(localPos, Quaternion.identity, Vector3.one);
				var data = new RingInstanceData {
					radius = radiusLocal,
					ringWidth = ringWidthLocal,
					tess = _ringTess,
					tessMode = (float)RingTessMode.Fixed,
					debugVis = (float)RingDebugVis.Off,
					pad = 0f,
					color = _ringColor
				};
				_ringRenderer.AddInstance(matrix, data);
			}
		}

		/// <summary>
		/// Unity の Instantiate(プレハブ, 親) は子の local 姿勢を初期化するため、
		/// プレハブのルートの回転・スケールを毎回復元する。
		/// </summary>
		void ApplyParticleTransform(Transform t, float3 localPosition) {
			t.localPosition = localPosition;
			t.localRotation = _fabLocalRotation;
			t.localScale = _fabLocalScale;
		}

		/// <summary>
		/// EffSpace 2.x の <see cref="FPointGridExt.RecommendGrid"/> は第2引数が縦方向セル数。
		/// 従来の <c>1 &lt;&lt; grid</c> を総セル数の目安として縦セル数に換算する。
		/// </summary>

		static void RecommendGridFromExp(float2 screen, int gridExponent, out int2 cellCount, out float2 cellSize) {
			var total = 1 << Mathf.Clamp(gridExponent, 0, 20);
			var vert = math.max(1, (int)math.round(math.sqrt(total * screen.y / math.max(1e-5f, screen.x))));
			FPointGridExt.RecommendGrid(screen, vert, out cellCount, out cellSize);
		}

		[System.Serializable]
		public class Tuner {
			[Tooltip("総セル数の目安は 2^grid。EffSpace 2.x 用に縦セル数へ換算して RecommendGrid に渡す。")]
			public int grid = 10;
			public int count = 10;
			public float speed = 0.1f;
			public float freq = 0.1f;
		}

		[System.Serializable]
		public class Link {
			public GameObject fab;
		}

		[System.Serializable]
		public class Particle {
			public int id;
			public int element;
			public float2 seed;
			public float3 pos;
			public GameObject go;

			public override string ToString() {
				return $"{GetType().Name}: {id}/{element}, pos={pos}";
			}
		}
	}

}
