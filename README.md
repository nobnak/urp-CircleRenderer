# urp-CircleRenderer — technical reference

Patch meshes for circles and rings, tessellation shaders, and GPU instancing on URP. Cross-checked against the implementation under `Packages/jp.nobnak.circle` (Runtime / Shaders).

## Contents

1. [GPU instancing (C#)](#1-gpu-instancing-c)
2. [Circle](#2-circle)
3. [Ring](#3-ring)
4. [Key files](#4-key-files)

---

## 1. GPU instancing (C#)

### 1.1 Overview

Per-instance data lives in a `ComputeBuffer` (`ComputeBufferType.Structured`), bound with `MaterialPropertyBlock.SetBuffer`, and drawn with `Graphics.DrawMeshInstanced`. `LightProbeUsage.Off` is set.

Circles use `Custom/CircleInstanced`, rings use `Custom/RingInstanced`. Logic is shared in `InstancedRendererBase<TData>`; concrete types differ only in `TData`, the buffer `PropertyToID`, the fallback shader name, and default mesh creation.

### 1.2 `InstancedRendererBase<TData>`

| Topic | Behavior |
|------|------------|
| Generics | Components must be **concrete types** (e.g. `CircleInstancedRenderer`). Only the base uses the shape `InstancedRendererBase<CircleInstanceData>`. Unity **cannot** attach a generic `MonoBehaviour` as that generic type. |
| Stride | `Marshal.SizeOf<TData>()`. The C# `TData` must match the `struct` in each `*Instanced.shader`. |
| Batch cap | `kMaxInstancesPerDraw == 1023`. Both accumulated draws and `Draw` split via offset loops. |
| GPU buffer size | `AlignedDrawBatchCapacity` rounds up to 16 and clamps to 1023. |
| Accumulation buffers | `_accumMatrices` and `_accumData` grow only when needed; new capacity uses `Align16` (multiple of 16). |
| Shader | Serialized field `_shader`. If unset, `Shader.Find(FallbackInstancedShaderName)`. At runtime, `new Material(shader)` with `HideFlags.HideAndDontSave` and `enableInstancing = true`. No material asset required. |
| Lifecycle | `OnEnable` rebuilds material and `EnsureDefaultMeshIfNull`. `OnDisable` / `OnDestroy` release `ComputeBuffer` and generated materials. |
| Editor | `[ExecuteAlways]`. Under `UNITY_EDITOR`, `OnValidate` reacts to shader changes. |
| Camera | `DrawMeshInstanced` is called with camera argument `null`. |

**What derived types override**

| Topic | Circle | Ring |
|------|--------|------|
| `TData` | `CircleInstanceData` | `RingInstanceData` |
| `InstanceBufferPropertyId` | `_CircleInstances` | `_RingInstances` |
| `FallbackInstancedShaderName` | `Custom/CircleInstanced` | `Custom/RingInstanced` |
| `EnsureDefaultMeshIfNull` | `CirclePatchMesh.Create()` | `RingPatchMesh.Create()` |

**Public members (base)**

| Symbol | Description |
|----------|------|
| `Material` | Draw material (get only; no setter). |
| `Mesh` | Mesh to draw (get / set). |
| `AccumulatedCount` | Count accumulated for the current frame. |
| `ClearFrameInstances` | Clears accumulation. |
| `AddInstance` / `AddInstances` | Append to frame accumulation. |
| `Draw` / `DrawOne` | Draw immediately, separate from accumulation. |

**Warning**  
If `enableInstancing` is off, logs a warning **once**, including `GetType().Name` (concrete renderer).

**`LateUpdate`**  
Only **`CircleInstancedRenderer` / `RingInstancedRenderer`** should call `DrawAccumulatedFrame()` from `void LateUpdate() => DrawAccumulatedFrame();`. Putting this only on the base can fail to run in some setups.

**Execution order**  
Concrete renderers use `[DefaultExecutionOrder(1000)]`. Intended flow: group `Update` calls `AddInstances`, then `LateUpdate` on the renderer flushes the same frame.

### 1.3 `InstancedGroupScratch`

`EnsurePair<T>` keeps `Matrix4x4[]` and `T[]` at the same capacity, growing on 16-byte boundaries. Used by `CircleInstancedGroup` / `RingInstancedGroup` for scratch.

### 1.4 `CircleInstancedGroup` / `RingInstancedGroup`

`[ExecuteAlways]`. Children `CircleInstance` / `RingInstance` are held in a `List`. Each `Update`, after dropping nulls, writes `localToWorldMatrix` and `InstanceData` into scratch and passes them to `…InstancedRenderer.AddInstances`. Scratch arrays are `[NonSerialized]`.

### 1.5 `CircleInstance` / `RingInstance`

`[ExecuteAlways]`. `OnEnable` uses `_group` if set; otherwise `GetComponentInParent<…Group>()`. If none is found, warns and does not register. `OnDisable` unregisters.

### 1.6 Instancing vs patch shaders

`CircleInstanceData` / `RingInstanceData` must match each `*Instanced.shader` `StructuredBuffer`. Non-instanced `Circle.shader` / `Ring.shader` (material properties + tessellation) are covered in [§2](#2-circle) and [§3](#3-ring).

---

## 2. Circle

Shaders: `Custom/Circle`, `Custom/CircleInstanced`.

### 2.1 `CirclePatchMesh`

For `Custom/Circle`: a three-sector triangle patch.

- `uv.x`: 0 = center A, 1 = B, 2 = C  
- `uv.y`: sector index  

### 2.2 `Circle.shader` (tessellation)

`PatchConstant` comes from `BuildPatchFactors`: `edge[0]` is the tessellation factor for **arc BC on the u==0 side**. Radial edges A–B / A–C use factor 1.

### 2.3 `CircleShared.hlsl`

Three sectors per patch (center A on the circle, B/C on the circumference). `CircleParams` groups parameters. `ComputeArcTess` handles distance-based tessellation; `BuildPatchFactors` uses `edge[0]` for arc BC; `EvalFragColor` shows debug barycentrics.

### 2.4 `CircleInstanceData` / `CircleInstance`

- `CircleTessMode` / `CircleDebugVis` mirror shader modes. On the GPU, enum values are stored in **float** fields such as `tessMode` / `debugVis`.  
- Layout must match `StructuredBuffer<CircleInstanceData>` (`_CircleInstances`) in `Custom/CircleInstanced`.

### 2.5 `CircleInstanced.shader`

`#pragma target 5.0`, hull / domain / fragment. Instance data is read from `_CircleInstances` via `UNITY_GET_INSTANCE_ID`.

---

## 3. Ring

Shaders: `Custom/Ring`, `Custom/RingInstanced`.

### 3.1 `RingPatchMesh`

For `Custom/Ring`: three quads, `MeshTopology.Quads`. Vertices sit on circles with **inner radius 1 and outer radius 2**. `Ring.shader` `Vert` scales them to the actual `rIn` / `rOut`.

- **Second UV channel** (`uv2`) **x** holds the sector index (0, 1, 2). The shader reads it as `TEXCOORD1` (`sectorPack.x`).

### 3.2 `Ring.shader` (quad tessellation)

Quad `SV_TessFactor` follows **UV edges**, not control-point order: `[0]=u==0`, `[1]=v==0`, `[2]=u==1`, `[3]=v==1`. `v==0` is the inner arc (V0–V1), `v==1` the outer arc (V2–V3). `u==0` / `u==1` are radial (V3–V0, V1–V2).

### 3.3 `RingInstanceData` / `RingInstance`

- `RingTessMode` / `RingDebugVis` align with the shader; enums are stored in matching float fields on the GPU.  
- Layout must match `StructuredBuffer<RingInstanceData>` (`_RingInstances`) in `Custom/RingInstanced`.

### 3.4 `RingInstanced.shader`

Instance data is read from `_RingInstances`.

---
