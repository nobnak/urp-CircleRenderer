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
