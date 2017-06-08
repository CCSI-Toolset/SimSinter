// dllmain.h : Declaration of module class.

class CUC2Module : public CAtlDllModuleT< CUC2Module >
{
public :
	DECLARE_LIBID(LIBID_UC2Lib)
	DECLARE_REGISTRY_APPID_RESOURCEID(IDR_UC2, "{759B5C3F-05EB-4A21-A064-715AB1254C9F}")
};

extern class CUC2Module _AtlModule;
