using UnityEngine;
using UnityEditor;

public class TerrainToMesh : EditorWindow
{
    [MenuItem("Tools/Terrain To Mesh")]
    static void Convert()
    {
        Terrain terrain = Selection.activeGameObject?.GetComponent<Terrain>();
        if (terrain == null)
        {
            Debug.LogWarning("Hierarchy에서 Terrain 오브젝트를 선택한 뒤 실행하세요.");
            return;
        }

        TerrainData data = terrain.terrainData;
        int fullRes = data.heightmapResolution;

        // 너무 크면 버텍스 수를 줄임 (257 이하 권장)
        int resolution = fullRes <= 257 ? fullRes : 257;

        // 전체 heightmap을 읽고 균등 간격으로 샘플링
        float[,] heights = data.GetHeights(0, 0, fullRes, fullRes);

        Vector3[] verts = new Vector3[resolution * resolution];
        Vector2[] uvs   = new Vector2[resolution * resolution];
        int[]     tris  = new int[(resolution - 1) * (resolution - 1) * 6];

        for (int y = 0; y < resolution; y++)
        for (int x = 0; x < resolution; x++)
        {
            // 전체 heightmap 범위에서 균등 샘플 인덱스 계산
            int hx = Mathf.RoundToInt((float)x / (resolution - 1) * (fullRes - 1));
            int hy = Mathf.RoundToInt((float)y / (resolution - 1) * (fullRes - 1));

            int i = y * resolution + x;
            verts[i] = new Vector3(
                (float)x / (resolution - 1) * data.size.x,
                heights[hy, hx] * data.size.y,
                (float)y / (resolution - 1) * data.size.z);
            uvs[i] = new Vector2((float)x / (resolution - 1), (float)y / (resolution - 1));
        }

        int t = 0;
        for (int y = 0; y < resolution - 1; y++)
        for (int x = 0; x < resolution - 1; x++)
        {
            int i = y * resolution + x;
            tris[t++] = i;         tris[t++] = i + resolution;     tris[t++] = i + 1;
            tris[t++] = i + 1;     tris[t++] = i + resolution;     tris[t++] = i + resolution + 1;
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices  = verts;
        mesh.triangles = tris;
        mesh.uv        = uvs;
        mesh.RecalculateNormals();

        string path = "Assets/TerrainMesh.asset";
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();

        GameObject go = new GameObject("Terrain_Rough");
        go.transform.position = terrain.transform.position;
        go.AddComponent<MeshFilter>().sharedMesh   = mesh;
        go.AddComponent<MeshRenderer>();
        go.AddComponent<MeshCollider>().sharedMesh = mesh;
        go.layer = LayerMask.NameToLayer("Rough");

        Undo.RegisterCreatedObjectUndo(go, "Terrain To Mesh");
        Debug.Log($"완료 — position: {terrain.transform.position}, size: {data.size}, resolution: {fullRes}→{resolution}");
    }
}
