using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SphereMesh : MonoBehaviour
{
    public Material lineMat;

    [Header("Shape Settings")]
    public float radius = 1.2f;
    [Min(6)] public int segments = 12; // Must be > 5 segments
    public float focalLength = 5f;
    public Vector3 position = new Vector3(3.5f, 0f, 5f);

    [Header("Rotation Settings")]
    [Range(0f, 360f)] public float xRotation = 0f;
    [Range(0f, 360f)] public float yRotation = 0f;
    public bool autoRotate = true;
    public float rotationSpeed = 45f;

    private Mesh mesh;

    void Awake()
    {
        mesh = new Mesh { name = "WireframeSphere" };
        GetComponent<MeshFilter>().sharedMesh = mesh;

        var mr = GetComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        if (lineMat != null) mr.sharedMaterial = lineMat;
    }

    void Update()
    {
        if (autoRotate)
        {
            xRotation = (xRotation + rotationSpeed * Time.deltaTime) % 360f;
            yRotation = (yRotation + rotationSpeed * Time.deltaTime) % 360f;
        }
        Rebuild();
    }

    void OnValidate()
    {
        if (segments <= 5) segments = 6;
        if (mesh == null)
        {
            mesh = new Mesh { name = "WireframeSphere" };
            var mf = GetComponent<MeshFilter>();
            if (mf != null) mf.sharedMesh = mesh;
        }
        Rebuild();
    }

    Vector3 RotatePoint(Vector3 point)
    {
        float radX = xRotation * Mathf.Deg2Rad;
        float radY = yRotation * Mathf.Deg2Rad;

        // Rotate around X axis
        float y1 = point.y * Mathf.Cos(radX) - point.z * Mathf.Sin(radX);
        float z1 = point.y * Mathf.Sin(radX) + point.z * Mathf.Cos(radX);
        point = new Vector3(point.x, y1, z1);

        // Rotate around Y axis
        float x2 = point.x * Mathf.Cos(radY) + point.z * Mathf.Sin(radY);
        float z2 = -point.x * Mathf.Sin(radY) + point.z * Mathf.Cos(radY);
        point = new Vector3(x2, point.y, z2);

        return point;
    }

    Vector3 Project(Vector3 p)
    {
        float denom = focalLength + p.z;
        float scale = denom > 0.001f ? focalLength / denom : 1f;
        return new Vector3(p.x * scale, p.y * scale, 0f);
    }

    bool FacesCamera(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 normal = Vector3.Cross(b - a, c - a);
        return normal.z < 0f;
    }

    void Rebuild()
    {
        int segs = Mathf.Max(6, segments);
        int rings = segs;
        int sectors = segs;

        
        int stride = sectors + 1;
        Vector3[] worldVerts = new Vector3[(rings + 1) * stride];

        for (int r = 0; r <= rings; r++)
        {
            float v = (float)r / rings;
            float phi = v * Mathf.PI; 

            float y = Mathf.Cos(phi) * radius;
            float ringRadius = Mathf.Sin(phi) * radius;

            for (int s = 0; s <= sectors; s++)
            {
                float u = (float)s / sectors;
                float theta = u * Mathf.PI * 2f;

                float x = Mathf.Cos(theta) * ringRadius;
                float z = Mathf.Sin(theta) * ringRadius;

                Vector3 localPos = new Vector3(x, y, z);
                worldVerts[r * stride + s] = RotatePoint(localPos) + position;
            }
        }

        var verts = new List<Vector3>();
        var indices = new List<int>();
        var edges = new HashSet<(int, int)>();

        void AddEdge(int a, int b)
        {
            var key = a < b ? (a, b) : (b, a);
            if (!edges.Add(key)) return;

            int idx = verts.Count;
            verts.Add(Project(worldVerts[a]));
            verts.Add(Project(worldVerts[b]));
            indices.Add(idx);
            indices.Add(idx + 1);
        }

        for (int r = 0; r < rings; r++)
        {
            for (int s = 0; s < sectors; s++)
            {
                int i0 = r * stride + s;
                int i1 = r * stride + (s + 1);
                int i2 = (r + 1) * stride + (s + 1);
                int i3 = (r + 1) * stride + s;

                if (r == 0)
                {
                    // top cap triangles 
                    if (!FacesCamera(worldVerts[i0], worldVerts[i2], worldVerts[i3])) continue;
                    AddEdge(i0, i2);
                    AddEdge(i2, i3);
                    AddEdge(i3, i0);
                }
                else if (r == rings - 1)
                {
                    // bottom cap triangles
                    if (!FacesCamera(worldVerts[i0], worldVerts[i1], worldVerts[i2])) continue;
                    AddEdge(i0, i1);
                    AddEdge(i1, i2);
                    AddEdge(i2, i0);
                }
                else
                {
                    // mddle quad bands
                    if (!FacesCamera(worldVerts[i0], worldVerts[i1], worldVerts[i2])) continue;
                    AddEdge(i0, i1);
                    AddEdge(i1, i2);
                    AddEdge(i2, i3);
                    AddEdge(i3, i0);
                }
            }
        }

        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetIndices(indices, MeshTopology.Lines, 0);
        mesh.RecalculateBounds();
    }
}
