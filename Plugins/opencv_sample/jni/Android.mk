LOCAL_PATH := $(call my-dir)

include $(CLEAR_VARS)

OPENCV_CAMERA_MODULES:=on
OPENCV_INSTALL_MODULES:=on
OPENCV_LIB_TYPE  := STATIC
include opencv/jni/OpenCV.mk
include aruco/jni/Aruco.mk

NDK_APP_DST_DIR := ../../../Assets/Plugins/Android
LOCAL_MODULE    := opencv_sample
LOCAL_SRC_FILES += $(shell ls -1 *.cpp)
LOCAL_CFLAGS    += -std=c++11

include $(BUILD_SHARED_LIBRARY)
