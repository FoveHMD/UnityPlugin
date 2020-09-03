using Fove.Unity;
using UnityEngine;

public class GenerateMeshes : MonoBehaviour {
    public int count = 100;

    public float amplitude = 5;

    public int polyCount = 10000;

    public Material material;

    // Use this for initialization
    void Start () {
        var renderMesh = new Mesh();
        renderMesh.vertices = new[]
        {
            0.5f * new Vector3(-1, 1, 0),
            0.5f * new Vector3(1, 1, 0),
            0.5f * new Vector3(-1, -1, 0),
            0.5f * new Vector3(1, -1, 0),
        };
        renderMesh.triangles = new[] { 0, 1, 2, 1, 3, 2 };
        renderMesh.RecalculateBounds();
        renderMesh.RecalculateNormals();

        var sqrtCount = Mathf.CeilToInt(Mathf.Sqrt(polyCount));
        var step = 1f / (sqrtCount-1);

        var colVertices = new Vector3[sqrtCount * sqrtCount];
        for (int i=0; i<sqrtCount; ++i)
        {
            for (int j=0; j<sqrtCount; ++j)
            {
                colVertices[i * sqrtCount + j].x = j * step - 0.5f;
                colVertices[i * sqrtCount + j].y = i * step - 0.5f;
                colVertices[i * sqrtCount + j].z = 0;
            }
        }

        var colTriangles = new int[2 * 3 * (sqrtCount - 1) * (sqrtCount - 1)];
        for (int i = 0; i < sqrtCount-1; ++i)
        {
            for (int j = 0; j < sqrtCount-1; ++j)
            {
                colTriangles[6 * (i * (sqrtCount - 1) + j) + 0] = (i + 0) * sqrtCount + (j + 0);
                colTriangles[6 * (i * (sqrtCount - 1) + j) + 1] = (i + 0) * sqrtCount + (j + 1);
                colTriangles[6 * (i * (sqrtCount - 1) + j) + 2] = (i + 1) * sqrtCount + (j + 0);
                colTriangles[6 * (i * (sqrtCount - 1) + j) + 3] = (i + 0) * sqrtCount + (j + 1);
                colTriangles[6 * (i * (sqrtCount - 1) + j) + 4] = (i + 1) * sqrtCount + (j + 1);
                colTriangles[6 * (i * (sqrtCount - 1) + j) + 5] = (i + 1) * sqrtCount + (j + 0);
            }
        }
        var colMesh = new Mesh();
        colMesh.vertices = colVertices;
        colMesh.triangles = colTriangles;
        colMesh.RecalculateBounds();
        colMesh.RecalculateNormals();

        var sqrt = Mathf.CeilToInt(Mathf.Sqrt(count));
        for (int i=0; i<sqrt; ++i)
        {
            for (int j=0; j<sqrt; ++j)
            {
                var go = new GameObject("mesh obj " + (i * sqrt + j));
                go.transform.parent = transform;
                go.transform.position = amplitude * (new Vector3(i, j, 2) - sqrt / 2 * new Vector3(1, 1, 0));
                go.AddComponent<MeshRenderer>().material = material;
                go.AddComponent<MeshFilter>().mesh = renderMesh;
                go.AddComponent<MeshCollider>().sharedMesh = colMesh;
                go.AddComponent<GazeHighlight>();
                go.AddComponent<GazableObject>();
            }
        }
    }
}
