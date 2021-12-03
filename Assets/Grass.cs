using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grass : MonoBehaviour {
    public int resolution = 100;
    public Material grassMaterial;
    public Mesh grassMesh;

    private ComputeShader initializeGrassShader;
    private ComputeBuffer grassPositionsBuffer;

    void Start() {
        initializeGrassShader = Resources.Load<ComputeShader>("GrassPoint");
        grassPositionsBuffer = new ComputeBuffer(resolution * resolution, 3 * 4);

        initializeGrassShader.SetFloat("_Dimension", resolution);
        initializeGrassShader.SetBuffer(0, "_GrassPositionsBuffer", grassPositionsBuffer);
        initializeGrassShader.Dispatch(0, Mathf.CeilToInt(resolution / 8.0f), Mathf.CeilToInt(resolution / 8.0f), 1);
        grassMaterial.SetBuffer("positionBuffer", grassPositionsBuffer);
    }

    void Update() {
        grassMaterial.SetBuffer("positionBuffer", grassPositionsBuffer);
        Graphics.DrawMeshInstancedProcedural(grassMesh, 0, grassMaterial, grassMesh.bounds, grassPositionsBuffer.count);
    }
    
    void OnDisable() {
        grassPositionsBuffer.Release();
        grassPositionsBuffer = null;
    }
}
