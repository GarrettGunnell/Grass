using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelGrass : MonoBehaviour {
    public int resolution = 100;
    public int scale = 1;
    public float displacementStrength = 200.0f;
    public Material grassMaterial;
    public Mesh grassMesh;
    public Texture heightMap;

    public bool updateGrass;

    [Header("Wind")]
    public float windSpeed = 1.0f;
    public float frequency = 1.0f;
    public float windStrength = 1.0f;

    private ComputeShader initializeGrassShader, generateWindShader, cullGrassShader;
    private ComputeBuffer grassDataBuffer, grassVoteBuffer, grassScanBuffer, groupSumArrayBuffer, scannedGroupSumBuffer, culledGrassOutputBuffer, argsBuffer;

    private ComputeBuffer compactedGrassIndicesBuffer;

    private RenderTexture wind;

    private int numInstances, numGroups;

    private struct GrassData {
        public Vector4 position;
        public Vector2 uv;
        public float displacement;
    }

    void OnEnable() {
        numInstances = resolution * scale;
        numInstances *= numInstances;
        Debug.Log("NumInstances: " + numInstances.ToString());

        numGroups = numInstances / 128;
        Debug.Log("NumGroups: " + numGroups.ToString());

        initializeGrassShader = Resources.Load<ComputeShader>("GrassPoint");
        generateWindShader = Resources.Load<ComputeShader>("WindNoise");
        cullGrassShader = Resources.Load<ComputeShader>("CullGrass");

        grassDataBuffer = new ComputeBuffer(numInstances, 4 * 7);
        grassVoteBuffer = new ComputeBuffer(numInstances, 4);
        grassScanBuffer = new ComputeBuffer(numInstances, 4);
        groupSumArrayBuffer = new ComputeBuffer(numGroups, 4);
        scannedGroupSumBuffer = new ComputeBuffer(numGroups, 4);
        culledGrassOutputBuffer = new ComputeBuffer(numInstances, 4 * 7);

        compactedGrassIndicesBuffer = new ComputeBuffer(numInstances, 4);

        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);

        wind = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        wind.enableRandomWrite = true;
        wind.Create();

        updateGrassBuffer();
        /*
        uint[] computedata = new uint[numGroups];
        scannedGroupSumBuffer.GetData(computedata);

        for (int i = 0; i < numGroups; ++i) {
            Debug.Log(computedata[i]);
        }*/
    }

    void updateGrassBuffer() {
        Matrix4x4 P = Camera.main.projectionMatrix;
        Matrix4x4 V = Camera.main.transform.worldToLocalMatrix;
        Matrix4x4 VP = P * V;

        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        // Arguments for drawing mesh.
        // 0 == number of triangle indices, 1 == population, others are only relevant if drawing submeshes.
        args[0] = (uint)grassMesh.GetIndexCount(0);
        args[1] = (uint)0;
        args[2] = (uint)grassMesh.GetIndexStart(0);
        args[3] = (uint)grassMesh.GetBaseVertex(0);
        argsBuffer.SetData(args);

        initializeGrassShader.SetInt("_Dimension", resolution * scale);
        initializeGrassShader.SetInt("_Scale", scale);
        initializeGrassShader.SetBuffer(0, "_GrassDataBuffer", grassDataBuffer);
        initializeGrassShader.SetTexture(0, "_HeightMap", heightMap);
        initializeGrassShader.SetFloat("_DisplacementStrength", displacementStrength);
        initializeGrassShader.Dispatch(0, Mathf.CeilToInt((resolution * scale) / 8.0f), Mathf.CeilToInt((resolution * scale) / 8.0f), 1);

        int threadGroupSizeX = Mathf.CeilToInt(numInstances / 128.0f);

        // Vote
        cullGrassShader.SetMatrix("MATRIX_VP", VP);
        cullGrassShader.SetBuffer(0, "_GrassDataBuffer", grassDataBuffer);
        cullGrassShader.SetBuffer(0, "_GrassVoteBuffer", grassVoteBuffer);
        cullGrassShader.Dispatch(0, Mathf.CeilToInt(numInstances / 128.0f), 1, 1);

        // Scan Instances
        cullGrassShader.SetBuffer(1, "_GrassVoteBuffer", grassVoteBuffer);
        cullGrassShader.SetBuffer(1, "_GrassScanBuffer", grassScanBuffer);
        cullGrassShader.SetBuffer(1, "_GroupSumArray", groupSumArrayBuffer);
        cullGrassShader.Dispatch(1, threadGroupSizeX, 1, 1);

        // Scan Groups
        cullGrassShader.SetInt("_NumOfGroups", numGroups);
        cullGrassShader.SetBuffer(2, "_GroupSumArrayIn", groupSumArrayBuffer);
        cullGrassShader.SetBuffer(2, "_GroupSumArrayOut", scannedGroupSumBuffer);
        cullGrassShader.Dispatch(2, Mathf.CeilToInt(numInstances / 1024), 1, 1);

        // Compact
        cullGrassShader.SetBuffer(3, "_GrassDataBuffer", grassDataBuffer);
        cullGrassShader.SetBuffer(3, "_GrassVoteBuffer", grassVoteBuffer);
        cullGrassShader.SetBuffer(3, "_GrassScanBuffer", grassScanBuffer);
        cullGrassShader.SetBuffer(3, "_ArgsBuffer", argsBuffer);
        cullGrassShader.SetBuffer(3, "_CulledGrassOutputBuffer", culledGrassOutputBuffer);
        cullGrassShader.SetBuffer(3, "_GroupSumArray", scannedGroupSumBuffer);
        cullGrassShader.SetBuffer(3, "_CompactedIndicesBuffer", compactedGrassIndicesBuffer);
        cullGrassShader.Dispatch(3, threadGroupSizeX, 1, 1);
        
        GenerateWind();

        grassMaterial.SetBuffer("positionBuffer", grassDataBuffer);
        grassMaterial.SetFloat("_DisplacementStrength", displacementStrength);
        grassMaterial.SetTexture("_WindTex", wind);
    }

    void GenerateWind() {
        generateWindShader.SetTexture(0, "_WindMap", wind);
        generateWindShader.SetFloat("_Time", Time.time * windSpeed);
        generateWindShader.SetFloat("_Frequency", frequency);
        generateWindShader.SetFloat("_Amplitude", windStrength);
        generateWindShader.Dispatch(0, Mathf.CeilToInt(wind.width / 8.0f), Mathf.CeilToInt(wind.height / 8.0f), 1);
    }

    void Update() {      
        GenerateWind();

        grassMaterial.SetBuffer("positionBuffer", culledGrassOutputBuffer);
        grassMaterial.SetBuffer("voteBuffer", grassVoteBuffer);
        grassMaterial.SetFloat("_DisplacementStrength", displacementStrength);
        grassMaterial.SetTexture("_WindTex", wind);

        Graphics.DrawMeshInstancedIndirect(grassMesh, 0, grassMaterial, new Bounds(Vector3.zero, new Vector3(-500.0f, 200.0f, 500.0f)), argsBuffer);

        if (updateGrass) {
            updateGrassBuffer();
            updateGrass = false;
        }
    }
    
    void OnDisable() {
        grassDataBuffer.Release();
        argsBuffer.Release();
        grassVoteBuffer.Release();
        grassScanBuffer.Release();
        culledGrassOutputBuffer.Release();
        groupSumArrayBuffer.Release();
        scannedGroupSumBuffer.Release();
        compactedGrassIndicesBuffer.Release();
        wind.Release();
        grassDataBuffer = null;
        argsBuffer = null;
        wind = null;
        scannedGroupSumBuffer = null;
        grassVoteBuffer = null;
        grassScanBuffer = null;
        groupSumArrayBuffer = null;
        culledGrassOutputBuffer = null;
        compactedGrassIndicesBuffer = null;
    }
}
