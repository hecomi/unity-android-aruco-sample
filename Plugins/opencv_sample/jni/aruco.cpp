#include <opencv2/opencv.hpp>
#include <aruco.h>
#include <cstdio>
#include "log.h"


class aruco_manager
{
public:
	struct marker_result
	{
		int id;
		double position[3];
		double orientation[4];
	};

	aruco_manager()
	{
	}

	void set_image_size(int width, int height)
	{
		width_ = width;
		height_ = height;
		input_ = cv::Mat(height, width, CV_8UC4);
	}

	void set_marker_size(float marker_size)
	{
		marker_size_ = marker_size;
	}

	void set_params(const char* camera_params_file_path)
	{
		params_.readFromXMLFile(camera_params_file_path);
		params_.resize(cv::Size(width_, height_));
	}

	void set_image(unsigned char* src)
	{
		if (src != nullptr) {
			input_ = cv::Mat(height_, width_, CV_8UC4, src).clone();
		}
	}

	void get_image(unsigned char* dest) const
	{
		if (dest != nullptr) {
			memcpy(dest, output_.data, output_.total() * output_.elemSize());
		}
	}

	size_t detect(bool drawOutputImage = true)
	{
		if (input_.empty()) return -1;

		cv::Mat bgr;
		cv::cvtColor(input_, bgr, cv::COLOR_BGRA2BGR);

		cv::Mat resized;
		cv::resize(bgr, resized, cv::Size(), 0.4f, 0.4f);

		std::vector<aruco::Marker> markers;
		detector_.detect(bgr, markers, params_, marker_size_, true);

		markers_.clear();

		for (auto&& marker : markers) {
			marker.draw(bgr, cv::Scalar(0, 0, 255), 2);
			aruco::CvDrawingUtils::draw3dCube(bgr, marker, params_);

			marker_result result;
			result.id = marker.id;
			marker.OgreGetPoseParameters(result.position, result.orientation);

			markers_.push_back(result);
		}

		if (drawOutputImage) {
			cv::cvtColor(bgr, output_, cv::COLOR_BGR2BGRA);
		}

		input_.release();

		return markers_.size();
	}

	void* get_markers()
	{
		return &markers_[0];
	}

private:
	aruco::CameraParameters params_;
	aruco::MarkerDetector detector_;
	cv::Mat input_;
	cv::Mat output_;
	int width_  = 0;
	int height_ = 0;
	float marker_size_ = 0.1f;
	int marker_num_ = 10;
	std::vector<marker_result> markers_;
};


extern "C"
{

void* aruco_initialize(
	int width,
	int height,
	float marker_size,
	const char* camera_params_file_path)
{
	auto manager = new aruco_manager();
	manager->set_image_size(width, height);
	manager->set_marker_size(marker_size);
	manager->set_params(camera_params_file_path);
	return manager;
}

void aruco_finalize(void* instance)
{
	auto manager = static_cast<aruco_manager*>(instance);
	delete manager;
	instance = nullptr;
}

void aruco_set_image(void* instance, unsigned char* src)
{
	auto manager = static_cast<aruco_manager*>(instance);
	if (manager == nullptr) return;
	manager->set_image(src);
}

void aruco_get_image(void* instance, unsigned char* dest)
{
	auto manager = static_cast<aruco_manager*>(instance);
	if (manager == nullptr) return;
	manager->get_image(dest);
}

int aruco_detect(void* instance)
{
	auto manager = static_cast<aruco_manager*>(instance);
	if (manager == nullptr) return -1;
	return static_cast<int>(manager->detect());
}

void* aruco_get_markers(void* instance)
{
	auto manager = static_cast<aruco_manager*>(instance);
	if (manager == nullptr) return nullptr;
	return manager->get_markers();
}

}
