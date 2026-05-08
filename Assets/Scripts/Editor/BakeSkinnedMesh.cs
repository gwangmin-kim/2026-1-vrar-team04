using UnityEngine;
using UnityEditor;

public class BakeSkinnedMesh
{
    [MenuItem("Tools/Bake Skinned Mesh To Static")]
    private static void Bake()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogError("[BakeSkinnedMesh] GameObject를 선택해주세요");
            return;
        }

        SkinnedMeshRenderer smr = selected.GetComponent<SkinnedMeshRenderer>();
        if (smr == null)
        {
            Debug.LogError("[BakeSkinnedMesh] SkinnedMeshRenderer가 없습니다");
            return;
        }

        Mesh baked = new Mesh();
        baked.name = smr.sharedMesh.name + "_Baked";
        smr.BakeMesh(baked);

        string path = EditorUtility.SaveFilePanelInProject(
            "Save Baked Mesh",
            baked.name + ".asset",
            "asset",
            "베이크된 메쉬 저장 위치 선택"
        );

        if (string.IsNullOrEmpty(path)) return;

        AssetDatabase.CreateAsset(baked, path);
        AssetDatabase.SaveAssets();

        Material[] mats = smr.sharedMaterials;

        Object.DestroyImmediate(smr);

        MeshFilter mf = selected.AddComponent<MeshFilter>();
        mf.sharedMesh = baked;
        MeshRenderer mr = selected.AddComponent<MeshRenderer>();
        mr.sharedMaterials = mats;

        Debug.Log($"[BakeSkinnedMesh] 변환 완료: {path}");
    }
}
