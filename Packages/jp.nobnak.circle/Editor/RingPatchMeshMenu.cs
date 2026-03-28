using UnityEditor;
using UnityEngine;

public static class RingPatchMeshMenu
{
    const string MenuPath = "Assets/Create/Meshes/Ring Patch";

    [MenuItem(MenuPath, false, 101)]
    static void CreateMeshAsset()
    {
        var mesh = RingPatchMesh.Create();
        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/RingPatch.asset");
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(mesh);
        Selection.activeObject = mesh;
    }
}
