using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections.Generic;

namespace kmty.geom.csg {
    using d3 = double3;

    public static class CSG {

        public static CsgTree GenCsgTree(Transform t, bool isFragmentized = false) {
            var mf = t.GetComponent<MeshFilter>();
            var vs = new List<Vector3>();
            var ns = new List<Vector3>();
            if (isFragmentized) {
                mf.sharedMesh.GetVertices(vs);
                mf.sharedMesh.GetNormals(ns);
            } else {
                Fragmentize(mf.sharedMesh, out vs, out ns);
            }
            Assert.IsTrue(vs.Count % 3 == 0);

            var verts = new (d3 pos, d3 nrm)[vs.Count];
            var polys = new Polygon[vs.Count / 3];
            for (var i = 0; i < verts.Length; i++) 
                verts[i] = (
                    (float3)t.TransformPoint(vs[i]),
                    (float3)t.TransformDirection(ns[i])
                    );

            for (var i = 0; i < polys.Length; i++) {
                var a = verts[i * 3 + 0];
                var b = verts[i * 3 + 1];
                var c = verts[i * 3 + 2];
                var crs = math.cross(b.pos - a.pos, c.pos - a.pos);
                if (math.dot(crs, a.nrm) > 0) {
                    polys[i] = new Polygon(new d3[] { a.pos, b.pos, c.pos });
                } else {
                    polys[i] = new Polygon(new d3[] { a.pos, c.pos, b.pos });
                    Debug.LogWarning("vertices order is clockwise");
                }
            }
            return new CsgTree(polys);
        }

        public static Mesh Meshing(CsgTree tree){
            var vs = new List<Vector3>();
            var ns = new List<Vector3>();
            var ts = new List<int>();
            var dst = new Mesh();
            dst.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            var count = 0;
            for (var j = 0; j < tree.polygons.Length; j++) {
                var p = tree.polygons[j];
                for (var i = 3; i <= p.verts.Length; i++) {
                    var n = (float3)p.plane.n;
                    vs.Add((float3)p.verts[0]);
                    vs.Add((float3)p.verts[i - 2]);
                    vs.Add((float3)p.verts[i - 1]);
                    ns.Add(n);
                    ns.Add(n);
                    ns.Add(n);
                    ts.Add(count++);
                    ts.Add(count++);
                    ts.Add(count++);
                }
            }
            dst.SetVertices(vs);
            dst.SetNormals(ns);
            dst.SetTriangles(ts, 0);
            dst.RecalculateNormals();
            dst.RecalculateBounds();
            return dst;
        }

        static Mesh Fragmentize(Mesh src, out List<Vector3> out_vrts, out List<Vector3> out_nrms) {
            var vrts = src.vertices;
            var tris = src.triangles;
            var nrms = src.normals;
            var new_vrts = new Vector3[tris.Length];
            var new_nrms = new Vector3[tris.Length];
            var new_tris = new int[tris.Length];
            var dst = GameObject.Instantiate(src);
            dst.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            for (int i = 0; i < tris.Length; i++) {
                var t = tris[i];
                new_vrts[i] = vrts[t];
                new_nrms[i] = nrms[t];
                new_tris[i] = i;
            }

            dst.vertices = new_vrts;
            dst.triangles = new_tris;
            dst.RecalculateNormals();
            out_vrts = new_vrts.ToList();
            out_nrms = new_nrms.ToList();
            return dst;
        }
    }
}