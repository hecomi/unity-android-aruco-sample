#include <opencv2/opencv.hpp>

#ifdef __ANDROID__

#include <android/log.h>
#define LOG_TAG ("UnityOpenCV")
#define LOGI(...) ((void)__android_log_print(ANDROID_LOG_INFO,  LOG_TAG, __VA_ARGS__))
#define LOGD(...) ((void)__android_log_print(ANDROID_LOG_DEBUG, LOG_TAG, __VA_ARGS__))
#define LOGW(...) ((void)__android_log_print(ANDROID_LOG_WARN,  LOG_TAG, __VA_ARGS__))
#define LOGE(...) ((void)__android_log_print(ANDROID_LOG_ERROR, LOG_TAG, __VA_ARGS__))

#else

#define LOGI(...) printf(__VA_ARGS__)
#define LOGD(...) printf(__VA_ARGS__)
#define LOGW(...) printf(__VA_ARGS__)
#define LOGE(...) printf(__VA_ARGS__)

#endif

extern "C"
{

bool get_image_size(const char* path, int* width, int* height)
{
	const auto img = cv::imread(path);
	if (img.empty()) return false;
	*width  = img.rows;
	*height = img.cols;
	return true;
}

bool read_image(const char* path, unsigned char* dest)
{
	const auto rgb = cv::imread(path);
	if (rgb.empty()) return false;
	cv::Mat canny;
	cv::Canny(rgb, canny, 50.f, 200.f);
	cv::Mat rgba;
	cv::cvtColor(canny, rgba, cv::COLOR_GRAY2BGRA);
	memcpy(dest, rgba.data, rgba.total() * rgba.elemSize());
	return true;
}

}
