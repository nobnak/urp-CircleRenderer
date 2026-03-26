using UnityEditor;
using UnityEngine;

public static class RingTessellationQuadPatchMeshMenu
{
    const string MenuPath = "Assets/Create/Meshes/Ring Tessellation Quad Patch";

    [MenuItem(MenuPath, false, 101)]
    static void CreateMeshAsset()
    {
        var mesh = RingTessellationQuadPatchMesh.Create();
        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/RingTessellationQuadPatch.asset");
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(mesh);
        Selection.activeObject = mesh;
    }
}
