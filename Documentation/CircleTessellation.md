# Circle tessellation（スクリプト注釈の集約）

元は各 C# ファイルの XML ドキュメントコメントに書いていた内容をここにまとめたものです。

## `CircleTessellationInstance.cs`

`CircleTessellationInstancedGroup` に登録され、Transform とパラメータから円インスタンスを描画する。

## `CircleTessellationInstancedGroup.cs`

`CircleTessellationInstance` を保持し、毎フレーム `Update` で `CircleTessellationInstancedRenderer.AddInstances` を通じてレンダラーに累積登録する。

## `CircleTessellationInstancedRenderer.cs`

- `CircleTessellationInstanceData` を StructuredBuffer に載せ、`Graphics.DrawMeshInstanced` で複数円を描画する。
- 同一フレーム内で `AddInstance` / `AddInstances` を複数回呼び出して累積でき、`LateUpdate` でまとめて描画する。
- 累積バッファは再利用し、不足時のみ 16 境界で拡張する（毎フレームの配列確保は行わない）。
- マテリアルは GPU Instancing を有効にし、シェーダーは `Custom/CircleTessellationInstanced` を使用する。
- `ExecuteAlways` により再生していない Edit Mode の Scene ビューでも描画できる。
- 描画カメラは常に未指定（`null`＝既定の全カメラ向け）とする。

### メンバー

| シンボル | 説明 |
|----------|------|
| `AccumulatedCount` | このフレームに累積されたインスタンス数（読み取り専用用途）。 |
| `ClearFrameInstances` | このフレームの累積を破棄する。通常は不要（`LateUpdate` 後に自動クリア）。 |
| `AddInstance` | 1 件追加。呼び出し側で新しい配列を都度確保しないこと。 |
| `AddInstances` | 連続範囲をコピーして追加。`sourceStart` から `count` 件（両配列で同一オフセット）。 |
| `Draw` | ワールド行列とインスタンスデータ（同じ長さ）で描画する。 |
| `DrawOne` | 単一インスタンス（テスト用）。 |

## `CircleTessellationInstanceData.cs`

### `CircleTessellationTessMode`

シェーダー `ComputeArcTessellation` の mode と同値（0 / 1）。

### `CircleTessellationDebugVis`

シェーダー `EvalFragColor` の debugVis と同値（>0.5 でデバッグ表示）。

### `CircleTessellationInstanceData`（構造体）

`Custom/CircleTessellationInstanced` の StructuredBuffer とレイアウト一致。  
`tessMode`: `CircleTessellationTessMode` を float として格納。

## `CircleTessellationPatchMesh.cs`

`Custom/CircleTessellation` 用。3 セクタの三角形パッチを生成する。  
`uv.x` = 0（A: 中心）、1（B）、2（C） / `uv.y` = セクタ index。

## `RingTessellationQuadPatchMesh.cs`

`Custom/RingTessellationQuad` 用。3 quad パッチ（各 4 制御点）。uv は各パッチで (0,0),(1,0),(1,1),(0,1) = Domain の (u,v)。  
頂点位置は内周が単位円、外周が半径 2 の円上。`uv2.x` にセクタ index（0..2）。メッシュは `MeshTopology.Quads` 必須。
