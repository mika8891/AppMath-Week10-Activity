using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CubeMesh : MonoBehaviour
{
    public Material lineMat;

    [Header("Shape Settings")]
    public float size = 2f;
    public float focalLength = 5f;
    public Vector3 position = new Vector3(-3.5f, 0f, 5f);

    [Header("Rotation Settings")]
    [Range(0f, 360f)] public float xRotation = 0f;
    [Range(0f, 360f)] public float yRotation = 0f;
    public bool autoRotate = true;
    public float rotationSpeed = 45f;

    private Mesh mesh;

    private static readonly int[,] Faces = new int[6, 4]
    {
        { 0, 1, 2, 3 }, // Back
        { 5, 4, 7, 6 }, // Front
        { 4, 0, 3, 7 }, // Left
        { 1, 5, 6, 2 }, // Right
        { 3, 2, 6, 7 }, // Top
        { 4, 5, 1, 0 }  // Bottom
    };

    void Awake()
    {
        mesh = new Mesh { name = "WireframeCube" };
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
        if (mesh == null)
        {
            mesh = new Mesh { name = "WireframeCube" };
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
        float h = size * 0.5f;

        Vector3[] localVerts = new Vector3[8]
        {
            new Vector3(-h, -h, -h),
            new Vector3( h, -h, -h),
            new Vector3( h,  h, -h),
            new Vector3(-h,  h, -h),
            new Vector3(-h, -h,  h),
            new Vector3( h, -h,  h),
            new Vector3( h,  h,  h),
            new Vector3(-h,  h,  h)
        };

        Vector3[] worldVerts = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            worldVerts[i] = RotatePoint(localVerts[i]) + position;
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

        for (int f = 0; f < 6; f++)
        {
            int i0 = Faces[f, 0], i1 = Faces[f, 1], i2 = Faces[f, 2], i3 = Faces[f, 3];
            if (!FacesCamera(worldVerts[i0], worldVerts[i1], worldVerts[i2])) continue;

            AddEdge(i0, i1);
            AddEdge(i1, i2);
            AddEdge(i2, i3);
            AddEdge(i3, i0);
        }

        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetIndices(indices, MeshTopology.Lines, 0);
        mesh.RecalculateBounds();
    }
}
