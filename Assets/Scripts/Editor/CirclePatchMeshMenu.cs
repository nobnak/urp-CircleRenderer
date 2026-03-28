using UnityEditor;
using UnityEngine;

public static class CirclePatchMeshMenu
{
    const string MenuPath = "Assets/Create/Meshes/Circle Patch";

    [MenuItem(MenuPath, false, 100)]
    static void CreateMeshAsset()
    {
        var mesh = CirclePatchMesh.Create();
        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/CirclePatch.asset");
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(mesh);
        Selection.activeObject = mesh;
    }
}
