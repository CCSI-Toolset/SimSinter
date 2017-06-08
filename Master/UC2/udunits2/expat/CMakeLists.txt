INCLUDE (CheckFunctionExists)
INCLUDE (CheckIncludeFiles)
INCLUDE (TestBigEndian)
IF (${BIGENDIAN})
  set (expat_BYTEORDER 4321)
ELSE(${BIGENDIAN})
  set (expat_BYTEORDER 1234)
ENDIF(${BIGENDIAN})
set (XML_CONTEXT_BYTES 1024)
CHECK_INCLUDE_FILES (dlfcn.h HAVE_DLFCN_H)
CHECK_INCLUDE_FILES (fcntl.h HAVE_FCNTL_H)
CHECK_INCLUDE_FILES (inttypes.h HAVE_INTTYPES_H)
CHECK_INCLUDE_FILES (memory.h HAVE_MEMORY_H)
CHECK_INCLUDE_FILES (stdint.h HAVE_STDINT_H)
CHECK_INCLUDE_FILES (stdlib.h HAVE_STDLIB_H)
CHECK_INCLUDE_FILES (strings.h HAVE_STRINGS_H)
CHECK_INCLUDE_FILES (string.h HAVE_STRING_H)
CHECK_INCLUDE_FILES ("sys/stat.h" HAVE_SYS_STAT_H)
CHECK_INCLUDE_FILES ("sys/types.h" HAVE_SYS_TYPES_H)
CHECK_INCLUDE_FILES (unistd.h HAVE_UNISTD_H)
check_function_exists(mmap HAVE_MMAP)
check_function_exists(memmove HAVE_MEMMOVE)
check_function_exists(getpagesize HAVE_GETPAGESIZE)

ADD_DEFINITIONS(-DHAVE_EXPAT_CONFIG_H)

configure_file (
  "${PROJECT_SOURCE_DIR}/expat/expat_config.h.in"
  "${PROJECT_BINARY_DIR}/expat/expat_config.h"
)

add_library(expat SHARED ascii.h asciitab.h expat.h expat_external.h iasciitab.h internal.h latin1tab.h nametab.h utf8tab.h xmlparse.c xmlrole.c xmlrole.h xmltok.c xmltok.h xmltok_impl.h )

install (TARGETS expat
	 RUNTIME DESTINATION bin
	 LIBRARY DESTINATION lib
	 ARCHIVE DESTINATION lib)
