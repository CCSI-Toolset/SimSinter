// Copyright (c) 2012, Lawrence Livermore National Security, LLC.
// Produced at the Lawrence Livermore National Laboratory
// Written by Thomas Epperly <epperly2@llnl.gov>, James Leek <leek2@llnl.gov>,
//            Brenda Ng <ng30@llnl.gov>, Jeremy Ou <ou3@llnl.gov>, and
//           Charles Tong <tong10@llnl.gov>.
// LLNL-CODE-579212
// All rights reserved.
//   
// This file is part of CCSI Integration and UI Toolkit (CIUT). For
// details, see https://acceleratecarboncapture.org/. Please also read
// the file COPYRIGHT_LLNL.txt that contains the Additional BSD Notice.
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
// 
//   * Redistributions of source code must retain the above copyright
//     notice, this list of conditions and the disclaimer below.
// 
//   * Redistributions in binary form must reproduce the above copyright
//     notice, this list of conditions and the disclaimer (as noted
//     below) in the documentation and/or other materials provided with
//     the distribution.
// 
//   * Neither the name of the LLNS/LLNL nor the names of its
//     contributors may be used to endorse or promote products derived
//     from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL LAWRENCE
// LIVERMORE NATIONAL SECURITY, LLC, THE U.S. DEPARTMENT OF ENERGY OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
// EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
// LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 
// UnitCon.cpp : Implementation of CUnitCon

#include "stdafx.h"
#include "comutil.h"
#include "UnitCon.h"
#include <ctype.h>
// CUnitCon




static wchar_t
s_XMLFilePath[MAX_PATH+1];

#define UDUNITS_DB_FILE L"udunits2.xml"

//A user shouldn't pass a string of all digits (eg. "1234") to UDUnits2
//UDUnits2 will accept to all digit strings, but the behavior is odd so I'm 
//disallowing it.
static bool isValidUnitString(const char* unitString) {

	//I don't want to allow arbitry length strings, and unit string shouldn't be all
	//that long, so MAX_PATH should be long enough
   int stringLen = strnlen(unitString, MAX_PATH);

   if(stringLen >= MAX_PATH) {
     return false;
   }

   for(int ii = 0; ii < stringLen; ++ii) {
     if(!isdigit(unitString[ii])) {
       return true;
     }
   }

   return false;

}

static void
GetInstallDir(void)
{
    int i;
	HKEY hKey;
	s_XMLFilePath[0] = L'\0';
	s_XMLFilePath[MAX_PATH] = L'\0'; // force NUL termination
	if (::RegOpenKeyEx(HKEY_CLASSES_ROOT, _T("CLSID\\{A1B6EBC0-FEB9-43C4-B006-06598D449DFD}\\InprocServer32"),
				0, KEY_QUERY_VALUE, &hKey) == ERROR_SUCCESS)
	{
		DWORD dwKeyDataType;
		DWORD dwDataBufSize = sizeof(s_XMLFilePath)-sizeof(wchar_t);
		if (::RegQueryValueEx(hKey, NULL, NULL, &dwKeyDataType, // /"InprocServer32"
				(LPBYTE) s_XMLFilePath, &dwDataBufSize) == ERROR_SUCCESS)
		{
			switch ( dwKeyDataType )
			{
				case REG_SZ:
					for(i = 0; (i < MAX_PATH) && (s_XMLFilePath[i] != L'\0'); ++i) {
						if (s_XMLFilePath[i] == L'\\') {
							s_XMLFilePath[i] = L'/';
						}
					}
					if ((i >= 7) && (wcscmp(L"UC2.dll",&(s_XMLFilePath[i-7])) == 0) &&
					     (i+5 < MAX_PATH)) {
				        wcscpy_s(&(s_XMLFilePath[i-7]), MAX_PATH-(i-7),
							     UDUNITS_DB_FILE);
					}
					break;
				default:
					s_XMLFilePath[0] = L'\0';
					break;
			}
		}
		::RegCloseKey( hKey );
	}
    
}

