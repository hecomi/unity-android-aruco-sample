using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

public class ArPlane : MonoBehaviour
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
	private static extern int aruco_detect(IntPtr instance, bool drawOutputImage);
	[DllImport("opencv_sample")]
	private static extern IntPtr aruco_get_markers(IntPtr instance);

	private WebCamTexture webcamTexture_;
	private bool isWebCamInitialized_ = false;
	private int width_;
	private int height_;

	private IntPtr aruco_ = IntPtr.Zero;
	private bool isArucoUpdated_ = false;
	public float unityMarkerSize = 1f;
	public float markerSize = 0.1f;
	public string cameraParamsFileName = "intrinsics.yml";
	private List<MarkerResult> markers_ = new List<MarkerResult>();
	private int markerNum_;

	private Thread thread_;
	private Mutex mutex_;

	public GameObject prefab;
	private Dictionary<int, GameObject> arObjects_ = new Dictionary<int, GameObject>();

	[StructLayout(LayoutKind.Sequential)]
	struct MarkerResult
	{
		[MarshalAs(UnmanagedType.I4)]
		public int id;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst=3)]
		public double[] position;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst=4)]
		public double[] orientation;
	}

	public int cameraWidth = 800;
	public int cameraHeight = 450;
	public int cameraFrameRate = 15;
	public float cameraFov = 38f;

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

		mutex_ = new Mutex();
		thread_ = new Thread(() => {
			try {
				for (;;) {
					Thread.Sleep(0);
					if (!isArucoUpdated_) {
						mutex_.WaitOne();
						var num = aruco_detect(aruco_, false);
						GetMarkers(num);
						mutex_.ReleaseMutex();
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
			webcamTexture_ = new WebCamTexture(devices[0].name, cameraWidth, cameraHeight, cameraFrameRate);
			webcamTexture_.Play();
			GetComponent<Renderer>().material.mainTexture = webcamTexture_;
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

	void GetMarkers(int num)
	{
		markers_.Clear();
		var ptr = aruco_get_markers(aruco_);
		var size = Marshal.SizeOf(typeof(MarkerResult));
		for (int i = 0; i < num; ++i) {
			var data = new IntPtr(ptr.ToInt64() + size * i);
			var marker = (MarkerResult)Marshal.PtrToStructure(data, typeof(MarkerResult));
			markers_.Add(marker);
		}
		Debug.Log(num);
	}

	void OnMarkerDetected(int id, Vector3 pos, Quaternion rot)
	{
		if (arObjects_.ContainsKey(id)) {
			var obj = arObjects_[id].transform;
			obj.localPosition = pos;
			obj.localRotation = rot;
		} else {
			var obj = Instantiate(prefab) as GameObject;
			obj.transform.parent = Camera.main.transform;
			obj.transform.localPosition = pos;
			obj.transform.localRotation = rot;
			arObjects_.Add(id, obj);
		}
	}

	Vector3 GetMarkerPos(double[] p)
	{
		var unityFov = Camera.main.fieldOfView;
		// should be 1f
		var xyScaleFactor = Mathf.Tan(Mathf.Deg2Rad * unityFov) / Mathf.Tan(Mathf.Deg2Rad * cameraFov);
		var realToUnityScale = unityMarkerSize / markerSize;
		var x = -(float)p[0] * xyScaleFactor;
		var y = -(float)p[1] * xyScaleFactor;
		var z =  (float)p[2];
		return new Vector3(x, y, z) * realToUnityScale;
	}

	Quaternion GetMarkerRot(double[] q)
	{
		var x = -(float)q[2];
		var y =  (float)q[1];
		var z =  (float)q[0];
		var w = -(float)q[3];
		return new Quaternion(x, y, z, w);
	}

	void FullScreen()
	{
		var fov = Camera.main.fieldOfView;
		var farPos = 1000f;
		var scaleY = farPos * Mathf.Tan(Mathf.Deg2Rad * fov / 2) * 2 * 0.1f;
		var scaleX = scaleY * width_ / height_;
		transform.localPosition = Vector3.forward * farPos;
		transform.localScale = new Vector3(scaleX, 1f, scaleY);
	}

	void SetObjectsVisibility()
	{
		var detectedIds = new List<int>();
		foreach (var marker in markers_) {
			detectedIds.Add(marker.id);
		}
		foreach (var kvp in arObjects_) {
			var id = kvp.Key;
			var obj = kvp.Value;
			bool isDetected = false;
			foreach (var detectedId in detectedIds) {
				if (id == detectedId) {
					isDetected = true;
					break;
				}
			}
			obj.SetActive(isDetected);
		}
	}

	void Update()
	{
		if (isWebCamInitialized_ && webcamTexture_.didUpdateThisFrame) return;

		while (!isArucoUpdated_) Thread.Sleep(1);

		var pixels = webcamTexture_.GetPixels32();
		var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
		var ptr	= handle.AddrOfPinnedObject();
		aruco_set_image(aruco_, ptr);
		handle.Free();

		mutex_.WaitOne();
		foreach (var marker in markers_) {
			var pos = GetMarkerPos(marker.position);
			var rot = GetMarkerRot(marker.orientation);
			OnMarkerDetected(marker.id, pos, rot);
		}
		SetObjectsVisibility();
		isArucoUpdated_ = false;
		mutex_.ReleaseMutex();

		FullScreen();
	}

	void OnGUI()
	{
		Rect rect = new Rect(20, 50, 400, 30);
		cameraFov = GUI.HorizontalSlider(rect, cameraFov, 0, 100);
	}
}
