#include "framework.h"
#include "detours.h"
#include "offsets.h"
#include "builtins.h"

// Note: Some auto-exec scripts will not get detoured due to the way linking works in the game

struct ReadScriptDetour
{
	INT32 FixupName;
	INT32 ReplaceNamespace;
	INT32 ReplaceFunction;
	INT32 FixupOffset;
	INT32 FixupSize;
};

tScr_GetFunction ScriptDetours::Scr_GetFunction = NULL;
tScr_GetMethod ScriptDetours::Scr_GetMethod = NULL;
tDB_FindXAssetHeader ScriptDetours::DB_FindXAssetHeader = NULL;
tScr_GscObjLink ScriptDetours::Scr_GscObjLink = NULL;
char* ScriptDetours::GSC_OBJ = NULL;

std::vector<ScriptDetour*> ScriptDetours::RegisteredDetours;
std::unordered_map<INT64, ScriptDetour*> ScriptDetours::LinkedDetours;
std::unordered_map<INT64*, INT64> ScriptDetours::AppliedFixups;
tVM_Opcode ScriptDetours::VM_OP_GetFunction_Old = NULL;
tVM_Opcode ScriptDetours::VM_OP_GetAPIFunction_Old = NULL;
tVM_Opcode ScriptDetours::VM_OP_ScriptFunctionCall_Old = NULL;
tVM_Opcode ScriptDetours::VM_OP_ScriptMethodCall_Old = NULL;
tVM_Opcode ScriptDetours::VM_OP_ScriptThreadCall_Old = NULL;
tVM_Opcode ScriptDetours::VM_OP_ScriptMethodThreadCall_Old = NULL;
tVM_Opcode ScriptDetours::VM_OP_CallBuiltin_Old = NULL;
tVM_Opcode ScriptDetours::VM_OP_CallBuiltinMethod_Old = NULL;
bool ScriptDetours::DetoursLinked = false;
bool ScriptDetours::DetoursReset = true;
bool ScriptDetours::DetoursEnabled = false;
bool ScriptDetours::DetoursInitialized = false;

EXPORT void ResetDetours()
{
	if (!ScriptDetours::DetoursInitialized)
	{
		return;
	}
#ifdef DETOUR_LOGGING
	GSCBuiltins::nlog("Resetting detours...");
#endif
	for (auto it = ScriptDetours::AppliedFixups.begin(); it != ScriptDetours::AppliedFixups.end(); it++)
	{
		*it->first = it->second;
	}
	ScriptDetours::AppliedFixups.clear();
	ScriptDetours::DetoursReset = true;
	ScriptDetours::DetoursLinked = false;
	ScriptDetours::DetoursEnabled = false;
}

EXPORT void RemoveDetours()
{
	if (!ScriptDetours::DetoursInitialized)
	{
		return;
	}
#ifdef DETOUR_LOGGING
	GSCBuiltins::nlog("Removing detours...");
#endif
	for (auto it = ScriptDetours::RegisteredDetours.begin(); it != ScriptDetours::RegisteredDetours.end(); it++)
	{
		free(*it);
	}
	ResetDetours();
	ScriptDetours::RegisteredDetours.clear();
	ScriptDetours::DetoursLinked = false;
}

EXPORT bool RegisterDetours(void* DetourData, int NumDetours, INT64 scriptOffset)
{
	if (!ScriptDetours::DetoursInitialized)
	{
		return true;
	}
	RemoveDetours();
	ScriptDetours::GSC_OBJ = (char*)scriptOffset;
	
#ifdef DETOUR_LOGGING
	GSCBuiltins::nlog("Registering %d detours in script %p...", NumDetours, scriptOffset);
#endif

	INT64 base = (INT64)DetourData;
	for (int i = 0; i < NumDetours; i++)
	{
		ReadScriptDetour* read_detour = (ReadScriptDetour*)(base + (i * 256));
		ScriptDetour* detour = new ScriptDetour();
		detour->hFixup = read_detour->FixupOffset + scriptOffset;
		detour->ReplaceFunction = read_detour->ReplaceFunction;
		detour->ReplaceNamespace = read_detour->ReplaceNamespace;
		detour->FixupSize = read_detour->FixupSize;
#ifdef DETOUR_LOGGING
		GSCBuiltins::nlog("Detour Parsed: {FixupName:%x, ReplaceNamespace:%x, ReplaceFunction:%x, FixupOffset:%x, FixupSize:%x} {FixupMin:%p, FixupMax:%p}", read_detour->FixupName, read_detour->ReplaceNamespace, read_detour->ReplaceFunction, read_detour->FixupOffset, read_detour->FixupSize, detour->hFixup, detour->hFixup + detour->FixupSize);
#endif
		detour->ReplaceScriptName = *(INT64*)((INT64)read_detour + sizeof(ReadScriptDetour));
		ScriptDetours::RegisteredDetours.push_back(detour);
	}

	ScriptDetours::DetoursLinked = false;
	return true;
}

