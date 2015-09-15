using UnityEngine;
using System;
using System.Runtime.InteropServices;

public class CannyCamera : MonoBehaviour
{
    [DllImport("opencv_sample")]
    private static extern int to_canny(IntPtr src, IntPtr dest, int width, int height, int thresh1, int thresh2);

    private WebCamTexture webcamTexture_;
    private Texture2D cannyTexture_;

    private bool isWebCamInitialized_ = false;
    private int width_;
    private int height_;

    [Range(0, 255)]
    public int thresh1 = 50;
    [Range(0, 255)]
    public int thresh2 = 200;

    void Start()
    {
        SetupWebCamTexture();
        SetupCannyTexture();
    }

    void SetupWebCamTexture()
    {
        var devices = WebCamTexture.devices;
        if (devices.Length > 0) {
            webcamTexture_ = new WebCamTexture(devices[0].name, 640, 480);
            webcamTexture_.Play();
            width_  = webcamTexture_.width;
            height_ = webcamTexture_.height;
            isWebCamInitialized_ = true;
        } else {
            Debug.Log("no camera");
        }
    }

    void SetupCannyTexture()
    {
        if (!isWebCamInitialized_) return;
        cannyTexture_ = new Texture2D(width_, height_);
    }

    void Update()
    {
        if (!isWebCamInitialized_) return;

        var pixels = webcamTexture_.GetPixels32();
        var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        var ptr    = handle.AddrOfPinnedObject();
        to_canny(ptr, ptr, width_, height_, thresh1, thresh2);

        cannyTexture_.SetPixels32(pixels);
        cannyTexture_.Apply();
        GetComponent<Renderer>().material.mainTexture = cannyTexture_;
    }
}