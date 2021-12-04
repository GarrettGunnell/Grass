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

        initializeGrassShader.SetInt("_Dimension", resolution);
        initializeGrassShader.SetBuffer(0, "_GrassPositionsBuffer", grassPositionsBuffer);
        initializeGrassShader.Dispatch(0, Mathf.CeilToInt(resolution / 8.0f), Mathf.CeilToInt(resolution / 8.0f), 1);
        grassMaterial.SetBuffer("positionBuffer", grassPositionsBuffer);
        /*
        Vector3[] positions = new Vector3[resolution * resolution];

        grassPositionsBuffer.GetData(positions);

        foreach (Vector3 pos in positions) {
            Debug.Log($"{pos.x}, {pos.y}, {pos.z}");
        }
        */
    }

    void Update() {
        grassMaterial.SetBuffer("positionBuffer", grassPositionsBuffer);
        grassMaterial.SetFloat("_Rotation", 0.0f);
        Graphics.DrawMeshInstancedProcedural(grassMesh, 0, grassMaterial, new Bounds(Vector3.zero, new Vector3(-500.0f, 200.0f, 500.0f)), grassPositionsBuffer.count);
        Material grassMaterial2 = new Material(grassMaterial);
        grassMaterial2.SetBuffer("positionBuffer", grassPositionsBuffer);
        grassMaterial2.SetFloat("_Rotation", 50.0f);
        Graphics.DrawMeshInstancedProcedural(grassMesh, 0, grassMaterial2, new Bounds(Vector3.zero, new Vector3(-500.0f, 200.0f, 500.0f)), grassPositionsBuffer.count);
        Material grassMaterial3 = new Material(grassMaterial);
        grassMaterial3.SetBuffer("positionBuffer", grassPositionsBuffer);
        grassMaterial3.SetFloat("_Rotation", -50.0f);
        Graphics.DrawMeshInstancedProcedural(grassMesh, 0, grassMaterial3, new Bounds(Vector3.zero, new Vector3(-500.0f, 200.0f, 500.0f)), grassPositionsBuffer.count);
    }
    
    void OnDisable() {
        grassPositionsBuffer.Release();
        grassPositionsBuffer = null;
    }
}
