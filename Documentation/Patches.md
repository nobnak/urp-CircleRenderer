# パッチメッシュ・インスタンシング（注釈の集約）

元は各 C# の XML ドキュメントに書いていた内容の要約です。

## 円（`Custom/Circle` / `Custom/CircleInstanced`）

### `CircleInstance.cs`

`CircleInstancedGroup` に登録され、Transform とパラメータから円インスタンスを描画する。

### `CircleInstancedGroup.cs`

`CircleInstance` を保持し、毎フレーム `Update` で `CircleInstancedRenderer.AddInstances` を通じてレンダラーに累積登録する。

### `CircleInstancedRenderer.cs`

- `CircleInstanceData` を StructuredBuffer に載せ、`Graphics.DrawMeshInstanced` で複数円を描画する。
- 同一フレーム内で `AddInstance` / `AddInstances` を複数回呼び出して累積でき、`LateUpdate` でまとめて描画する。
- 累積バッファは再利用し、不足時のみ 16 境界で拡張する。
- マテリアルは GPU Instancing を有効にし、シェーダーは `Custom/CircleInstanced` を使用する。
- `ExecuteAlways` により Edit Mode の Scene ビューでも描画できる。
- 描画カメラは未指定（`null`）。

| シンボル | 説明 |
|----------|------|
| `AccumulatedCount` | このフレームに累積されたインスタンス数。 |
| `ClearFrameInstances` | 累積を破棄（通常は不要）。 |
| `AddInstance` / `AddInstances` | 累積追加。 |
| `Draw` / `DrawOne` | 直接描画。 |

### `CircleInstanceData.cs`

- `CircleTessMode` / `CircleDebugVis`: シェーダー `CircleShared.hlsl` の `ComputeArcTess` / `EvalFragColor` と対応。
- 構造体は `Custom/CircleInstanced` の StructuredBuffer とレイアウト一致。

### `CirclePatchMesh.cs`

`Custom/Circle` 用。3 セクタの三角形パッチ。`uv.x` = 0（中心 A）、1（B）、2（C） / `uv.y` = セクタ index。

## リング（`Custom/Ring` / `Custom/RingInstanced`）

### `RingPatchMesh.cs`

`Custom/Ring` 用。3 quad パッチ。`MeshTopology.Quads`。内周 r=1・外周 r=2。`uv2.x` にセクタ index。

Ring 側のインスタンスは `RingInstance` / `RingInstancedGroup` / `RingInstancedRenderer` / `RingInstanceData`（`RingTessMode` / `RingDebugVis`）。Circle とコードは共有しない。
