using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;

public class DetectArucoMarker : MonoBehaviour
{
    [DllImport("opencv_sample")]
    private static extern IntPtr aruco_initialize(int width, int height, float markerSize, string cameraParamsFilePath);
    [DllImport("opencv_sample")]
    private static extern void aruco_finalize(IntPtr instance);
    [DllImport("opencv_sample")]
    private static extern void aruco_set_image(IntPtr instance, IntPtr src);
    [DllImport("opencv_sample")]
    private static extern void aruco_get_image(IntPtr instance, IntPtr dest);
    [DllImport("opencv_sample")]
    private static extern int aruco_detect(IntPtr instance);

    private WebCamTexture webcamTexture_;
    private bool isWebCamInitialized_ = false;
    private int width_;
    private int height_;

    private IntPtr aruco_ = IntPtr.Zero;
    private bool isArucoUpdated_ = false;
    public float markerSize = 0.04f;
    public string cameraParamsFileName = "intrinsics.yml";

    public Thread thread_;

    string GetFilePath(string fileName)
    {
#if UNITY_EDITOR
        return Application.streamingAssetsPath + "/" + fileName;
#elif UNITY_ANDROID
        return "/sdcard/AndroidOpenCvSample/" + fileName;
#endif
    }

    void Awake()
    {
        SetupWebCamTexture();
        InitializeAruco();

        thread_ = new Thread(() => {
            try {
                for (;;) {
                    Thread.Sleep(0);
                    if (!isArucoUpdated_) {
                        aruco_detect(aruco_);
                        isArucoUpdated_ = true;
                    }
                }
            } catch (Exception e) {
                if (!(e is ThreadAbortException)) {
                    Debug.LogError("Unexpected Death: " + e.ToString());
                }
            }
        });
        thread_.Start();
    }

    void OnDestroy()
    {
        thread_.Abort();
        FinalizeAruco();
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

    void InitializeAruco()
    {
        if (!isWebCamInitialized_) return;

        var path = GetFilePath(cameraParamsFileName);
        aruco_ = aruco_initialize(width_, height_, markerSize, path);
    }

    void FinalizeAruco()
    {
        aruco_finalize(aruco_);
    }

    void Update()
    {
        if (isWebCamInitialized_ && webcamTexture_.didUpdateThisFrame) return;

        if (isArucoUpdated_) {
            {
                var pixels = webcamTexture_.GetPixels32();
                var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
                var ptr    = handle.AddrOfPinnedObject();
                aruco_set_image(aruco_, ptr);
                handle.Free();
            }
            {
                var tex = new Texture2D(width_, height_, TextureFormat.RGBA32, false, false);
                var pixels = tex.GetPixels32();
                var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
                var ptr    = handle.AddrOfPinnedObject();
                aruco_get_image(aruco_, ptr);
                handle.Free();
                tex.SetPixels32(pixels);
                tex.Apply(false, false);
                GetComponent<Renderer>().material.mainTexture = tex;
            }
            isArucoUpdated_ = false;
        }
    }
}
