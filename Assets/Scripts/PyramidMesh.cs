using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PyramidMesh : MonoBehaviour
{
    public Material lineMat;

    [Header("Shape Settings")]
    public float baseSize = 2f;
    public float height = 2.2f;
    public float focalLength = 5f;
    public Vector3 position = new Vector3(0f, 0f, 5f);

    [Header("Rotation Settings")]
    [Range(0f, 360f)] public float xRotation = 0f;
    [Range(0f, 360f)] public float yRotation = 0f;
    public bool autoRotate = true;
    public float rotationSpeed = 45f;

    private Mesh mesh;

    void Awake()
    {
        mesh = new Mesh { name = "WireframePyramid" };
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
            mesh = new Mesh { name = "WireframePyramid" };
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
        float h = baseSize * 0.5f;
        float halfH = height * 0.5f;

        // 0..3 = Base corners, 4 = Top Apex
        Vector3[] localVerts = new Vector3[5]
        {
            new Vector3(-h, -halfH, -h), // 0 Back-Left
            new Vector3( h, -halfH, -h), // 1 Back-Right
            new Vector3( h, -halfH,  h), // 2 Front-Right
            new Vector3(-h, -halfH,  h), // 3 Front-Left
            new Vector3( 0f, halfH, 0f)  // 4 Apex
        };

        Vector3[] worldVerts = new Vector3[5];
        for (int i = 0; i < 5; i++)
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

        //Triangular side faces 
        int[,] sideFaces = new int[4, 3]
        {
            { 0, 4, 1 }, // Back
            { 1, 4, 2 }, // Right
            { 2, 4, 3 }, // Front
            { 3, 4, 0 }  // Left
        };

        for (int f = 0; f < 4; f++)
        {
            int a = sideFaces[f, 0], b = sideFaces[f, 1], c = sideFaces[f, 2];
            if (!FacesCamera(worldVerts[a], worldVerts[b], worldVerts[c])) continue;

            AddEdge(a, b);
            AddEdge(b, c);
            AddEdge(c, a);
        }

        // sqr base face 
        if (FacesCamera(worldVerts[0], worldVerts[1], worldVerts[2]))
        {
            AddEdge(0, 1);
            AddEdge(1, 2);
            AddEdge(2, 3);
            AddEdge(3, 0);
        }

        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetIndices(indices, MeshTopology.Lines, 0);
        mesh.RecalculateBounds();
    }
}
