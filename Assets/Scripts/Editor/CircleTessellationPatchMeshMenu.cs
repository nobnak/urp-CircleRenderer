using UnityEditor;
using UnityEngine;

public static class CircleTessellationPatchMeshMenu
{
    const string MenuPath = "Assets/Create/Meshes/Circle Tessellation Patch";

    [MenuItem(MenuPath, false, 100)]
    static void CreateMeshAsset()
    {
        var mesh = CircleTessellationPatchMesh.Create();
        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/CircleTessellationPatch.asset");
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(mesh);
        Selection.activeObject = mesh;
    }
}
