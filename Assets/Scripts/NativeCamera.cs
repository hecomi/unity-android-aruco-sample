using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

public class NativeCamera : MonoBehaviour 
{
    [DllImport("opencv_sample")]
	private static extern IntPtr get_camera();
    [DllImport("opencv_sample")]
	private static extern void release_camera(IntPtr camera);
    [DllImport("opencv_sample")]
	private static extern bool fetch_image(IntPtr camera, IntPtr dest, int width, int height);

	private IntPtr camera_ = IntPtr.Zero;
	private Texture2D texture_;
	private Color32[] pixels_;
	private GCHandle handle_;
	private IntPtr ptr_;

	void Start() 
	{
		camera_ = get_camera();
		if (camera_ == IntPtr.Zero) {
			Debug.LogError("camera cannot be opened.");
			return;
		}

		texture_ = new Texture2D(640, 480, TextureFormat.RGBA32, false);
		pixels_ = texture_.GetPixels32();
		handle_ = GCHandle.Alloc(pixels_, GCHandleType.Pinned);
		ptr_ = handle_.AddrOfPinnedObject();
		GetComponent<Renderer>().material.mainTexture = texture_;
	}

	void OnDestroy()
	{
		if (handle_.IsAllocated) {
			handle_.Free();
		}
		if (camera_ != IntPtr.Zero) {
			release_camera(camera_);
		}
	}
	
	void Update() 
	{
		if (fetch_image(camera_, ptr_, 640, 480)) {
			texture_.SetPixels32(pixels_);
			texture_.Apply();
		}
	}
}
