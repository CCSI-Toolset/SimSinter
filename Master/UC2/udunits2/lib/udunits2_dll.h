#ifndef __UDUNITS2_DLL_H_LOADED__
#define __UDUNITS2_DLL_H_LOADED__

/* Cmake will define MyLibrary_EXPORTS on Windows when it
configures to build a shared library. If you are going to use
another build system on windows or create the visual studio
projects by hand you need to define MyLibrary_EXPORTS when
building a DLL on windows.
*/

#if defined (_WIN32) 
  #if defined(udunits2_EXPORTS)
    #define  UDUNITS2_EXPORT __declspec(dllexport)
  #else
    #define  UDUNITS2_EXPORT __declspec(dllimport)
  #endif /* udunits2_EXPORTS */
#else /* defined (_WIN32) */
 #define UDUNITS2_EXPORT
#endif

#if defined(_WIN32)
#define strcasecmp stricmp
#endif

#endif /*  __UDUNITS2_DLL_H_LOADED__ */
