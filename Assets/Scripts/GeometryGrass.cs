using System.Collections;
using System.Collections.Generic;
using static System.Runtime.InteropServices.Marshal;
using UnityEngine;


[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GeometryGrass : MonoBehaviour {
    public int resolution = 100;
    public int scale = 1;
    public float displacementStrength = 200.0f;
    public Material grassMaterial;
    public Texture heightMap;

    private ComputeShader initializeGrassShader;
    private ComputeBuffer grassDataBuffer, grassIndicesBuffer;
    private Vector3[] grassVertices;
    private int[] grassIndices;
    private Bounds bounds;
    private Mesh grassMesh;

    void Start() {
        resolution *= scale;
        initializeGrassShader = Resources.Load<ComputeShader>("GrassGeometryPoint");
        grassDataBuffer = new ComputeBuffer(resolution * resolution, SizeOf(typeof(Vector3)));
        grassIndicesBuffer = new ComputeBuffer(resolution * resolution, SizeOf(typeof(int)));
        grassVertices = new Vector3[resolution * resolution];
        grassIndices = new int[resolution * resolution];
        bounds = new Bounds(Vector3.zero, new Vector3(-resolution, displacementStrength * 2.0f, resolution));

        updateGrassMesh();
        grassDataBuffer.GetData(grassVertices);
        grassIndicesBuffer.GetData(grassIndices);

        grassMesh = new Mesh {name = "Grass"};
        grassMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        grassMesh.vertices = grassVertices;
        grassMesh.SetIndices(grassIndices, MeshTopology.Points, 0);

        GetComponent<MeshFilter>().mesh = grassMesh;
    }

    void updateGrassMesh() {
        initializeGrassShader.SetInt("_Dimension", resolution);
        initializeGrassShader.SetInt("_Scale", scale);
        initializeGrassShader.SetBuffer(0, "_GrassDataBuffer", grassDataBuffer);
        initializeGrassShader.SetBuffer(0, "_GrassIndicesBuffer", grassIndicesBuffer);
        initializeGrassShader.SetTexture(0, "_HeightMap", heightMap);
        initializeGrassShader.SetFloat("_DisplacementStrength", displacementStrength);
        initializeGrassShader.Dispatch(0, Mathf.CeilToInt(resolution / 8.0f), Mathf.CeilToInt(resolution / 8.0f), 1);
    }

    void Update() {

    }
    
    void OnDisable() {
        grassDataBuffer.Release();
        grassDataBuffer = null;
        grassIndicesBuffer.Release();
        grassIndicesBuffer = null;
    }
}
