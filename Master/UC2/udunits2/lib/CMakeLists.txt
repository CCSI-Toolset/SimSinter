CHECK_INCLUDE_FILES (strings.h HAVE_STRINGS_H)
CHECK_INCLUDE_FILES (string.h HAVE_STRING_H)
CHECK_INCLUDE_FILES (inttypes.h HAVE_INTTYPES_H)
CHECK_INCLUDE_FILES (stdint.h HAVE_STDINT_H)
CHECK_INCLUDE_FILES (unistd.h HAVE_UNISTD_H)
CHECK_INCLUDE_FILES (libgen.h HAVE_LIBGEN_H)
ADD_DEFINITIONS("-DDEFAULT_UDUNITS2_XML_PATH=\"${CMAKE_INSTALL_PREFIX}/shared\"")
install (FILES udunits2.xml udunits2-accepted.xml udunits2-base.xml udunits2-common.xml udunits2-derived.xml udunits2-prefixes.xml
         DESTINATION shared)
install (FILES converter.h udunits2.h udunits2_dll.h getopt.h
	 DESTINATION include)
add_library(udunits2 SHARED unitcore.c converter.c formatter.c idToUnitMap.c idToUnitMap.h unitToIdMap.c unitToIdMap.h unitAndId.c unitAndId.h systemMap.c systemMap.h parser.c prefix.c status.c xml.c error.c ut_free_system.c udunits2_dll.h tsearch.c tsearch.h getopt.h getopt.c getopt1.c)

if(WIN32)
target_link_libraries (udunits2 expat msvcrt)
else(WIN32)
target_link_libraries (udunits2 expat m)
endif(WIN32)

install (TARGETS udunits2
	 RUNTIME DESTINATION bin
	 LIBRARY DESTINATION lib
	 ARCHIVE DESTINATION lib)

configure_file (
  "${PROJECT_SOURCE_DIR}/lib/udunits2_config.h.in"
  "${PROJECT_BINARY_DIR}/lib/udunits2_config.h"
)
