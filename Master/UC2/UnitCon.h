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
// UnitCon.h : Declaration of the CUnitCon

#pragma once
#include "UC2_i.h"
#include "resource.h"       // main symbols
#include <comsvcs.h>
#include "udunits2.h"



// CUnitCon

class ATL_NO_VTABLE CUnitCon :
	public CComObjectRootEx<CComSingleThreadModel>,
	public CComCoClass<CUnitCon, &CLSID_UnitCon>,
	public IDispatchImpl<IUnitCon, &IID_IUnitCon, &LIBID_UC2Lib, /*wMajor =*/ 1, /*wMinor =*/ 0>
{
private:
	ut_system *d_unitSystem;
public:
	CUnitCon();

	DECLARE_PROTECT_FINAL_CONSTRUCT()

	HRESULT FinalConstruct()
	{
		return S_OK;
	}

	void FinalRelease()
	{
	}

DECLARE_REGISTRY_RESOURCEID(IDR_UNITCON)

DECLARE_NOT_AGGREGATABLE(CUnitCon)

BEGIN_COM_MAP(CUnitCon)
	COM_INTERFACE_ENTRY(IUnitCon)
	COM_INTERFACE_ENTRY(IDispatch)
END_COM_MAP()




// IUnitCon
public:
	STDMETHOD(CheckUnit)(BSTR unitStr, VARIANT_BOOL* isValid);
	STDMETHOD(CheckUnits)(BSTR fromUnit, BSTR toUnit, VARIANT_BOOL* isValidConversion);
	STDMETHOD(ConvertUnits)(DOUBLE value, BSTR currentUnits, BSTR newUnits, DOUBLE* newValue);
};

OBJECT_ENTRY_AUTO(__uuidof(UnitCon), CUnitCon)
