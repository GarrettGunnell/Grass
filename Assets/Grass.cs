using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grass : MonoBehaviour {
    public int resolution = 100;
    public int scale = 1;
    public Material grassMaterial;
    public Mesh grassMesh;

    private ComputeShader initializeGrassShader;
    private ComputeBuffer grassDataBuffer;

    void Start() {
        resolution *= scale;
        initializeGrassShader = Resources.Load<ComputeShader>("GrassPoint");
        grassDataBuffer = new ComputeBuffer(resolution * resolution, 4 * 4);

        initializeGrassShader.SetInt("_Dimension", resolution);
        initializeGrassShader.SetInt("_Scale", scale);
        initializeGrassShader.SetBuffer(0, "_GrassDataBuffer", grassDataBuffer);
        initializeGrassShader.Dispatch(0, Mathf.CeilToInt(resolution / 8.0f), Mathf.CeilToInt(resolution / 8.0f), 1);
        grassMaterial.SetBuffer("positionBuffer", grassDataBuffer);
        /*
        Vector3[] positions = new Vector3[resolution * resolution];

        grassDataBuffer.GetData(positions);

        foreach (Vector3 pos in positions) {
            Debug.Log($"{pos.x}, {pos.y}, {pos.z}");
        }
        */
    }

    void Update() {
        grassMaterial.SetBuffer("positionBuffer", grassDataBuffer);
        grassMaterial.SetFloat("_Rotation", 0.0f);
        Graphics.DrawMeshInstancedProcedural(grassMesh, 0, grassMaterial, new Bounds(Vector3.zero, new Vector3(-500.0f, 200.0f, 500.0f)), grassDataBuffer.count);
        Material grassMaterial2 = new Material(grassMaterial);
        grassMaterial2.SetBuffer("positionBuffer", grassDataBuffer);
        grassMaterial2.SetFloat("_Rotation", 50.0f);
        Graphics.DrawMeshInstancedProcedural(grassMesh, 0, grassMaterial2, new Bounds(Vector3.zero, new Vector3(-500.0f, 200.0f, 500.0f)), grassDataBuffer.count);
        Material grassMaterial3 = new Material(grassMaterial);
        grassMaterial3.SetBuffer("positionBuffer", grassDataBuffer);
        grassMaterial3.SetFloat("_Rotation", -50.0f);
        Graphics.DrawMeshInstancedProcedural(grassMesh, 0, grassMaterial3, new Bounds(Vector3.zero, new Vector3(-500.0f, 200.0f, 500.0f)), grassDataBuffer.count);
    }
    
    void OnDisable() {
        grassDataBuffer.Release();
        grassDataBuffer = null;
    }
}
