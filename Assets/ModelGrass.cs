using System.Collections;
using System.Collections.Generic;
using static System.Runtime.InteropServices.Marshal;
using UnityEngine;

public class ModelGrass : MonoBehaviour {
    public int fieldSize = 100;
    public int chunkDensity = 1;
    public int numChunks = 1;
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

    private int numInstancesPerChunk, numThreadGroups;

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
        numInstancesPerChunk = fieldSize * chunkDensity;
        numInstancesPerChunk *= numInstancesPerChunk;

        numThreadGroups = numInstancesPerChunk / 128;
        if (numThreadGroups > 128) {
            int powerOfTwo = 128;
            while (powerOfTwo < numThreadGroups)
                powerOfTwo *= 2;
            
            numThreadGroups = powerOfTwo;
        } else {
            while (128 % numThreadGroups != 0)
                numThreadGroups++;
        }

        initializeGrassShader = Resources.Load<ComputeShader>("GrassChunkPoint");
        generateWindShader = Resources.Load<ComputeShader>("WindNoise");
        cullGrassShader = Resources.Load<ComputeShader>("CullGrass");

        voteBuffer = new ComputeBuffer(numInstancesPerChunk, 4);
        scanBuffer = new ComputeBuffer(numInstancesPerChunk, 4);
        groupSumArrayBuffer = new ComputeBuffer(numThreadGroups, 4);
        scannedGroupSumBuffer = new ComputeBuffer(numThreadGroups, 4);

        grassChunk = initializeGrassChunk(0, 0);
        grassChunk2 = initializeGrassChunk(1, 1);

        wind = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        wind.enableRandomWrite = true;
        wind.Create();

        grassMat2 = new Material(grassMaterial);
        updateGrassBuffer();
        /*
        uint[] computedata = new uint[numThreadGroups];
        scannedGroupSumBuffer.GetData(computedata);

        for (int i = 0; i < numThreadGroups; ++i) {
            Debug.Log(computedata[i]);
        }*/
    }

    GrassChunk initializeGrassChunk(int xOffset, int yOffset) {
        GrassChunk chunk = new GrassChunk();

        chunk.argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);

        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        // Arguments for drawing mesh.
        // 0 == number of triangle indices, 1 == population, others are only relevant if drawing submeshes.
        args[0] = (uint)grassMesh.GetIndexCount(0);
        args[1] = (uint)0;
        args[2] = (uint)grassMesh.GetIndexStart(0);
        args[3] = (uint)grassMesh.GetBaseVertex(0);
        chunk.argsBuffer.SetData(args);

        chunk.positionsBuffer = new ComputeBuffer(numInstancesPerChunk, SizeOf(typeof(GrassData)));
        chunk.culledPositionsBuffer = new ComputeBuffer(numInstancesPerChunk, SizeOf(typeof(GrassData)));
        chunk.bounds = new Bounds(this.transform.position, new Vector3(-20.0f, 200.0f, 20.0f));

        return chunk;
    }

    void updateGrassBuffer() {
        initializeGrassShader.SetInt("_Dimension", fieldSize);
        initializeGrassShader.SetInt("_Scale", chunkDensity);
        initializeGrassShader.SetInt("_XOffset", 1);
        initializeGrassShader.SetInt("_YOffset", 1);
        initializeGrassShader.SetInt("_NumChunks", 2);
        initializeGrassShader.SetBuffer(0, "_GrassDataBuffer", grassChunk.positionsBuffer);
        initializeGrassShader.SetTexture(0, "_HeightMap", heightMap);
        initializeGrassShader.SetFloat("_DisplacementStrength", displacementStrength);
        initializeGrassShader.Dispatch(0, Mathf.CeilToInt((fieldSize * chunkDensity) / 8.0f), Mathf.CeilToInt((fieldSize * chunkDensity) / 8.0f), 1);

        initializeGrassShader.SetBuffer(0, "_GrassDataBuffer", grassChunk2.positionsBuffer);
        initializeGrassShader.SetInt("_XOffset", 0);
        initializeGrassShader.SetInt("_YOffset", 0);
        initializeGrassShader.Dispatch(0, Mathf.CeilToInt((fieldSize * chunkDensity) / 8.0f), Mathf.CeilToInt((fieldSize * chunkDensity) / 8.0f), 1);
        
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
        cullGrassShader.Dispatch(0, Mathf.CeilToInt(numInstancesPerChunk / 128.0f), 1, 1);

        // Scan Instances
        cullGrassShader.SetBuffer(1, "_VoteBuffer", voteBuffer);
        cullGrassShader.SetBuffer(1, "_ScanBuffer", scanBuffer);
        cullGrassShader.SetBuffer(1, "_GroupSumArray", groupSumArrayBuffer);
        cullGrassShader.Dispatch(1, numThreadGroups, 1, 1);

        // Scan Groups
        cullGrassShader.SetInt("_NumOfGroups", numThreadGroups);
        cullGrassShader.SetBuffer(2, "_GroupSumArrayIn", groupSumArrayBuffer);
        cullGrassShader.SetBuffer(2, "_GroupSumArrayOut", scannedGroupSumBuffer);
        cullGrassShader.Dispatch(2, Mathf.CeilToInt(numInstancesPerChunk / 1024.0f), 1, 1);

        // Compact
        cullGrassShader.SetBuffer(3, "_GrassDataBuffer", chunk.positionsBuffer);
        cullGrassShader.SetBuffer(3, "_VoteBuffer", voteBuffer);
        cullGrassShader.SetBuffer(3, "_ScanBuffer", scanBuffer);
        cullGrassShader.SetBuffer(3, "_ArgsBuffer", chunk.argsBuffer);
        cullGrassShader.SetBuffer(3, "_CulledGrassOutputBuffer", chunk.culledPositionsBuffer);
        cullGrassShader.SetBuffer(3, "_GroupSumArray", scannedGroupSumBuffer);
        cullGrassShader.Dispatch(3, numThreadGroups, 1, 1);
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
