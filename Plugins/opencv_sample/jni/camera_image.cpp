#include <opencv2/opencv.hpp>
#include "log.h"

extern "C"
{

void* get_camera()
{
	LOGI("get_camera");

#ifdef __APPLE__
	auto camera = new cv::VideoCapture(0);
#elif __ANDROID__
	auto camera = new cv::VideoCapture(CV_CAP_ANDROID);
#endif
	if (!camera->isOpened()) {
		delete camera;
		return nullptr;
	}
	return camera;
}

void release_camera(void* ptr)
{
	LOGI("release_camera");

	auto camera = static_cast<cv::VideoCapture*>(ptr);
	delete camera;
}

bool fetch_image(void* ptr, unsigned char* dest, int width, int height)
{
	LOGI("fetch_image");

	auto camera = static_cast<cv::VideoCapture*>(ptr);
	if (!camera->isOpened()) {
		return false;
	}

	cv::Mat rgb;
	(*camera) >> rgb;
	if (rgb.empty()) {
		return false;
	}

	cv::Mat resized(height, width, rgb.type());
	cv::resize(rgb, resized, resized.size(), cv::INTER_LINEAR);

	cv::Mat rgba;
	cv::cvtColor(resized, rgba, CV_BGR2BGRA);
	memcpy(dest, rgba.data, rgba.total() * rgba.elemSize());

	return true;
}

void to_canny(unsigned char* src, unsigned char* dest, int width, int height, int thresh1, int thresh2)
{
	cv::Mat src_img(height, width, CV_8UC4, src);
	cv::Mat dest_img;
	cv::Canny(src_img, dest_img, thresh1, thresh2);
	cv::cvtColor(dest_img, dest_img, CV_GRAY2BGRA);
	memcpy(dest, dest_img.data, dest_img.total() * dest_img.elemSize());
}

}
