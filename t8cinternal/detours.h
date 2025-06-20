#pragma once
#include "framework.h"
#include <vector>
#include <unordered_map>

struct ScriptDetour
{
	INT64 ReplaceScriptName;
	INT32 ReplaceNamespace;
	INT32 ReplaceFunction;
	INT64 hFixup;
	INT32 FixupSize;
};

struct __t8export
{
	INT32 discardCRC32;
	INT32 bytecodeOffset;
	INT32 funcName;
	INT32 funcNS;
	INT32 funcNS_discard;
	INT32 discardParamsFlagsPad;
};

struct SPTEntry
{
	INT64 Name;
	INT64 pad;
	char* Buffer;
	INT32 size;
	INT32 Unk0;
};

typedef void(__fastcall* tVM_Opcode)(INT32 inst, INT64* fs_0, INT64 vmc, bool* terminate);
typedef INT64(__fastcall* tScr_GetFunction)(INT32 canonID, INT32* type, INT32* min_args, INT32* max_args);
typedef INT64(__fastcall* tScr_GetMethod)(INT32 canonID, INT32* type, INT32* min_args, INT32* max_args);
typedef INT64(__fastcall* tDB_FindXAssetHeader)(int type, char* name, bool errorIfMissing, int waitTime);
typedef INT64(__fastcall* tScr_GscObjLink)(int inst, char* gsc_obj);

#define DETOUR_LOGGING

class ScriptDetours
{
public:
	static std::vector<ScriptDetour*> RegisteredDetours;
	static std::unordered_map<INT64, ScriptDetour*> LinkedDetours;
	static std::unordered_map<INT64*, INT64> AppliedFixups;
	static INT64 FindScriptParsetree(INT64 name);
	static bool DetoursLinked;
	static bool DetoursReset;
	static bool DetoursEnabled;
	static bool DetoursInitialized;
	static char* GSC_OBJ;
	static void InstallHooks();
	static void LinkDetours();

private:
	static void VTableReplace(INT32 sub_offset, tVM_Opcode ReplaceFunc, tVM_Opcode* OutOld);
	static void VM_OP_GetFunction(INT32 inst, INT64* fs_0, INT64 vmc, bool* terminate);
	static void VM_OP_GetAPIFunction(INT32 inst, INT64* fs_0, INT64 vmc, bool* terminate);
	static void VM_OP_ScriptFunctionCall(INT32 inst, INT64* fs_0, INT64 vmc, bool* terminate);
	static void VM_OP_ScriptMethodCall(INT32 inst, INT64* fs_0, INT64 vmc, bool* terminate);
	static void VM_OP_ScriptThreadCall(INT32 inst, INT64* fs_0, INT64 vmc, bool* terminate);
	static void VM_OP_ScriptMethodThreadCall(INT32 inst, INT64* fs_0, INT64 vmc, bool* terminate);
	static void VM_OP_CallBuiltin(INT32 inst, INT64* fs_0, INT64 vmc, bool* terminate);
	static void VM_OP_CallBuiltinMethod(INT32 inst, INT64* fs_0, INT64 vmc, bool* terminate);
	static bool CheckDetour(INT32 inst, INT64* fs_0, INT32 offset = 0);
	static tScr_GetFunction Scr_GetFunction;
	static tScr_GetMethod Scr_GetMethod;
	static tScr_GscObjLink Scr_GscObjLink;
	static tDB_FindXAssetHeader DB_FindXAssetHeader;
	static tVM_Opcode VM_OP_GetFunction_Old;
	static tVM_Opcode VM_OP_GetAPIFunction_Old;
	static tVM_Opcode VM_OP_ScriptFunctionCall_Old;
	static tVM_Opcode VM_OP_ScriptMethodCall_Old;
	static tVM_Opcode VM_OP_ScriptThreadCall_Old;
	static tVM_Opcode VM_OP_ScriptMethodThreadCall_Old;
	static tVM_Opcode VM_OP_CallBuiltin_Old;
	static tVM_Opcode VM_OP_CallBuiltinMethod_Old;
};