using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class TerrainSplitByLayer : EditorWindow
{
    [MenuItem("Tools/Terrain Split By Layer (OBJ Export)")]
    static void SplitAndExport()
    {
        Terrain terrain = Selection.activeGameObject?.GetComponent<Terrain>();
        if (terrain == null)
        {
            Debug.LogWarning("Hierarchy에서 Terrain 오브젝트를 선택한 뒤 실행하세요.");
            return;
        }

        TerrainData data = terrain.terrainData;
        int fullRes = data.heightmapResolution;
        int resolution = fullRes <= 257 ? fullRes : 257;

        float[,] heights = data.GetHeights(0, 0, fullRes, fullRes);
        int alphaRes = data.alphamapResolution;
        float[,,] alphamaps = data.GetAlphamaps(0, 0, alphaRes, alphaRes);
        int layerCount = data.alphamapLayers;

        TerrainLayer[] layers = data.terrainLayers;
        Debug.Log($"Terrain: res={fullRes}, alphaRes={alphaRes}, layers={layerCount}");
        for (int i = 0; i < layers.Length; i++)
            Debug.Log($"  Layer {i}: {layers[i].name}");

        string exportDir = Path.Combine(Application.dataPath, "ExportedTerrainLayers");
        if (!Directory.Exists(exportDir))
            Directory.CreateDirectory(exportDir);

        Vector3[] allVerts = new Vector3[resolution * resolution];
        Vector2[] allUVs = new Vector2[resolution * resolution];

        for (int y = 0; y < resolution; y++)
        for (int x = 0; x < resolution; x++)
        {
            int hx = Mathf.RoundToInt((float)x / (resolution - 1) * (fullRes - 1));
            int hy = Mathf.RoundToInt((float)y / (resolution - 1) * (fullRes - 1));
            int i = y * resolution + x;

            // Y에 terrain.transform.position.y(음수)를 더하면 지면이 통째로 0 아래로 내려가
            // 공이 닿지 못합니다. 수평(X/Z)은 기존대로 두고, 높이만 0 ~ data.size.y로 내보냅니다.
            allVerts[i] = new Vector3(
                (float)x / (resolution - 1) * data.size.x + terrain.transform.position.x,
                heights[hy, hx] * data.size.y,
                (float)y / (resolution - 1) * data.size.z + terrain.transform.position.z);
            allUVs[i] = new Vector2((float)x / (resolution - 1), (float)y / (resolution - 1));
        }

        // Pre-classify each face to the layer with highest weight (no gaps)
        int[,] faceLayer = new int[resolution - 1, resolution - 1];
        for (int y = 0; y < resolution - 1; y++)
        for (int x = 0; x < resolution - 1; x++)
        {
            float u = (float)x / (resolution - 1);
            float v = (float)y / (resolution - 1);
            int ax = Mathf.Clamp(Mathf.RoundToInt(u * (alphaRes - 1)), 0, alphaRes - 1);
            int ay = Mathf.Clamp(Mathf.RoundToInt(v * (alphaRes - 1)), 0, alphaRes - 1);

            int bestLayer = 0;
            float bestWeight = 0f;
            for (int l = 0; l < layerCount; l++)
            {
                float w = alphamaps[ay, ax, l];
                if (w > bestWeight) { bestWeight = w; bestLayer = l; }
            }
            faceLayer[y, x] = bestLayer;
        }

        for (int layer = 0; layer < layerCount; layer++)
        {
            string layerName = layer < layers.Length ? layers[layer].name : $"Layer_{layer}";
            List<int> tris = new List<int>();

            for (int y = 0; y < resolution - 1; y++)
            for (int x = 0; x < resolution - 1; x++)
            {
                if (faceLayer[y, x] != layer) continue;

                int idx = y * resolution + x;
                tris.Add(idx);
                tris.Add(idx + resolution);
                tris.Add(idx + 1);
                tris.Add(idx + 1);
                tris.Add(idx + resolution);
                tris.Add(idx + resolution + 1);
            }

            if (tris.Count == 0)
            {
                Debug.Log($"  {layerName}: no faces (skipped)");
                continue;
            }

            // Remap vertices to only include used ones
            HashSet<int> usedSet = new HashSet<int>(tris);
            List<int> usedList = new List<int>(usedSet);
            usedList.Sort();
            Dictionary<int, int> remap = new Dictionary<int, int>();
            for (int i = 0; i < usedList.Count; i++)
                remap[usedList[i]] = i;

            // Export OBJ
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"# {layerName} - exported from Unity Terrain");
            sb.AppendLine($"# Vertices: {usedList.Count}, Faces: {tris.Count / 3}");

            foreach (int vi in usedList)
            {
                Vector3 p = allVerts[vi];
                sb.AppendLine($"v {p.x} {p.y} {p.z}");
            }
            foreach (int vi in usedList)
            {
                Vector2 uv = allUVs[vi];
                sb.AppendLine($"vt {uv.x} {uv.y}");
            }

            for (int t = 0; t < tris.Count; t += 3)
            {
                int a = remap[tris[t]] + 1;
                int b = remap[tris[t + 1]] + 1;
                int c = remap[tris[t + 2]] + 1;
                sb.AppendLine($"f {a}/{a} {b}/{b} {c}/{c}");
            }

            string path = Path.Combine(exportDir, $"{layerName}.obj");
            File.WriteAllText(path, sb.ToString());
            Debug.Log($"  {layerName}: {usedList.Count} verts, {tris.Count / 3} faces → {path}");
        }

        AssetDatabase.Refresh();
        Debug.Log($"Export 완료! 경로: {exportDir}");
        EditorUtility.RevealInFinder(exportDir);
    }
}
