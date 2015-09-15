using UnityEngine;
using System;
using System.Runtime.InteropServices;

public class ReadTexture : MonoBehaviour
{
	[DllImport("opencv_sample")]
	private static extern bool get_image_size(string path, out int width, out int height);
	[DllImport("opencv_sample")]
	private static extern bool read_image(string path, IntPtr ptr);

	private Texture2D texture_;
	private Color32[] pixels_;
	private GCHandle pixels_handle_;
	private IntPtr pixels_ptr_ = IntPtr.Zero;

	public string imagePath = "zzz.png";

	string GetFilePath(string fileName)
	{
#if UNITY_EDITOR
		return Application.streamingAssetsPath + "/" + fileName;
#elif UNITY_ANDROID
		return "/sdcard/AndroidOpenCvSample/" + fileName;
#endif
	}

	void Start()
	{
		var path = GetFilePath(imagePath);
		int width, height;
		if (!get_image_size(path, out width, out height)) {
			Debug.LogFormat("{0} was not found", path);
			return;
		}

		texture_ = new Texture2D(width, height, TextureFormat.RGB24, false);
		texture_.filterMode = FilterMode.Point;
		pixels_ = texture_.GetPixels32();
		pixels_handle_ = GCHandle.Alloc(pixels_, GCHandleType.Pinned);
		pixels_ptr_ = pixels_handle_.AddrOfPinnedObject();
		GetComponent<Renderer>().material.mainTexture = texture_;

		read_image(path, pixels_ptr_);

		texture_.SetPixels32(pixels_);
		texture_.Apply();
		pixels_handle_.Free();
	}
}
