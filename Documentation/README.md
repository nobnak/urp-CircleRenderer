# urp-CircleRenderer — 技術ドキュメント

円／リングのパッチメッシュ、テッセレーション用シェーダー、および URP 上での GPU インスタンシング描画の要点。実装（`Packages/jp.nobnak.circle` の Runtime / Shaders）と照合済み。

## 目次

1. [GPU インスタンシング（C#）](#1-gpu-インスタンシングc)
2. [円（`Custom/Circle` / `Custom/CircleInstanced`）](#2-円customcircle--customcircleinstanced)
3. [リング（`Custom/Ring` / `Custom/RingInstanced`）](#3-リングcustomring--customringinstanced)
4. [主要ファイル一覧](#4-主要ファイル一覧)

---

## 1. GPU インスタンシング（C#）

### 1.1 概要

`ComputeBuffer`（`ComputeBufferType.Structured`）に per-instance データを載せ、`MaterialPropertyBlock.SetBuffer` でバインドし、`Graphics.DrawMeshInstanced` で描画する。`LightProbeUsage.Off` を指定している。

円は `Custom/CircleInstanced`、リングは `Custom/RingInstanced`。ロジックは `InstancedRendererBase<TData>` で共通化し、`TData` とバッファ用 `PropertyToID`、フォールバックシェーダー名、デフォルトメッシュ生成だけが具象側で異なる。

### 1.2 `InstancedRendererBase<TData>`

| 項目 | 実装の事実 |
|------|------------|
| ジェネリクス | コンポーネントに付けるのは **常に具象型**（`CircleInstancedRenderer` 等）。基底だけ `InstancedRendererBase<CircleInstanceData>` の形。Unity では **ジェネリックな `MonoBehaviour` をその型のままコンポーネントに付けることは不可**。 |
| ストライド | `Marshal.SizeOf<TData>()`。C# の `TData` と各 `*Instanced.shader` 内の `struct` を一致させる。 |
| バッチ上限 | `kMaxInstancesPerDraw == 1023`。累積描画・`Draw` ともオフセットループで分割。 |
| GPU バッファ容量 | `AlignedDrawBatchCapacity` で 16 に切り上げつつ 1023 を上限にクランプ。 |
| 累積バッファ | `_accumMatrices` と `_accumData` は不足時のみ拡張。拡張時のキャパシティは `Align16`（16 の倍数）。 |
| シェーダー指定 | シリアライズされるのは `_shader`。未設定時は `Shader.Find(FallbackInstancedShaderName)`。実行時に `new Material(shader)`（`HideFlags.HideAndDontSave`、`enableInstancing = true`）。マテリアルアセットは不要。 |
| ライフサイクル | `OnEnable` でマテリアル再構築・`EnsureDefaultMeshIfNull`。`OnDisable` / `OnDestroy` で `ComputeBuffer` と生成マテリアルを解放。 |
| エディタ | `[ExecuteAlways]`。`UNITY_EDITOR` 時のみ `OnValidate` でシェーダー変更に追随。 |
| カメラ | `DrawMeshInstanced` のカメラ引数は `null`。 |

**派生がオーバーライドするもの**

| 項目 | 円 | リング |
|------|-----|--------|
| `TData` | `CircleInstanceData` | `RingInstanceData` |
| `InstanceBufferPropertyId` | `_CircleInstances` | `_RingInstances` |
| `FallbackInstancedShaderName` | `Custom/CircleInstanced` | `Custom/RingInstanced` |
| `EnsureDefaultMeshIfNull` | `CirclePatchMesh.Create()` | `RingPatchMesh.Create()` |

**公開メンバー（基底）**

| シンボル | 説明 |
|----------|------|
| `Material` | 生成済み描画用マテリアル（取得のみ。setter はない）。 |
| `Mesh` | 描画メッシュ（get / set）。 |
| `AccumulatedCount` | 現在フレームの累積件数。 |
| `ClearFrameInstances` | 累積を破棄。 |
| `AddInstance` / `AddInstances` | フレーム累積へ追加。 |
| `Draw` / `DrawOne` | 累積とは別に、その場で描画。 |

**警告**  
`enableInstancing` がオフのとき、`GetType().Name`（具象レンダラー名）付きで警告を **一度だけ** 出す。

**`LateUpdate`**  
`DrawAccumulatedFrame()` の呼び出しは **`CircleInstancedRenderer` / `RingInstancedRenderer` のみ**に `void LateUpdate() => DrawAccumulatedFrame();` として置く。基底だけに置くと環境によって呼ばれないことがあるため。

**実行順**  
具象レンダラーに `[DefaultExecutionOrder(1000)]`。グループの `Update` で `AddInstances` したあと、同フレームの `LateUpdate` でフラッシュする想定。

### 1.3 `InstancedGroupScratch`

`EnsurePair<T>` で `Matrix4x4[]` と `T[]` を同じキャパシティにし、16 境界で拡張。`CircleInstancedGroup` / `RingInstancedGroup` のスクラッチ確保に使用。

### 1.4 `CircleInstancedGroup` / `RingInstancedGroup`

`[ExecuteAlways]`。子の `CircleInstance` / `RingInstance` を `List` で保持。`Update` で null を除き、`localToWorldMatrix` と `InstanceData` をスクラッチに書き、`…InstancedRenderer.AddInstances` に渡す。スクラッチ配列は `[NonSerialized]`。

### 1.5 `CircleInstance` / `RingInstance`

`[ExecuteAlways]`。`OnEnable` で `_group` があればそれを使い、なければ `GetComponentInParent<…Group>()`。見つからなければ警告して登録しない。`OnDisable` で登録解除。

### 1.6 インスタンシングとパッチの関係

`CircleInstanceData` / `RingInstanceData` の内容は各 `*Instanced.shader` の `StructuredBuffer` と一致させる。非インスタンス路の `Circle.shader` / `Ring.shader`（マテリアルプロパティ＋テッセレーション）は [§2](#2-円customcircle--customcircleinstanced)・[§3](#3-リングcustomring--customringinstanced)。

---

## 2. 円（`Custom/Circle` / `Custom/CircleInstanced`）

### 2.1 `CirclePatchMesh`

`Custom/Circle` 用。3 セクタの三角形パッチ。

- `uv.x`: 0 = 中心 A、1 = B、2 = C  
- `uv.y`: セクタ index  

### 2.2 `Circle.shader`（テッセレーション）

`PatchConstant` は `BuildPatchFactors` により、`edge[0]` が **u==0 側の弧 BC** の分割度。径方向 A–B / A–C は係数 1。

### 2.3 `CircleShared.hlsl`

3 セクタ（各パッチは中心 A と円周上の B/C）。`CircleParams` はパラメータ塊。`ComputeArcTess` が距離連動テッセレーション、`BuildPatchFactors` の `edge[0]` が弧 BC 向け係数、`EvalFragColor` がデバッグ用バリセントリック表示。

### 2.4 `CircleInstanceData` / `CircleInstance`

- `CircleTessMode` / `CircleDebugVis` はシェーダー側のモードと対応。GPU では `tessMode` / `debugVis` 等の **float** に列挙値を格納。  
- レイアウトは `Custom/CircleInstanced` の `StructuredBuffer<CircleInstanceData>`（`_CircleInstances`）と一致必須。

### 2.5 `CircleInstanced.shader`

`#pragma target 5.0`、ハル／ドメイン／フラグメント。インスタンスデータは `_CircleInstances` から `UNITY_GET_INSTANCE_ID` で参照。

---

## 3. リング（`Custom/Ring` / `Custom/RingInstanced`）

### 3.1 `RingPatchMesh`

`Custom/Ring` 用。3 quad、`MeshTopology.Quads`。メッシュ頂点は **内周 r=1・外周 r=2** の円上。`Ring.shader` の `Vert` で実際の `rIn` / `rOut` にスケールする。

- メッシュの **2 番目の UV チャンネル**（`uv2`）の **x** にセクタ index（0, 1, 2）。シェーダーでは `TEXCOORD1`（`sectorPack.x`）として読む。

### 3.2 `Ring.shader`（Quad テッセレーション）

Quad の `SV_TessFactor` は CP 順ではなく **UV の辺** に対応: `[0]=u==0`, `[1]=v==0`, `[2]=u==1`, `[3]=v==1`。`v==0` が内弧 (V0–V1)、`v==1` が外弧 (V2–V3)。`u==0` / `u==1` は径方向 (V3–V0, V1–V2)。

### 3.3 `RingInstanceData` / `RingInstance`

- `RingTessMode` / `RingDebugVis` とシェーダーが対応。GPU では対応 float に列挙値。  
- レイアウトは `Custom/RingInstanced` の `StructuredBuffer<RingInstanceData>`（`_RingInstances`）と一致必須。

### 3.4 `RingInstanced.shader`

インスタンスデータは `_RingInstances` から参照。

---

## 4. 主要ファイル一覧

| 種別 | パス（`Packages/jp.nobnak.circle/` 以下） |
|------|-------------------------|
| インスタンス基底 | `Runtime/InstancedRendererBase.cs` |
| グループ・スクラッチ | `Runtime/InstancedGroupScratch.cs`, `Runtime/CircleInstancedGroup.cs`, `Runtime/RingInstancedGroup.cs` |
| レンダラー | `Runtime/CircleInstancedRenderer.cs`, `Runtime/RingInstancedRenderer.cs` |
| インスタンス | `Runtime/CircleInstance.cs`, `Runtime/RingInstance.cs`, `Runtime/CircleInstanceData.cs`, `Runtime/RingInstanceData.cs` |
| メッシュ | `Runtime/CirclePatchMesh.cs`, `Runtime/RingPatchMesh.cs` |
| エディタ | `Editor/CirclePatchMeshMenu.cs`, `Editor/RingPatchMeshMenu.cs` |
| メッシュアセット | `Models/CirclePatch.asset`, `Models/RingPatch.asset` |
| シェーダー | `Shaders/Circle.shader`, `Shaders/CircleInstanced.shader`, `Shaders/Ring.shader`, `Shaders/RingInstanced.shader`, `Shaders/Includes/CircleShared.hlsl` |

リポジトリ直下の [README.md](../README.md) は本書への入口のみ。
