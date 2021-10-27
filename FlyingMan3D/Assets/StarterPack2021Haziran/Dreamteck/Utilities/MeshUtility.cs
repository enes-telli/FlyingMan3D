using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Dreamteck
{
    public class MeshUtility
    {
        private static Vector3[] tan1 = new Vector3[0];
        private static Vector3[] tan2 = new Vector3[0];

        public static int[] GeneratePlaneTriangles(int x, int z, bool flip, int startTriangleIndex = 0, int startVertex = 0)
        {
            int nbFaces = x * (z - 1);
            int[] triangles = new int[nbFaces * 6];
            GeneratePlaneTriangles(ref triangles, x, z, flip);
            return triangles;
        }

        public static int[] GeneratePlaneTriangles(ref int[] triangles, int x, int z, bool flip, int startTriangleIndex = 0, int startVertex = 0, bool reallocateArray = false)
        {
            int nbFaces = x * (z - 1);
            if (reallocateArray && triangles.Length != nbFaces * 6)
            {
                if(startTriangleIndex > 0)
                {
                    int[] newTris = new int[startTriangleIndex + nbFaces * 6];
                    for(int i = 0; i < startTriangleIndex; i++) newTris[i] = triangles[i];
                    triangles = newTris;
                } else triangles = new int[nbFaces * 6];
            }
            int g = x + 1;
            int t = startTriangleIndex;
            for (int face = 0; face < nbFaces + z - 2; face++)
            {
                if ((float)(face + 1) % (float)g == 0f && face != 0) face++;
                if (flip)
                {
                    triangles[t++] = face + x + 1 + startVertex;
                    triangles[t++] = face + 1 + startVertex;
                    triangles[t++] = face + startVertex;

                    triangles[t++] = face + x + 1 + startVertex;
                    triangles[t++] = face + x + 2 + startVertex;
                    triangles[t++] = face + 1 + startVertex;
                }
                else
                {
                    triangles[t++] = face + startVertex;
                    triangles[t++] = face + 1 + startVertex;
                    triangles[t++] = face + x + 1 + startVertex;

                    triangles[t++] = face + 1 + startVertex;
                    triangles[t++] = face + x + 2 + startVertex;
                    triangles[t++] = face + x + 1 + startVertex;
                }
            } 
            return triangles;
        }

        public static void CalculateTangents(TS_Mesh mesh)
        {
            int triangleCount = mesh.triangles.Length / 3;
            if (mesh.tangents.Length != mesh.vertexCount)  mesh.tangents = new Vector4[mesh.vertexCount];
            if (tan1.Length != mesh.vertexCount)
            {
                tan1 = new Vector3[mesh.vertexCount];
                tan2 = new Vector3[mesh.vertexCount];
            }

            int tri = 0;
            for (int i = 0; i < triangleCount; i++)
            {
                int i1 = mesh.triangles[tri];
                int i2 = mesh.triangles[tri + 1];
                int i3 = mesh.triangles[tri + 2];

                float x1 = mesh.vertices[i2].x - mesh.vertices[i1].x;
                float x2 = mesh.vertices[i3].x - mesh.vertices[i1].x;
                float y1 = mesh.vertices[i2].y - mesh.vertices[i1].y;
                float y2 = mesh.vertices[i3].y - mesh.vertices[i1].y;
                float z1 = mesh.vertices[i2].z - mesh.vertices[i1].z;
                float z2 = mesh.vertices[i3].z - mesh.vertices[i1].z;

                float s1 = mesh.uv[i2].x - mesh.uv[i1].x;
                float s2 = mesh.uv[i3].x - mesh.uv[i1].x;
                float t1 = mesh.uv[i2].y - mesh.uv[i1].y;
                float t2 = mesh.uv[i3].y - mesh.uv[i1].y;

                float div = s1 * t2 - s2 * t1;
                float r = div == 0f ? 0f : 1f / div;

                Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
                Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                tan1[i1] += sdir;
                tan1[i2] += sdir;
                tan1[i3] += sdir;

                tan2[i1] += tdir;
                tan2[i2] += tdir;
                tan2[i3] += tdir;

                tri += 3;
            }

            for (int i = 0; i < mesh.vertexCount; i++)
            {
                Vector3 n = mesh.normals[i];
                Vector3 t = tan1[i];
                Vector3.OrthoNormalize(ref n, ref t);
                mesh.tangents[i].x = t.x;
                mesh.tangents[i].y = t.y;
                mesh.tangents[i].z = t.z;
                mesh.tangents[i].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[i]) < 0.0f) ? -1.0f : 1.0f;
            }
        }

        public static void MakeDoublesided(Mesh input)
        {
            Vector3[] vertices = input.vertices;
            Vector3[] normals = input.normals;
            Vector2[] uvs = input.uv;
            Color[] colors = input.colors;
            int[] triangles = input.triangles;
            List<int[]> submeshes = new List<int[]>();
            for (int i = 0; i < input.subMeshCount; i++) submeshes.Add(input.GetTriangles(i));

            Vector3[] newVertices = new Vector3[vertices.Length * 2];
            Vector3[] newNormals = new Vector3[normals.Length * 2];
            Vector2[] newUvs = new Vector2[uvs.Length * 2];
            Color[] newColors = new Color[colors.Length * 2];
            int[] newTris = new int[triangles.Length * 2];
            List<int[]> newSubmeshes = new List<int[]>();
            for (int i = 0; i < submeshes.Count; i++)
            {
                newSubmeshes.Add(new int[submeshes[i].Length * 2]);
                submeshes[i].CopyTo(newSubmeshes[i], 0);
            }

            for (int i = 0; i < vertices.Length; i++)
            {
                newVertices[i] = vertices[i];
                newNormals[i] = normals[i];
                newUvs[i] = uvs[i];
                if (colors.Length > i) newColors[i] = colors[i];

                newVertices[i + vertices.Length] = vertices[i];
                newNormals[i + vertices.Length] = -normals[i];
                newUvs[i + vertices.Length] = uvs[i];
                if (colors.Length > i) newColors[i + vertices.Length] = colors[i];
            }

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int index1 = triangles[i];
                int index2 = triangles[i + 1];
                int index3 = triangles[i + 2];
                newTris[i] = index1;
                newTris[i + 1] = index2;
                newTris[i + 2] = index3;

                newTris[i + triangles.Length] = index3 + vertices.Length;
                newTris[i + triangles.Length + 1] = index2 + vertices.Length;
                newTris[i + triangles.Length + 2] = index1 + vertices.Length;
            }

            for (int i = 0; i < submeshes.Count; i++)
            {
                for (int n = 0; n < submeshes[i].Length; n += 3)
                {
                    int index1 = submeshes[i][n];
                    int index2 = submeshes[i][n + 1];
                    int index3 = submeshes[i][n + 2];
                    newSubmeshes[i][n] = index1;
                    newSubmeshes[i][n + 1] = index2;
                    newSubmeshes[i][n + 2] = index3;

                    newSubmeshes[i][n + submeshes[i].Length] = index3 + vertices.Length;
                    newSubmeshes[i][n + submeshes[i].Length + 1] = index2 + vertices.Length;
                    newSubmeshes[i][n + submeshes[i].Length + 2] = index1 + vertices.Length;
                }
            }

            input.vertices = newVertices;
            input.normals = newNormals;
            input.uv = newUvs;
            input.colors = newColors;
            input.triangles = newTris;
            for (int i = 0; i < newSubmeshes.Count; i++) input.SetTriangles(newSubmeshes[i], i);
        }

        public static void MakeDoublesided(TS_Mesh input)
        {
            Vector3[] vertices = input.vertices;
            Vector3[] normals = input.normals;
            Vector2[] uvs = input.uv;
            Color[] colors = input.colors;
            int[] triangles = input.triangles;
            List<int[]> submeshes = input.subMeshes;

            Vector3[] newVertices = new Vector3[vertices.Length * 2];
            Vector3[] newNormals = new Vector3[normals.Length * 2];
            Vector2[] newUvs = new Vector2[uvs.Length * 2];
            Color[] newColors = new Color[colors.Length * 2];
            int[] newTris = new int[triangles.Length * 2];
            List<int[]> newSubmeshes = new List<int[]>();
            for(int i = 0; i < submeshes.Count; i++)
            {
                newSubmeshes.Add(new int[submeshes[i].Length * 2]);
                submeshes[i].CopyTo(newSubmeshes[i], 0);
            }

            for (int i = 0; i < vertices.Length; i++)
            {
                newVertices[i] = vertices[i];
                newNormals[i] = normals[i];
                newUvs[i] = uvs[i];
                if(colors.Length > i) newColors[i] = colors[i];

                newVertices[i + vertices.Length] = vertices[i];
                newNormals[i + vertices.Length] = -normals[i];
                newUvs[i + vertices.Length] = uvs[i];
                if (colors.Length > i) newColors[i + vertices.Length] = colors[i];
            }

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int index1 = triangles[i];
                int index2 = triangles[i + 1];
                int index3 = triangles[i + 2];
                newTris[i] = index1;
                newTris[i + 1] = index2;
                newTris[i + 2] = index3;

                newTris[i + triangles.Length] = index3 + vertices.Length;
                newTris[i + triangles.Length + 1] = index2 + vertices.Length;
                newTris[i + triangles.Length + 2] = index1 + vertices.Length;
            }

            for(int i = 0; i < submeshes.Count; i++)
            {
                for(int n = 0; n < submeshes[i].Length; n+= 3)
                {
                    int index1 = submeshes[i][n];
                    int index2 = submeshes[i][n + 1];
                    int index3 = submeshes[i][n + 2];
                    newSubmeshes[i][n] = index1;
                    newSubmeshes[i][n + 1] = index2;
                    newSubmeshes[i][n + 2] = index3;

                    newSubmeshes[i][n + submeshes[i].Length] = index3 + vertices.Length;
                    newSubmeshes[i][n + submeshes[i].Length + 1] = index2 + vertices.Length;
                    newSubmeshes[i][n + submeshes[i].Length + 2] = index1 + vertices.Length;
                }
            }

            input.vertices = newVertices;
            input.normals = newNormals;
            input.uv = newUvs;
            input.colors = newColors;
            input.triangles = newTris;
            input.subMeshes = newSubmeshes;
        }

        public static void MakeDoublesidedHalf(TS_Mesh input)
        {
            int vertexHalf = input.vertices.Length / 2;
            int trisHalf = input.triangles.Length / 2;
            for (int i = 0; i < vertexHalf; i++)
            {
                input.vertices[i + vertexHalf] = input.vertices[i];
                if (input.normals.Length > i) input.normals[i + vertexHalf] = -input.normals[i];
                if (input.tangents.Length > i) input.tangents[i + vertexHalf] = input.tangents[i];
                if (input.uv.Length > i) input.uv[i + vertexHalf] = input.uv[i];
                if (input.uv2.Length > i) input.uv2[i + vertexHalf] = input.uv2[i];
                if (input.uv3.Length > i) input.uv3[i + vertexHalf] = input.uv3[i];
                if (input.uv4.Length > i) input.uv4[i + vertexHalf] = input.uv4[i];
                if (input.colors.Length > i) input.colors[i + vertexHalf] = input.colors[i];
            }

            for (int i = 0; i < trisHalf; i += 3)
            {
                input.triangles[i + trisHalf + 2] = input.triangles[i] + vertexHalf;
                input.triangles[i + trisHalf + 1] = input.triangles[i + 1] + vertexHalf;
                input.triangles[i + trisHalf] = input.triangles[i + 2] + vertexHalf;
            }

            for (int i = 0; i < input.subMeshes.Count; i++)
            {
                trisHalf = input.subMeshes[i].Length / 2;
                for (int n = 0; n < trisHalf; n += 3)
                {
                    input.subMeshes[i][n + trisHalf + 2] = input.subMeshes[i][n] + vertexHalf;
                    input.subMeshes[i][n + trisHalf + 1] = input.subMeshes[i][n + 1] + vertexHalf;
                    input.subMeshes[i][n + trisHalf] = input.subMeshes[i][n + 2] + vertexHalf;
                }
            }
        }

        public static void InverseTransformMesh(TS_Mesh input, TS_Transform transform)
        {
            if (input.vertices == null || input.normals == null) return;
            for (int i = 0; i < input.vertices.Length; i++)
            {
                input.vertices[i] = transform.InverseTransformPoint(input.vertices[i]);
                input.normals[i] = transform.InverseTransformDirection(input.normals[i]);
            }
        }

        public static void TransformMesh(TS_Mesh input, TS_Transform transform)
        {
            if (input.vertices == null || input.normals == null) return;
            for (int i = 0; i < input.vertices.Length; i++)
            {
                input.vertices[i] = transform.TransformPoint(input.vertices[i]);
                input.normals[i] = transform.TransformDirection(input.normals[i]);
            }
        }

        public static void InverseTransformMesh(TS_Mesh input, Transform transform)
        {
            if (input.vertices == null || input.normals == null) return;
            for (int i = 0; i < input.vertices.Length; i++)
            {
                input.vertices[i] = transform.InverseTransformPoint(input.vertices[i]);
                input.normals[i] = transform.InverseTransformDirection(input.normals[i]);
            }
        }

        public static void TransformMesh(TS_Mesh input, Transform transform)
        {
            if (input.vertices == null || input.normals == null) return;
            for (int i = 0; i < input.vertices.Length; i++)
            {
                input.vertices[i] = transform.TransformPoint(input.vertices[i]);
                input.normals[i] = transform.TransformDirection(input.normals[i]);
            }
        }

        public static void InverseTransformMesh(Mesh input, Transform transform)
        {
            Vector3[] vertices = input.vertices;
            Vector3[] normals = input.vertices;
            Matrix4x4 matrix = transform.worldToLocalMatrix;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = matrix.MultiplyPoint3x4(vertices[i]);
                normals[i] = matrix.MultiplyVector(normals[i]);
            }
            input.vertices = vertices;
            input.normals = normals;
        }

        public static void TransformMesh(Mesh input, Transform transform)
        {
            Vector3[] vertices = input.vertices;
            Vector3[] normals = input.vertices;
            Matrix4x4 matrix = transform.localToWorldMatrix;
            if (input.vertices == null || input.normals == null) return;
            for (int i = 0; i < input.vertices.Length; i++)
            {
                vertices[i] = matrix.MultiplyPoint3x4(vertices[i]);
                normals[i] = matrix.MultiplyVector(normals[i]);
            }
            input.vertices = vertices;
            input.normals = normals;
        }


        public static void TransformVertices(Vector3[] vertices, Transform transform)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = transform.TransformPoint(vertices[i]);
            }
        }

        public static void InverseTransformVertices(Vector3[] vertices, Transform transform)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = transform.InverseTransformPoint(vertices[i]);
            }
        }

        public static void TransformNormals(Vector3[] normals, Transform transform)
        {
            for (int i = 0; i < normals.Length; i++)
            {
                normals[i] = transform.TransformDirection(normals[i]);
            }
        }

        public static void InverseTransformNormals(Vector3[] normals, Transform transform)
        {
            for (int i = 0; i < normals.Length; i++)
            {
                normals[i] = transform.InverseTransformDirection(normals[i]);
            }
        }

        public static string ToOBJString(Mesh mesh, Material[] materials)
        {
            int numVertices = 0;
            if (mesh == null)
            {
                return "####Error####";
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("g " + mesh.name +"\n");
            foreach (Vector3 v in mesh.vertices)
            {
                numVertices++;
                sb.Append(string.Format("v {0} {1} {2}\n", -v.x, v.y, v.z));
            }
            sb.Append("\n");
            foreach (Vector3 n in mesh.normals)
            {
                sb.Append(string.Format("vn {0} {1} {2}\n", -n.x, n.y, n.z));
            }
            sb.Append("\n");
            foreach (Vector3 v in mesh.uv)
            {
                sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));
            }
            sb.Append("\n");
            foreach (Vector2 v in mesh.uv2)
            {
                sb.Append(string.Format("vt2 {0} {1}\n", v.x, v.y));
            }
            sb.Append("\n");
            foreach (Vector2 v in mesh.uv3)
            {
                sb.Append(string.Format("vt2 {0} {1}\n", v.x, v.y));
            }
            sb.Append("\n");
            foreach (Color c in mesh.colors)
            {
                sb.Append(string.Format("vc {0} {1} {2} {3}\n", c.r, c.g, c.b, c.a));
            }
            for (int material = 0; material < mesh.subMeshCount; material++)
            {
                sb.Append("\n");
                sb.Append("usemtl ").Append(materials[material].name).Append("\n");
                sb.Append("usemap ").Append(materials[material].name).Append("\n");

                int[] triangles = mesh.GetTriangles(material);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    sb.Append(string.Format("f {2}/{2}/{2} {1}/{1}/{1} {0}/{0}/{0}\n",
                        triangles[i] + 1, triangles[i + 1] + 1, triangles[i + 2] + 1));
                }
            }
            return sb.ToString().Replace(',', '.');
        }

        public static Mesh Copy(Mesh input)
        {
            Mesh copy = new Mesh();
            copy.name = input.name;
            copy.vertices = input.vertices;
            copy.normals = input.normals;
            copy.colors = input.colors;
            copy.uv = input.uv;
            copy.uv2 = input.uv2;
            copy.uv3 = input.uv3;
            copy.uv4 = input.uv4;
            copy.tangents = input.tangents;
            copy.boneWeights = input.boneWeights;
            copy.bindposes = input.bindposes;
            copy.triangles = input.triangles;
            copy.subMeshCount = input.subMeshCount;
            for (int i = 0; i < input.subMeshCount; i++)
            {
                copy.SetTriangles(input.GetTriangles(i), i);
            }
            return copy;
        }

        public static void Triangulate(Vector2[] points, ref int[] output)
        {
            List<int> indices = new List<int>();
            int pointsLength = points.Length;
            if (pointsLength < 3)
            {
                output = new int[0];
                return;
            }

            int[] V = new int[pointsLength];
            if (Area(points, pointsLength) > 0)
            {
                for (int v = 0; v < pointsLength; v++)
                    V[v] = v;
            }
            else
            {
                for (int v = 0; v < pointsLength; v++)
                    V[v] = (pointsLength - 1) - v;
            }

            int nv = pointsLength;
            int count = 2 * nv;
            for (int m = 0, v = nv - 1; nv > 2;)
            {
                if ((count--) <= 0)
                {
                    if (output.Length != indices.Count) output = new int[indices.Count];
                    indices.CopyTo(output, 0);
                    return;
                }

                int u = v;
                if (nv <= u)
                    u = 0;
                v = u + 1;
                if (nv <= v)
                    v = 0;
                int w = v + 1;
                if (nv <= w)
                    w = 0;

                if (Snip(points, u, v, w, nv, V))
                {
                    int a, b, c, s, t;
                    a = V[u];
                    b = V[v];
                    c = V[w];
                    indices.Add(c);
                    indices.Add(b);
                    indices.Add(a);
                    m++;
                    for (s = v, t = v + 1; t < nv; s++, t++)
                        V[s] = V[t];
                    nv--;
                    count = 2 * nv;
                }
            }

            indices.Reverse();
            if (output.Length != indices.Count) output = new int[indices.Count];
            indices.CopyTo(output, 0);
        }

        public static void FlipTriangles(ref int[] triangles)
        {
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int temp = triangles[i];
                triangles[i] = triangles[i + 2];
                triangles[i + 2] = temp;
            }
        }

        public static void FlipFaces(TS_Mesh input)
        {
            for (int i = 0; i < input.subMeshes.Count; i++)
            {
                int[] array = input.subMeshes[i];
                FlipTriangles(ref array);
            }
            FlipTriangles(ref input.triangles);
            for (int i = 0; i < input.normals.Length; i++)
            {
                input.normals[i] *= -1f;
            }
        }

        public static void BreakMesh(Mesh input, bool keepNormals = true)
        {
            Vector3[] newVertices = new Vector3[input.triangles.Length];
            Vector3[] newNormals = new Vector3[newVertices.Length];
            Vector2[] newUVs = new Vector2[newVertices.Length];
            Vector4[] newTangents = new Vector4[newVertices.Length];
            Color[] newColors = new Color[newVertices.Length];
            BoneWeight[] newBoneWeights = new BoneWeight[newVertices.Length];

            Vector3[] oldVertices = input.vertices;
            Vector2[] oldUvs = input.uv;
            Vector3[] oldNormals = input.normals;
            Vector4[] oldTangents = input.tangents;
            Color[] oldColors = input.colors;
            BoneWeight[] oldBoneWeights = input.boneWeights;

            if (oldColors.Length != oldVertices.Length)
            {
                oldColors = new Color[oldVertices.Length];
                for (int i = 0; i < oldColors.Length; i++) oldColors[i] = Color.white;
            }

            List<int[]> submeshList = new List<int[]>();
            int submeshes = input.subMeshCount;
            int vertIndex = 0;
            for (int i = 0; i < submeshes; i++)
            {
                int[] submesh = input.GetTriangles(i);
                for (int n = 0; n < submesh.Length; n += 3)
                {
                    newVertices[vertIndex] = oldVertices[submesh[n]];
                    newVertices[vertIndex + 1] = oldVertices[submesh[n + 1]];
                    newVertices[vertIndex + 2] = oldVertices[submesh[n + 2]];

                    if (oldNormals.Length > submesh[n + 2])
                    {
                        if (!keepNormals)
                        {
                            newNormals[vertIndex] = newNormals[vertIndex + 1] = newNormals[vertIndex + 2] = (oldNormals[submesh[n]] + oldNormals[submesh[n + 1]] + oldNormals[submesh[n + 2]]).normalized;
                        }
                        else
                        {
                            newNormals[vertIndex] = oldNormals[submesh[n]];
                            newNormals[vertIndex + 1] = oldNormals[submesh[n + 1]];
                            newNormals[vertIndex + 2] = oldNormals[submesh[n + 2]];
                        }

                    }

                    if (oldColors.Length > submesh[n + 2])
                        newColors[vertIndex] = newColors[vertIndex + 1] = newColors[vertIndex + 2] = (oldColors[submesh[n]] + oldColors[submesh[n + 1]] + oldColors[submesh[n + 2]]) / 3f;

                    if (oldUvs.Length > submesh[n + 2])
                    {
                        newUVs[vertIndex] = oldUvs[submesh[n]];
                        newUVs[vertIndex + 1] = oldUvs[submesh[n + 1]];
                        newUVs[vertIndex + 2] = oldUvs[submesh[n + 2]];
                    }

                    if (oldTangents.Length > submesh[n + 2])
                    {
                        newTangents[vertIndex] = oldTangents[submesh[n]];
                        newTangents[vertIndex + 1] = oldTangents[submesh[n + 1]];
                        newTangents[vertIndex + 2] = oldTangents[submesh[n + 2]];
                    }

                    if (oldBoneWeights.Length > submesh[n + 2])
                    {
                        newBoneWeights[vertIndex] = oldBoneWeights[submesh[n]];
                        newBoneWeights[vertIndex + 1] = oldBoneWeights[submesh[n + 1]];
                        newBoneWeights[vertIndex + 2] = oldBoneWeights[submesh[n + 2]];
                    }

                    submesh[n] = vertIndex;
                    submesh[n + 1] = vertIndex + 1;
                    submesh[n + 2] = vertIndex + 2;
                    vertIndex += 3;
                }
                submeshList.Add(submesh);
            }

            input.vertices = newVertices;
            input.normals = newNormals;
            input.colors = newColors;
            input.uv = newUVs;
            input.tangents = newTangents;
            input.subMeshCount = submeshList.Count;
            input.boneWeights = newBoneWeights;
            for (int i = 0; i < submeshList.Count; i++)
            {
                input.SetTriangles(submeshList[i], i);
            }
        }

        private static float Area(Vector2[] points, int maxCount)
        {
            float A = 0.0f;
            for (int p = maxCount - 1, q = 0; q < maxCount; p = q++)
            {
                Vector2 pval = points[p];
                Vector2 qval = points[q];
                A += pval.x * qval.y - qval.x * pval.y;
            }
            return (A * 0.5f);
        }

        private static bool Snip(Vector2[] points, int u, int v, int w, int n, int[] V)
        {
            int p;
            Vector2 A = points[V[u]];
            Vector2 B = points[V[v]];
            Vector2 C = points[V[w]];
            if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x))))
                return false;
            for (p = 0; p < n; p++)
            {
                if ((p == u) || (p == v) || (p == w))
                    continue;
                Vector2 P = points[V[p]];
                if (InsideTriangle(A, B, C, P))
                    return false;
            }
            return true;
        }

        private static bool InsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
        {
            float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy;
            float cCROSSap, bCROSScp, aCROSSbp;

            ax = C.x - B.x; ay = C.y - B.y;
            bx = A.x - C.x; by = A.y - C.y;
            cx = B.x - A.x; cy = B.y - A.y;
            apx = P.x - A.x; apy = P.y - A.y;
            bpx = P.x - B.x; bpy = P.y - B.y;
            cpx = P.x - C.x; cpy = P.y - C.y;

            aCROSSbp = ax * bpy - ay * bpx;
            cCROSSap = cx * apy - cy * apx;
            bCROSScp = bx * cpy - by * cpx;

            return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
        }

    }

}