void ScriptDetours::InstallHooks()
{
#ifdef DETOUR_LOGGING
	GSCBuiltins::nlog("Installing hooks...");
#endif

	// initialize methods
	Scr_GetFunction = (tScr_GetFunction)OFF_Scr_GetFunction;
	Scr_GetMethod = (tScr_GetMethod)OFF_Scr_GetMethod;
	DB_FindXAssetHeader = (tDB_FindXAssetHeader)OFF_DB_FindXAssetHeader;
	Scr_GscObjLink = (tScr_GscObjLink)OFF_Scr_GscObjLink;

	// opcodes to hook:
	VTableReplace(0x5d8, VM_OP_GetFunction, &VM_OP_GetFunction_Old);
	VTableReplace(0x6f7, VM_OP_GetAPIFunction, &VM_OP_GetAPIFunction_Old);
	VTableReplace(0x75c, VM_OP_ScriptFunctionCall, &VM_OP_ScriptFunctionCall_Old);
	VTableReplace(0x7f2, VM_OP_ScriptMethodCall, &VM_OP_ScriptMethodCall_Old);
	VTableReplace(0x8cd, VM_OP_ScriptThreadCall, &VM_OP_ScriptThreadCall_Old);
	VTableReplace(0xa34, VM_OP_ScriptMethodThreadCall, &VM_OP_ScriptMethodThreadCall_Old);
	VTableReplace(0x00f, VM_OP_CallBuiltin, &VM_OP_CallBuiltin_Old);
	VTableReplace(0x010, VM_OP_CallBuiltinMethod, &VM_OP_CallBuiltinMethod_Old);
	// TODO all the 2 methods (figuring out what the fuck they do too...)

	DetoursInitialized = true;
}

INT64 ScriptDetours::FindScriptParsetree(INT64 name)
{
	SPTEntry* currentSpt = (SPTEntry*)*(INT64*)OFFSET(0x912BBB0);
	INT32 sptCount = *(INT32*)OFFSET(0x912BBB0 + 0x14);
	for (int i = 0; i < sptCount; i++, currentSpt++)
	{
		if (!currentSpt->Name) continue;
		if (!currentSpt->Buffer) continue;
		if (!currentSpt->size) continue;
		if (currentSpt->Name != name) continue;
		return (INT64)currentSpt;
	}
	return 0;
}

void ScriptDetours::LinkDetours()
{
	LinkedDetours.clear();
	for (auto it = RegisteredDetours.begin(); it != RegisteredDetours.end(); it++)
	{
		auto detour = *it;
		if (detour->ReplaceScriptName) // not a builtin
		{
#ifdef DETOUR_LOGGING
			GSCBuiltins::nlog("Linking replacement %x<%p>::%x...", detour->ReplaceNamespace, detour->ReplaceScriptName, detour->ReplaceFunction);
#endif
			// locate the script to replace
			auto asset = FindScriptParsetree(detour->ReplaceScriptName);
			if (!asset)
			{
#ifdef DETOUR_LOGGING
				GSCBuiltins::nlog("Failed to locate %p...", detour->ReplaceScriptName);
#endif
				continue;
			}

#ifdef DETOUR_LOGGING
			GSCBuiltins::nlog("Located xAssetHeader...");
#endif
			// locate the target export to link
			auto buffer = *(char**)(asset + 0x10);
			auto exportsOffset = *(INT32*)(buffer + 0x30);
			auto exports = (INT64)(exportsOffset + buffer);
			auto numExports = *(INT16*)(buffer + 0x1E);
			__t8export* currentExport = (__t8export*)exports;
			for (INT16 i = 0; i < numExports; i++, currentExport++)
			{
				if (currentExport->funcName != detour->ReplaceFunction)
				{
					continue;
				}
				if (currentExport->funcNS != detour->ReplaceNamespace)
				{
					continue;
				}
#ifdef DETOUR_LOGGING
				GSCBuiltins::nlog("Found export at %p!", (INT64)buffer + currentExport->bytecodeOffset);
#endif
				LinkedDetours[(INT64)buffer + currentExport->bytecodeOffset] = detour;
				break;
			}
		}
		else
		{
#ifdef DETOUR_LOGGING
			GSCBuiltins::nlog("Linking replacement for builtin %x...", detour->ReplaceFunction);
#endif
			INT32 discardType;
			INT32 discardMinParams;
			INT32 discardMaxParams;
			auto hReplace = Scr_GetFunction(detour->ReplaceFunction, &discardType, &discardMinParams, &discardMaxParams);
			if (!hReplace)
			{
				hReplace = Scr_GetMethod(detour->ReplaceFunction, &discardType, &discardMinParams, &discardMaxParams);
			}
			if (hReplace)
			{
#ifdef DETOUR_LOGGING
				GSCBuiltins::nlog("Found function definition at %p!", hReplace);
#endif
				LinkedDetours[hReplace] = detour;
			}
		}
	}
	DetoursLinked = true;
}

void ScriptDetours::VTableReplace(INT32 original_code, tVM_Opcode ReplaceFunc, tVM_Opcode* OutOld)
{
	INT64 handler_table = OFF_ScrVm_Opcodes;
	INT64 stub_final = *(INT64*)(handler_table + original_code * 0x8);
	*OutOld = (tVM_Opcode)stub_final;
	for (int i = 0; i < 0x4000; i++)
	{
		if (*(INT64*)(handler_table + (i * 8)) == stub_final)
		{
			*(INT64*)(handler_table + (i * 8)) = (INT64)ReplaceFunc;
		}
	}
}

