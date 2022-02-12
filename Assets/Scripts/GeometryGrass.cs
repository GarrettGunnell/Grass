using System.Collections;
using System.Collections.Generic;
using static System.Runtime.InteropServices.Marshal;
using UnityEngine;

public class GeometryGrass : MonoBehaviour {
    public int resolution = 100;
    public int scale = 1;
    public float displacementStrength = 200.0f;
    public Material grassMaterial;
    public Texture heightMap;

    private ComputeShader initializeGrassShader;
    private ComputeBuffer grassDataBuffer;
    private Bounds bounds;

    private struct GrassData {
        public Vector4 position;
    }

    void Start() {
        resolution *= scale;
        initializeGrassShader = Resources.Load<ComputeShader>("GrassGeometryPoint");
        grassDataBuffer = new ComputeBuffer(resolution * resolution, SizeOf(typeof(GrassData)));
        bounds = new Bounds(Vector3.zero, new Vector3(-resolution,displacementStrength * 2.0f, resolution));

        updateGrassBuffer();
    }

    void updateGrassBuffer() {
        initializeGrassShader.SetInt("_Dimension", resolution);
        initializeGrassShader.SetInt("_Scale", scale);
        initializeGrassShader.SetBuffer(0, "_GrassDataBuffer", grassDataBuffer);
        initializeGrassShader.SetTexture(0, "_HeightMap", heightMap);
        initializeGrassShader.SetFloat("_DisplacementStrength", displacementStrength);
        initializeGrassShader.Dispatch(0, Mathf.CeilToInt(resolution / 8.0f), Mathf.CeilToInt(resolution / 8.0f), 1);

        grassMaterial.SetBuffer("positionBuffer", grassDataBuffer);
    }

    void Update() {
        Graphics.DrawProcedural(grassMaterial, bounds, MeshTopology.Points, 1, grassDataBuffer.count);
    }
    
    void OnDisable() {
        grassDataBuffer.Release();
        grassDataBuffer = null;
    }
}
