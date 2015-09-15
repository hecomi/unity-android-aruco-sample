ARUCO_PATH       := $(call my-dir)
LOCAL_SRC_FILES  += $(shell ls -1 $(ARUCO_PATH)/*.cpp)
LOCAL_C_INCLUDES += $(ARUCO_PATH)