void ScriptDetours::VM_OP_GetFunction(INT32 inst, INT64* fs_0, INT64 vmc, bool* terminate)
{
	CheckDetour(inst, fs_0);
	VM_OP_GetFunction_Old(inst, fs_0, vmc, terminate);
}

void ScriptDetours::VM_OP_GetAPIFunction(INT32 inst, INT64* fs_0, INT64 vmc, bool* terminate)
{
	CheckDetour(inst, fs_0);
	VM_OP_GetAPIFunction_Old(inst, fs_0, vmc, terminate);
}

void ScriptDetours::VM_OP_ScriptFunctionCall(INT32 inst, INT64* fs_0, INT64 vmc, bool* terminate)
{
	CheckDetour(inst, fs_0, 1);
	VM_OP_ScriptFunctionCall_Old(inst, fs_0, vmc, terminate);
}

void ScriptDetours::VM_OP_ScriptMethodCall(INT32 inst, INT64* fs_0, INT64 vmc, bool* terminate)
{
	CheckDetour(inst, fs_0, 1);
	VM_OP_ScriptMethodCall_Old(inst, fs_0, vmc, terminate);
}

void ScriptDetours::VM_OP_ScriptThreadCall(INT32 inst, INT64* fs_0, INT64 vmc, bool* terminate)
{
	CheckDetour(inst, fs_0, 1);
	VM_OP_ScriptThreadCall_Old(inst, fs_0, vmc, terminate);
}

void ScriptDetours::VM_OP_ScriptMethodThreadCall(INT32 inst, INT64* fs_0, INT64 vmc, bool* terminate)
{
	CheckDetour(inst, fs_0, 1);
	VM_OP_ScriptMethodThreadCall_Old(inst, fs_0, vmc, terminate);
}

void ScriptDetours::VM_OP_CallBuiltin(INT32 inst, INT64* fs_0, INT64 vmc, bool* terminate)
{
	if (CheckDetour(inst, fs_0, 1))
	{
		// spoof opcode to ScriptFunctionCall (because we are no longer calling a builtin)
		*(INT16*)(*fs_0 - 2) = 0x75c;
		VM_OP_ScriptFunctionCall_Old(inst, fs_0, vmc, terminate);
		return;
	}
	VM_OP_CallBuiltin_Old(inst, fs_0, vmc, terminate);
}

void ScriptDetours::VM_OP_CallBuiltinMethod(INT32 inst, INT64* fs_0, INT64 vmc, bool* terminate)
{
	if (CheckDetour(inst, fs_0, 1))
	{
		// spoof opcode to ScriptMethodCall (because we are no longer calling a builtin)
		*(INT16*)(*fs_0 - 2) = 0x7f2;
		VM_OP_ScriptMethodCall_Old(inst, fs_0, vmc, terminate);
		return;
	}
	VM_OP_CallBuiltinMethod_Old(inst, fs_0, vmc, terminate);
}

bool ScriptDetours::CheckDetour(INT32 inst, INT64* fs_0, INT32 offset)
{
	if (!DetoursEnabled)
	{
		return false;
	}
	// detours are not supported in UI level
	if (*(BYTE*)(OFF_s_runningUILevel))
	{
		if (!ScriptDetours::DetoursReset)
		{
			ResetDetours();
		}
		return false;
	}
	if (inst)
	{
		// csc is not supported at this time
		return false;
	}
	bool fixupApplied = false;
	if (!DetoursLinked)
	{
		LinkDetours();
	}
	INT64 ptrval = *(INT64*)((*fs_0 + 7 + offset) & 0xFFFFFFFFFFFFFFF8);
	if (LinkedDetours.find(ptrval) != LinkedDetours.end() && LinkedDetours[ptrval]->hFixup)
	{
		INT64 fs_pos = *fs_0;
		// if pointer is below fixup or above it, the pointer is not within the detour and thus can be fixed up
		if (LinkedDetours[ptrval]->hFixup > fs_pos || ((LinkedDetours[ptrval]->hFixup + LinkedDetours[ptrval]->FixupSize) <= fs_pos))
		{
#ifdef DETOUR_LOGGING
			GSCBuiltins::nlog("Replaced call at %p to fixup %p! Opcode: %x", (INT64)((*fs_0 + 7 + offset) & 0xFFFFFFFFFFFFFFF8), LinkedDetours[ptrval]->hFixup, *(INT16*)(*fs_0 - 2));
#endif
			AppliedFixups[(INT64*)((*fs_0 + 7 + offset) & 0xFFFFFFFFFFFFFFF8)] = ptrval;
			*(INT64*)((*fs_0 + 7 + offset) & 0xFFFFFFFFFFFFFFF8) = LinkedDetours[ptrval]->hFixup;
			DetoursReset = false;
			fixupApplied = true;
		}
	}
	return fixupApplied;
}
