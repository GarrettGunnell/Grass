using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyTerrain : MonoBehaviour {

    private ComputeShader displacePlane;
    
    void Start() {
        displacePlane = Resources.Load<ComputeShader>("DisplacePlane");

        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] verts = mesh.vertices;
        Vector2[] uvs = mesh.uv;

        ComputeBuffer vertBuffer = new ComputeBuffer(verts.Length, 12);
        ComputeBuffer uvBuffer = new ComputeBuffer(uvs.Length, 8);
        vertBuffer.SetData(verts);
        uvBuffer.SetData(uvs);

        Material terrainMat = GetComponent<Renderer>().sharedMaterial;


        displacePlane.SetBuffer(0, "_Vertices", vertBuffer);
        displacePlane.SetBuffer(0, "_UVs", uvBuffer);
        displacePlane.SetTexture(0, "_HeightMap", terrainMat.GetTexture("_HeightMap"));
        displacePlane.SetFloat("_DisplacementStrength", terrainMat.GetFloat("_DisplacementStrength"));
        displacePlane.Dispatch(0, Mathf.CeilToInt(verts.Length / 128.0f), 1, 1);

        vertBuffer.GetData(verts);
        vertBuffer.Release();
        uvBuffer.Release();

        mesh.vertices = verts;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshCollider mc = GetComponent<MeshCollider>();
        mc.sharedMesh = null;
        mc.sharedMesh = mesh;
    }
    
    void Update() {
        
    }
}
