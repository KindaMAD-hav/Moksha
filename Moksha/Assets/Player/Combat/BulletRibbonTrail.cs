using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class BulletRibbonTrail : MonoBehaviour
{
    [Header("Trail Shape")]
    public float width = 0.15f;
    public float lifetime = 0.12f;
    public int maxPoints = 20;

    [Header("Fade")]
    public AnimationCurve widthOverLife =
        new AnimationCurve(
            new Keyframe(0, 1),
            new Keyframe(1, 0)
        );

    struct TrailPoint
    {
        public Vector3 pos;
        public float time;
    }

    List<TrailPoint> points = new();
    Mesh mesh;
    Transform target;

    void Awake()
    {
        mesh = new Mesh();
        mesh.name = "BulletRibbonTrail";
        GetComponent<MeshFilter>().mesh = mesh;

        target = transform;
    }

    void LateUpdate()
    {
        AddPoint();
        RebuildMesh();
        CullOldPoints();
    }

    void AddPoint()
    {
        points.Insert(0, new TrailPoint
        {
            pos = target.position,
            time = Time.time
        });

        if (points.Count > maxPoints)
            points.RemoveAt(points.Count - 1);
    }

    void CullOldPoints()
    {
        float now = Time.time;
        points.RemoveAll(p => now - p.time > lifetime);
    }

    void RebuildMesh()
    {
        if (points.Count < 2)
        {
            mesh.Clear();
            return;
        }

        int vCount = points.Count * 2;
        Vector3[] verts = new Vector3[vCount];
        int[] tris = new int[(points.Count - 1) * 6];
        Color[] colors = new Color[vCount];

        Camera cam = Camera.main;
        if (!cam) return;

        // Camera forward converted to LOCAL space
        Vector3 camForwardLocal =
            transform.InverseTransformDirection(cam.transform.forward);

        for (int i = 0; i < points.Count; i++)
        {
            float t = i / (float)(points.Count - 1);
            float w = width * widthOverLife.Evaluate(t);

            // Direction in WORLD space
            Vector3 dirWorld =
                (i == points.Count - 1)
                    ? (points[i - 1].pos - points[i].pos)
                    : (points[i].pos - points[i + 1].pos);

            if (dirWorld.sqrMagnitude < 0.0001f)
                dirWorld = Vector3.forward;

            dirWorld.Normalize();

            // Convert world position to LOCAL space
            Vector3 localPos =
                transform.InverseTransformPoint(points[i].pos);

            // Side vector in LOCAL space
            Vector3 side =
                Vector3.Cross(dirWorld, camForwardLocal).normalized;

            verts[i * 2] = localPos + side * w;
            verts[i * 2 + 1] = localPos - side * w;

            float alpha = 1f - t;
            colors[i * 2] = new Color(1f, 1f, 1f, alpha);
            colors[i * 2 + 1] = new Color(1f, 1f, 1f, alpha);

            if (i < points.Count - 1)
            {
                int ti = i * 6;
                int vi = i * 2;

                tris[ti] = vi;
                tris[ti + 1] = vi + 2;
                tris[ti + 2] = vi + 1;

                tris[ti + 3] = vi + 1;
                tris[ti + 4] = vi + 2;
                tris[ti + 5] = vi + 3;
            }
        }

        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.colors = colors;
        mesh.RecalculateBounds();
    }

}
