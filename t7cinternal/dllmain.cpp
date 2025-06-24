// dllmain.cpp : Defines the entry point for the DLL application.
#include "framework.h"
#include "builtins.h"
#include "detours.h"
#include "Opcodes.h"
#include "offsets.h"
#include "winternl.h"

BOOL APIENTRY DllMain(HMODULE hModule,
    DWORD  ul_reason_for_call,
    LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        GSCBuiltins::Init();
        ScriptDetours::InstallHooks();
        Opcodes::Init();
        break;
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

#pragma optimize("",off)

namespace {
    // Convert macros to constexpr/inline functions where possible
    constexpr int HOTLOAD_ERROR_BADBUFF = 1;

    inline __int32 SL_GetString_Steam(const char* str, __int32 user, __int32 type) {
        return ((__int32(__fastcall*)(const char*, __int32, __int32))OFFSET_S(0x12D7B20))(str, user, type);
    }

    inline void SL_TransferRefToUser_Steam(__int32 scrStr, __int32 user) {
        ((void(__fastcall*)(__int32, __int32))OFFSET_S(0x12D8C60))(scrStr, user);
    }

    inline void GscObjResolve_Steam(__int32 inst, const char* obj) {
        ((void(__fastcall*)(__int32, const char*, __int32))OFFSET_S(0x12CA2B0))(inst, obj, 0);
    }

    inline __int32 Scr_ExecThread_Steam(__int32 inst, void* func, __int32 pcount, void* val, __int32 self) {
        return ((__int32(__fastcall*)(__int32, void*, __int32, void*, __int32))OFFSET_S(0x12EA770))(inst, func, pcount, val, self);
    }

    inline __int32 Scr_FreeThread_Steam(__int32 inst, __int32 thread) {
        return ((__int32(__fastcall*)(__int32, __int32))OFFSET_S(0x12EAB50))(inst, thread);
    }
}

EXPORT bool HotloadScript_Steam(const char* buff, int vm, int* error)
{
    // note that the only reason we dont call Scr_GscObjLink outright is because we dont want to fill up the vm object info buffer with scripts that are essentially throwaway buffers
    if (*(__int64*)buff != 0x1C000A0D43534780)
    {
        *error = HOTLOAD_ERROR_BADBUFF;
        return false;
    }

    // link strings
    const char* stringsPtr = *(__int32*)(0x18 + buff) + buff;
    unsigned __int16 numStrings = *(unsigned __int16*)(0x38 + buff);

    for (int i = 0; i < numStrings; i++)
    {
        const char* strValue = (*(__int32*)stringsPtr + buff);
        int numEntries = *(char*)(stringsPtr + 4);
        stringsPtr += 8;

        __int32 scrStr = SL_GetString_Steam(strValue, 0, 0x18);
        SL_TransferRefToUser_Steam(scrStr, 1);

        for (int j = 0; j < numEntries; j++)
        {
            *(__int32*)(*(__int32*)stringsPtr + buff) = scrStr;
            stringsPtr += 4;
        }
    }

    // TODO IN THE FUTURE: ANIM LINKAGE

    // link the vm (fast way)
    GscObjResolve_Steam(vm, buff);

    // run autoexec functions
    const char* exportsPtr = *(__int32*)(0x20 + buff) + buff;
    unsigned __int16 numExports = *(unsigned __int16*)(0x3A + buff);

    for (int i = 0; i < numExports; i++)
    {
        char flags = *(exportsPtr + 17);

        if (flags & 0x2) // autoexec
        {
            __int32 thread_id = Scr_ExecThread_Steam(vm, (void*)(*(__int32*)(exportsPtr + 0x4) + buff), 0, 0, 0);
            Scr_FreeThread_Steam(vm, thread_id);
        }

        exportsPtr += 20;
    }

    return true;
}

EXPORT void HotloadScript_Steam_Trail()
{
    HotloadScript_Steam(0, 0, 0); // force the other one to be emitted first
}

namespace {
    // WinStore versions of the functions
    inline __int32 SL_GetStringOfSize_WinStore(const char* str, __int32 user, __int32 len, __int32 type) {
        return ((__int32(__fastcall*)(const char*, __int32, __int32, __int32))OFFSET_S(0x137DAF0))(str, user, len, type);
    }

    inline void GscObjResolve_WinStore(__int32 inst, const char* obj) {
        ((void(__fastcall*)(__int32, const char*, __int32))OFFSET_S(0x136E630))(inst, obj, 0);
    }

    inline __int32 Scr_ExecThread_WinStore(__int32 inst, void* func, __int32 pcount, void* val, __int32 self) {
        return ((__int32(__fastcall*)(__int32, void*, __int32, void*, __int32))OFFSET_S(0x1390700))(inst, func, pcount, val, self);
    }

    inline __int32 Scr_FreeThread_WinStore(__int32 inst, __int32 thread) {
        return ((__int32(__fastcall*)(__int32, __int32))OFFSET_S(0x137F4F0))(inst, thread);
    }
}

EXPORT bool HotloadScript_WinStore(const char* buff, int vm, int* error)
{
    // note that the only reason we dont call Scr_GscObjLink outright is because we dont want to fill up the vm object info buffer with scripts that are essentially throwaway buffers
    if (*(__int64*)buff != 0x1C000A0D43534780)
    {
        *error = HOTLOAD_ERROR_BADBUFF;
        return false;
    }

    // link strings
    const char* stringsPtr = *(__int32*)(0x18 + buff) + buff;
    unsigned __int16 numStrings = *(unsigned __int16*)(0x38 + buff);

    for (int i = 0; i < numStrings; i++)
    {
        const char* strValue = (*(__int32*)stringsPtr + buff);
        int numEntries = *(char*)(stringsPtr + 4);
        stringsPtr += 8;

        uint32_t scrStr = SL_GetStringOfSize_WinStore(strValue, 0, strlen(strValue), 0x18);
        auto v12 = (volatile long*)((uint64_t)OFFSET_S(0x3B1F308) + 28llu * scrStr);
        if (*((unsigned char*)v12 + 2) & 1)
            _InterlockedDecrement(v12);
        else
            _InterlockedOr(v12, 0x10000u);

        for (int j = 0; j < numEntries; j++)
        {
            *(__int32*)(*(__int32*)stringsPtr + buff) = scrStr;
            stringsPtr += 4;
        }
    }

    // TODO IN THE FUTURE: ANIM LINKAGE

    // link the vm (fast way)
    GscObjResolve_WinStore(vm, buff);

    // run autoexec functions
    const char* exportsPtr = *(__int32*)(0x20 + buff) + buff;
    unsigned __int16 numExports = *(unsigned __int16*)(0x3A + buff);

    for (int i = 0; i < numExports; i++)
    {
        char flags = *(exportsPtr + 17);

        if (flags & 0x2) // autoexec
        {
            __int32 thread_id = Scr_ExecThread_WinStore(vm, (void*)(*(__int32*)(exportsPtr + 0x4) + buff), 0, 0, 0);
            Scr_FreeThread_WinStore(vm, thread_id);
        }

        exportsPtr += 20;
    }

    return true;
}

EXPORT void HotloadScript_WinStore_Trail()
{
    HotloadScript_WinStore(0, 0, 0); // force the other one to be emitted first
}
#pragma optimize("",on)