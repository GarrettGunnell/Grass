using System.Collections;
using System.Collections.Generic;
using static System.Runtime.InteropServices.Marshal;
using UnityEngine;

public class ModelGrass : MonoBehaviour {
    public int resolution = 100;
    public int scale = 1;
    public float displacementStrength = 200.0f;
    public Material grassMaterial;
    public Mesh grassMesh;
    public Texture heightMap;

    [Header("Wind")]
    public float windSpeed = 1.0f;
    public float frequency = 1.0f;
    public float windStrength = 1.0f;

    private ComputeShader initializeGrassShader, generateWindShader, cullGrassShader;
    private ComputeBuffer voteBuffer, scanBuffer, groupSumArrayBuffer, scannedGroupSumBuffer;

    private RenderTexture wind;

    private int numInstances, numGroups;

    private struct GrassData {
        public Vector4 position;
        public Vector2 uv;
        public float displacement;
    }

    private struct GrassChunk {
        public ComputeBuffer argsBuffer;
        public ComputeBuffer positionsBuffer;
        public ComputeBuffer culledPositionsBuffer;
        public Bounds bounds;
    }

    GrassChunk grassChunk, grassChunk2;

    private Material grassMat2;

    void OnEnable() {
        numInstances = resolution * scale;
        numInstances *= numInstances;

        numGroups = numInstances / 128;
        if (numGroups > 128) {
            int powerOfTwo = 128;
            while (powerOfTwo < numGroups)
                powerOfTwo *= 2;
            
            numGroups = powerOfTwo;
        } else {
            while (128 % numGroups != 0)
                numGroups++;
        }

        initializeGrassShader = Resources.Load<ComputeShader>("GrassChunkPoint");
        generateWindShader = Resources.Load<ComputeShader>("WindNoise");
        cullGrassShader = Resources.Load<ComputeShader>("CullGrass");
        
        grassChunk.argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        // Arguments for drawing mesh.
        // 0 == number of triangle indices, 1 == population, others are only relevant if drawing submeshes.
        args[0] = (uint)grassMesh.GetIndexCount(0);
        args[1] = (uint)0;
        args[2] = (uint)grassMesh.GetIndexStart(0);
        args[3] = (uint)grassMesh.GetBaseVertex(0);
        grassChunk.argsBuffer.SetData(args);

        grassChunk.positionsBuffer = new ComputeBuffer(numInstances, SizeOf(typeof(GrassData)));
        voteBuffer = new ComputeBuffer(numInstances, 4);
        scanBuffer = new ComputeBuffer(numInstances, 4);
        groupSumArrayBuffer = new ComputeBuffer(numGroups, 4);
        scannedGroupSumBuffer = new ComputeBuffer(numGroups, 4);
        grassChunk.culledPositionsBuffer = new ComputeBuffer(numInstances, SizeOf(typeof(GrassData)));

        grassChunk2.positionsBuffer = new ComputeBuffer(numInstances, SizeOf(typeof(GrassData)));
        grassChunk2.culledPositionsBuffer = new ComputeBuffer(numInstances, SizeOf(typeof(GrassData)));
        grassChunk2.argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        grassChunk2.argsBuffer.SetData(args);

        wind = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        wind.enableRandomWrite = true;
        wind.Create();

        grassChunk.bounds = new Bounds(this.transform.position, new Vector3(-20.0f, 200.0f, 20.0f));
        grassChunk2.bounds = new Bounds(this.transform.position, new Vector3(-20.0f, 200.0f, 20.0f));

        grassMat2 = new Material(grassMaterial);
        updateGrassBuffer();
        /*
        uint[] computedata = new uint[numGroups];
        scannedGroupSumBuffer.GetData(computedata);

        for (int i = 0; i < numGroups; ++i) {
            Debug.Log(computedata[i]);
        }*/
    }

    void updateGrassBuffer() {
        initializeGrassShader.SetInt("_Dimension", resolution);
        initializeGrassShader.SetInt("_Scale", scale);
        initializeGrassShader.SetInt("_XOffset", 1);
        initializeGrassShader.SetInt("_YOffset", 1);
        initializeGrassShader.SetInt("_NumChunks", 2);
        initializeGrassShader.SetBuffer(0, "_GrassDataBuffer", grassChunk.positionsBuffer);
        initializeGrassShader.SetTexture(0, "_HeightMap", heightMap);
        initializeGrassShader.SetFloat("_DisplacementStrength", displacementStrength);
        initializeGrassShader.Dispatch(0, Mathf.CeilToInt((resolution * scale) / 8.0f), Mathf.CeilToInt((resolution * scale) / 8.0f), 1);

        initializeGrassShader.SetBuffer(0, "_GrassDataBuffer", grassChunk2.positionsBuffer);
        initializeGrassShader.SetInt("_XOffset", 0);
        initializeGrassShader.SetInt("_YOffset", 0);
        initializeGrassShader.Dispatch(0, Mathf.CeilToInt((resolution * scale) / 8.0f), Mathf.CeilToInt((resolution * scale) / 8.0f), 1);
        
        CullGrass(grassChunk);
        CullGrass(grassChunk2);
        GenerateWind();

        grassMaterial.SetBuffer("positionBuffer", grassChunk.culledPositionsBuffer);
        grassMaterial.SetFloat("_DisplacementStrength", displacementStrength);
        grassMaterial.SetTexture("_WindTex", wind);
    }

