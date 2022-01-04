using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PostProcessor : MonoBehaviour {
    public Shader fogShader;

    public Color fogColor;
    
    [Range(0.0f, 1.0f)]
    public float fogDensity;
    
    private Material fogMat;

    void Start() {
        if (fogMat == null) {
            fogMat = new Material(fogShader);
            fogMat.hideFlags = HideFlags.HideAndDontSave;
        }

        Camera cam = GetComponent<Camera>();
        cam.depthTextureMode = cam.depthTextureMode | DepthTextureMode.Depth;
    }

    [ImageEffectOpaque]
    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        fogMat.SetVector("_FogColor", fogColor);
        fogMat.SetFloat("_FogDensity", fogDensity);
        Graphics.Blit(source, destination, fogMat);
    }
}