 #define Debug      // Debugging on
 using UnityEngine;
 using System.Collections;
 using System.Collections.Generic;
 using System.IO;
 using System;

 public class ObjLoader
 {
     List<Vector3> m_bufV = new List<Vector3>();
     List<Vector2> m_bufVt = new List<Vector2>();
     List<Vector3> m_bufVn = new List<Vector3>();
     struct FaceV
     {
         public int iv;
         public int ivt;
         public int ivn;
         public string key;
     };
     List<List<FaceV>> m_bufF = new List<List<FaceV>>();

     struct GLVertex
     {
         public Vector3 vert;
         public Vector2 uv;
         public Vector3 norm;
     };

     List<GLVertex> m_bufGLV = new List<GLVertex>();
     IDictionary<string, int> m_dictGLVertex = new Dictionary<string, int>();
     List<int> m_idxTri = new List<int>();

     GLVertex GenerateGLV(FaceV fv)
     {
 #if Debug
         if (!(fv.iv < m_bufV.Count
             && fv.ivt < m_bufVt.Count
             && fv.ivn < m_bufVn.Count
             && fv.iv > -1
             && fv.ivt > -1
             && fv.ivn > -1))
             Debug.LogError("out of range");
 #endif
         GLVertex glv = new GLVertex();
         if (fv.iv < m_bufV.Count)
             glv.vert = m_bufV[fv.iv];
         if (fv.ivt < m_bufVt.Count)
             glv.uv = m_bufVt[fv.ivt];
         if (fv.ivn < m_bufVn.Count)
             glv.norm = m_bufVn[fv.ivn];

         return glv;
     }
     bool Parse4V3(string strLn, ref Vector3 vec)
     {
         string[] subField = strLn.Split();
         int iE = 0;
         int iF = 1;
         for (; iF < subField.Length && iE < 3; iF ++)
         {
             try
              {
                 vec[iE] = System.Convert.ToSingle(subField[iF]);
                 iE ++;
             }
             catch (FormatException)
             {
             }
         }
         if (vec[0] < 0)
             Debug.Log("x is minus");
         return (iE == 3
             && iF == subField.Length);
     }

     bool Parse4V2(string strLn, ref Vector2 vec)
     {
         string[] subField = strLn.Split();
         int iE = 0;
         int iF = 1;
         for (; iF < subField.Length && iE < 2; iF ++)
         {
             try
              {
                 vec[iE] = System.Convert.ToSingle(subField[iF]);
                 iE ++;
             }
             catch (FormatException)
             {
             }
         }
         return (iE == 2
             && iF == subField.Length);
     }


     bool Parse4F(string strLn, ref List<FaceV> face)
     {
         string[] subField = strLn.Split();
         int iF = 1;
         int cnt = 0;
         for (; iF < subField.Length; iF++)
         {
             FaceV fv = new FaceV();
             fv.key = subField[iF];
             string[] ssf = subField[iF].Split('/');
             try
             {
                 fv.iv = System.Convert.ToInt32(ssf[0]) - 1;
                 fv.ivt = System.Convert.ToInt32(ssf[1]) - 1;
                 fv.ivn = System.Convert.ToInt32(ssf[2]) - 1;
             }
             catch (FormatException)
             {
                 continue;
             }
             face.Add(fv);
             cnt ++;
         }

         return cnt == 3
             || cnt == 4;
     }

     public Mesh ImportFile(string filePath)
     {
         StreamReader stream = File.OpenText(filePath);
         string strLine = stream.ReadLine();
         bool parseable = true;
         while (null != strLine
             && parseable)
         {
             if (strLine.StartsWith("v "))
             {
                 Vector3 vec3 = new Vector3();
                 parseable = Parse4V3(strLine,ref vec3);
                 if (parseable)
                     m_bufV.Add(vec3);
             }
             else if (strLine.StartsWith("vt"))
             {
                 Vector2 vec2 = new Vector2();
                 parseable = Parse4V2(strLine, ref vec2);
                 if (parseable)
                     m_bufVt.Add(vec2);
             }
             else if (strLine.StartsWith("vn"))
             {
                 Vector3 vec3 = new Vector3();
                 parseable = Parse4V3(strLine, ref vec3);
                 if (parseable)
                     m_bufVn.Add(vec3);
             }
             else if (strLine.StartsWith("f "))
             {
                 List<FaceV> face = new List<FaceV>();
                 parseable = Parse4F(strLine, ref face);
                 if (parseable)
                     m_bufF.Add(face);
             }
             strLine = stream.ReadLine();
         }
         stream.Close();

         List<int> faceprime = new List<int>();
         for (int iF = 0; iF < m_bufF.Count; iF++)
         {
             List<FaceV> face = m_bufF[iF];
             faceprime.Clear();
             int iFV = 0;
             for (; iFV < face.Count; iFV++)
             {
                 FaceV fv = face[iFV];
                 int iV = -1;
                 if (!m_dictGLVertex.TryGetValue(fv.key, out iV))
                 {
                     iV = m_bufGLV.Count;
                     GLVertex glv = GenerateGLV(fv);
                     m_bufGLV.Add(glv);
                     m_dictGLVertex[fv.key] = iV;
                 }
                 faceprime.Add(iV);
             }

             Debug.Assert(faceprime.Count > 2);
             int iPivot = faceprime[0];
             int cntTri = faceprime.Count - 1;
             for (iFV = 1; iFV < cntTri; iFV++)
             {
                 m_idxTri.Add(iPivot);
                 m_idxTri.Add(faceprime[iFV]);
                 m_idxTri.Add(faceprime[iFV+1]);
             }
         }

         //for (int iTri = 0; iTri < m_idxTri.Count; )
         //{
         //    int i_v1 = m_idxTri[iTri++];
         //    int i_v2 = m_idxTri[iTri++];
         //    int i_v3 = m_idxTri[iTri++];
         //    string strLog = string.Format("Triangle:<{0}>,<{1}>,<{2}>", m_bufGLV[i_v1].vert.ToString(), m_bufGLV[i_v1].uv.ToString(), m_bufGLV[i_v1].norm.ToString());
         //    strLog += string.Format("\n\t<{0}>,<{1}>,<{2}>", m_bufGLV[i_v2].vert.ToString(), m_bufGLV[i_v2].uv.ToString(), m_bufGLV[i_v2].norm.ToString());
         //    strLog += string.Format("\n\t<{0}>,<{1}>,<{2}>", m_bufGLV[i_v3].vert.ToString(), m_bufGLV[i_v3].uv.ToString(), m_bufGLV[i_v3].norm.ToString());
         //    Debug.Log(strLog);
         //}

         Vector3[] Verts = new Vector3[m_bufGLV.Count];
         Vector2[] Uvs = new Vector2[m_bufGLV.Count];
         Vector3[] Norms = new Vector3[m_bufGLV.Count];
         for (int iGLV = 0; iGLV < m_bufGLV.Count; iGLV ++)
         {
             Verts[iGLV] = m_bufGLV[iGLV].vert;
             Uvs[iGLV] = m_bufGLV[iGLV].uv;
             Norms[iGLV] = m_bufGLV[iGLV].norm;
         }
         int[] Tris = new int[m_idxTri.Count];
         for (int iTri = 0; iTri < m_idxTri.Count; iTri++)
         {
             Tris[iTri] = m_idxTri[iTri];
         }

         Mesh mesh = new Mesh();
         mesh.vertices = Verts;
         mesh.uv = Uvs;
         mesh.normals = Norms;
         mesh.triangles = Tris;
         mesh.RecalculateBounds();
         mesh.Optimize();
         return mesh;
     }
 }
