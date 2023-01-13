using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DepthVisualizer : MonoBehaviour
{
    public Shader visualizerShader;
    public Material visualizerMaterial;

    Camera cam;
    RenderTexture rt;
    RenderTexture rtDepth;

    Vector2 lastres = Vector2.zero;

    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();
        Vector2 currentRes = new Vector2(Screen.width, Screen.height);
        Initialize(currentRes);
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 currentRes = new Vector2(Screen.width, Screen.height);
        if(currentRes != lastres)
        {
            Initialize(currentRes);
        }
    }
    void Initialize(Vector2 currentRes)
    {
        if (rt)
            Destroy(rt);
        if (rtDepth)
            Destroy(rtDepth);

        rt = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat);
        rtDepth = new RenderTexture(Screen.width, Screen.height, 32, RenderTextureFormat.Depth);

        cam.SetTargetBuffers(rt.colorBuffer, rtDepth.depthBuffer);
        visualizerMaterial.SetTexture("depthTex", rtDepth);

        lastres= currentRes;
    }
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, visualizerMaterial);
    }
}