CUnitCon::CUnitCon()
{
	ut_set_error_message_handler(ut_ignore);
	GetInstallDir();
	CW2A path(s_XMLFilePath, CP_UTF8);
	d_unitSystem = ut_read_xml(path);
	if (!d_unitSystem) {
      wchar_t *lastSlash = wcsrchr(s_XMLFilePath, L'/');
	  if (lastSlash) {
		  do {
			  --lastSlash;
		  } while ((lastSlash > s_XMLFilePath) && (*lastSlash != L'/'));
		  if (lastSlash > s_XMLFilePath) {
			wcscpy_s(lastSlash + 1, MAX_PATH - (lastSlash - s_XMLFilePath), UDUNITS_DB_FILE);
		  }
		  CW2A second(s_XMLFilePath, CP_UTF8);
		  d_unitSystem = ut_read_xml(second);
	  }
	}
}

STDMETHODIMP CUnitCon::CheckUnit(BSTR unitStr, VARIANT_BOOL* isValid)
{
	// TODO: Add your implementation code here
	_bstr_t str(unitStr);

	//Pre-validate the string by my criteria
	if(!isValidUnitString((const char*)str)) {
		return VARIANT_FALSE;
	}

    ut_unit *uPtr = ut_parse(d_unitSystem, (const char *)str, UT_UTF8);
	if (uPtr) {
		*isValid = VARIANT_TRUE;
		ut_free(uPtr);
	}
	else {
		*isValid = VARIANT_FALSE;
	}

	return S_OK;
}

STDMETHODIMP CUnitCon::CheckUnits(BSTR fromUnit, BSTR toUnit, VARIANT_BOOL* isValidConversion)
{
	// TODO: Add your implementation code here
	CW2A fromStr(_bstr_t(fromUnit), CP_UTF8);
	CW2A toStr(_bstr_t(toUnit), CP_UTF8);
    ut_unit *fromPtr, *toPtr;
	*isValidConversion = VARIANT_FALSE;

	//Pre-validate the strings by my criteria
	if(!isValidUnitString((const char*)fromStr) ||
		!isValidUnitString((const char*)toStr)) {
		return VARIANT_FALSE;
	}


	fromPtr = ut_parse(d_unitSystem, (const char *)fromStr, UT_UTF8);
	if (fromPtr) {
		toPtr = ut_parse(d_unitSystem, (const char *)toStr, UT_UTF8);
		if (toPtr) {
			*isValidConversion = 
				(ut_are_convertible(fromPtr, toPtr) ? VARIANT_TRUE : VARIANT_FALSE);
			ut_free(toPtr);
		}
		ut_free(fromPtr);
	}
	return S_OK;
}

STDMETHODIMP CUnitCon::ConvertUnits(DOUBLE value, BSTR currentUnits, BSTR newUnits, DOUBLE* newValue)
{
	// TODO: Add your implementation code here
	CW2A fromStr(_bstr_t(currentUnits), CP_UTF8);
	CW2A toStr(_bstr_t(newUnits), CP_UTF8);
    ut_unit *fromPtr, *toPtr;
	*newValue = 0;

	//Pre-validate the strings by my criteria
	if(!isValidUnitString((const char*)fromStr) ||
		!isValidUnitString((const char*)toStr)) {
		return E_INVALIDARG;
	}

	fromPtr = ut_parse(d_unitSystem, (const char *)fromStr, UT_UTF8);
	if (fromPtr) {
		toPtr = ut_parse(d_unitSystem, (const char *)toStr, UT_UTF8);
		if (toPtr) {
			cv_converter *cvP = ut_get_converter(fromPtr, toPtr);
			if (cvP) {
				*newValue = cv_convert_double(cvP, value);
				cv_free(cvP);
			}
			ut_free(toPtr);
		}
		ut_free(fromPtr);
	}
	return S_OK;
}