    void CullGrass(GrassChunk chunk) {        
        Matrix4x4 P = Camera.main.projectionMatrix;
        Matrix4x4 V = Camera.main.transform.worldToLocalMatrix;
        Matrix4x4 VP = P * V;

        //Reset Args
        cullGrassShader.SetBuffer(4, "_ArgsBuffer", chunk.argsBuffer);
        cullGrassShader.Dispatch(4, 1, 1, 1);

        // Vote
        cullGrassShader.SetMatrix("MATRIX_VP", VP);
        cullGrassShader.SetBuffer(0, "_GrassDataBuffer", chunk.positionsBuffer);
        cullGrassShader.SetBuffer(0, "_VoteBuffer", voteBuffer);
        cullGrassShader.Dispatch(0, Mathf.CeilToInt(numInstances / 128.0f), 1, 1);

        // Scan Instances
        cullGrassShader.SetBuffer(1, "_VoteBuffer", voteBuffer);
        cullGrassShader.SetBuffer(1, "_ScanBuffer", scanBuffer);
        cullGrassShader.SetBuffer(1, "_GroupSumArray", groupSumArrayBuffer);
        cullGrassShader.Dispatch(1, numGroups, 1, 1);

        // Scan Groups
        cullGrassShader.SetInt("_NumOfGroups", numGroups);
        cullGrassShader.SetBuffer(2, "_GroupSumArrayIn", groupSumArrayBuffer);
        cullGrassShader.SetBuffer(2, "_GroupSumArrayOut", scannedGroupSumBuffer);
        cullGrassShader.Dispatch(2, Mathf.CeilToInt(numInstances / 1024.0f), 1, 1);

        // Compact
        cullGrassShader.SetBuffer(3, "_GrassDataBuffer", chunk.positionsBuffer);
        cullGrassShader.SetBuffer(3, "_VoteBuffer", voteBuffer);
        cullGrassShader.SetBuffer(3, "_ScanBuffer", scanBuffer);
        cullGrassShader.SetBuffer(3, "_ArgsBuffer", chunk.argsBuffer);
        cullGrassShader.SetBuffer(3, "_CulledGrassOutputBuffer", chunk.culledPositionsBuffer);
        cullGrassShader.SetBuffer(3, "_GroupSumArray", scannedGroupSumBuffer);
        cullGrassShader.Dispatch(3, numGroups, 1, 1);
    }

    void GenerateWind() {
        generateWindShader.SetTexture(0, "_WindMap", wind);
        generateWindShader.SetFloat("_Time", Time.time * windSpeed);
        generateWindShader.SetFloat("_Frequency", frequency);
        generateWindShader.SetFloat("_Amplitude", windStrength);
        generateWindShader.Dispatch(0, Mathf.CeilToInt(wind.width / 8.0f), Mathf.CeilToInt(wind.height / 8.0f), 1);
    }

    void Update() {
        CullGrass(grassChunk);
        CullGrass(grassChunk2);
        GenerateWind();

        grassMaterial.SetBuffer("positionBuffer", grassChunk.culledPositionsBuffer);
        grassMaterial.SetFloat("_DisplacementStrength", displacementStrength);
        grassMaterial.SetTexture("_WindTex", wind);

        grassMat2.SetBuffer("positionBuffer", grassChunk2.culledPositionsBuffer);
        grassMat2.SetFloat("_DisplacementStrength", displacementStrength);
        grassMat2.SetTexture("_WindTex", wind);

        Graphics.DrawMeshInstancedIndirect(grassMesh, 0, grassMaterial, grassChunk.bounds, grassChunk.argsBuffer);
        Graphics.DrawMeshInstancedIndirect(grassMesh, 0, grassMat2, grassChunk2.bounds, grassChunk2.argsBuffer);
    }
    
    void OnDisable() {
        grassChunk.positionsBuffer.Release();
        grassChunk.argsBuffer.Release();
        grassChunk2.positionsBuffer.Release();
        grassChunk2.argsBuffer.Release();
        voteBuffer.Release();
        scanBuffer.Release();
        grassChunk.culledPositionsBuffer.Release();
        grassChunk2.culledPositionsBuffer.Release();
        groupSumArrayBuffer.Release();
        scannedGroupSumBuffer.Release();
        wind.Release();
        wind = null;
        scannedGroupSumBuffer = null;
        voteBuffer = null;
        scanBuffer = null;
        groupSumArrayBuffer = null;
    }
}
