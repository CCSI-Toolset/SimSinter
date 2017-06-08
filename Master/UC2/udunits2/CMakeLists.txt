cmake_minimum_required (VERSION 2.8)

project (UDUnits)

include_directories ("${PROJECT_SOURCE_DIR}/lib" "${PROJECT_SOURCE_DIR}/expat"
  "${PROJECT_BINARY_DIR}/lib" "${PROJECT_BINARY_DIR}/expat" )
add_subdirectory (expat)
add_subdirectory (lib)
add_subdirectory (prog)