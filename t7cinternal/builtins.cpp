#include "builtins.h"
#include "offsets.h"
#include "detours.h"

std::unordered_map<int, void*> GSCBuiltins::CustomFunctions;
tScrVm_GetString GSCBuiltins::ScrVm_GetString;
tScrVm_GetInt GSCBuiltins::ScrVm_GetInt;
tScrVar_AllocVariableInternal GSCBuiltins::ScrVar_AllocVariableInternal;
tScrVm_GetFunc GSCBuiltins::ScrVm_GetFunc;

// Convert macros to constexpr
static constexpr size_t MEM_SCRVAR_COUNT = 130000;
static constexpr size_t MEM_SCRVAR_CSC_COUNT = 65000;
static constexpr size_t MEM_SCRVAR_SPACE(bool inst) {
    return sizeof(ScrVar_t) * (inst ? MEM_SCRVAR_CSC_COUNT : MEM_SCRVAR_COUNT);
}

void Scr_Error(uint32_t inst, const char* error, uint8_t force_terminal)
{
    if (!error) return;

    if (IS_WINSTORE)
    {
        ((void(__fastcall*)(uint32_t, const char*))REBASE(NULL, 0x1392DF0))(inst, error);
        if (REBASE(NULL, 0x3F66B50))
        {
            *((uint8_t*)REBASE(NULL, 0x3F66B50) + 0x8A40llu * inst + 43) = force_terminal;
        }
        ((void(__fastcall*)(uint32_t))REBASE(NULL, 0x138E030))(inst);
        return;
    }
    ((void(__fastcall*)(uint32_t, const char*, uint32_t))REBASE(0x12EA430, NULL))(inst, error, force_terminal);
}

uint32_t Scr_GetType(uint32_t inst, uint32_t index)
{
    static char err_buff[256]{ 0 };
    uint64_t v3 = 0x8A40llu * inst;

    if (index < *(uint32_t*)(REBASE(0x51A3840, 0x3F66B50) + v3 + 56))
        return *(uint32_t*)(*(uint64_t*)(REBASE(0x51A3840, 0x3F66B50) + v3 + 32) - 16llu * index + 8);

    sprintf_s(err_buff, "parameter %d does not exist", index + 1);
    Scr_Error(inst, err_buff, false);
    return 0;
}

void Scr_AddInt(int scriptInst, uint32_t val)
{
    if (IS_WINSTORE)
    {
        ((void(__fastcall*)(uint32_t))REBASE(NULL, 0x1390370))(scriptInst);
        *((uint32_t*)(*(uint64_t*)REBASE(NULL, 0x3F66B70)) + 2) = 7;
        *(uint32_t*)(*(uint64_t*)REBASE(NULL, 0x3F66B70)) = val;
        return;
    }
    ((void(__fastcall*)(int, __int32))REBASE(0x12E9870, NULL))(scriptInst, val);
}

void GSCBuiltins::Generate()
{
    // Compiler related functions //
    AddCustomFunction("detour", GSCBuiltins::GScr_detour);
    AddCustomFunction("relinkdetours", GSCBuiltins::GScr_relinkDetours);

    // General purpose //
    AddCustomFunction("livesplit", GSCBuiltins::GScr_livesplit);
    AddCustomFunction("nprintln", GSCBuiltins::GScr_nprintln);
    AddCustomFunction("patchbyte", GSCBuiltins::GScr_patchbyte);
    AddCustomFunction("setmempoolsize", GSCBuiltins::GScr_setmempool);
    AddCustomFunction("debugallocvariables", GSCBuiltins::GScr_debugallocvariables);
    AddCustomFunction("script_detour", GSCBuiltins::GScr_runtimedetour);
    AddCustomFunction("erasefunc", GSCBuiltins::GScr_erasefunc);
    AddCustomFunction("abort", GSCBuiltins::GScr_abort);
    AddCustomFunction("catch_exit", GSCBuiltins::GScr_catch_exit);
    AddCustomFunction("enableonlinematch", GSCBuiltins::GScr_enableonlinematch);
}

void GSCBuiltins::Init()
{
    GSCBuiltins::Generate();
    auto builtinFunction = (BuiltinFunctionDef*)OFF_IsProfileBuild;
    builtinFunction->max_args = 255;
    builtinFunction->actionFunc = GSCBuiltins::Exec;

    builtinFunction = (BuiltinFunctionDef*)OFF_BID_Scr_CastInt;
    builtinFunction->actionFunc = GSCBuiltins::Scr_CastInt_Wrapper;

    ScrVm_GetString = (tScrVm_GetString)OFF_ScrVm_GetString;
    ScrVm_GetInt = (tScrVm_GetInt)OFF_ScrVm_GetInt;
    ScrVar_AllocVariableInternal = (tScrVar_AllocVariableInternal)OFF_ScrVar_AllocVariableInternal;
    ScrVm_GetFunc = (tScrVm_GetFunc)OFF_ScrVm_GetFunc;
}

void GSCBuiltins::AddCustomFunction(const char* name, void* funcPtr)
{
    CustomFunctions[fnv1a(name)] = funcPtr;
}

EXPORT void AddCustomFunction(const char* name, void* funcPtr)
{
    GSCBuiltins::AddCustomFunction(name, funcPtr);
}

void GSCBuiltins::Exec(int scriptInst)
{
    INT32 func = static_cast<INT32>(ScrVm_GetInt(scriptInst, 0));
    if (CustomFunctions.find(func) == CustomFunctions.end())
    {
        nlog("unknown builtin %h", func);
        return;
    }
    reinterpret_cast<void(__fastcall*)(int)>(CustomFunctions[func])(scriptInst);
}

void GSCBuiltins::Scr_CastInt_Wrapper(int scriptInst)
{
    auto type = Scr_GetType(scriptInst, 0);
    if (type == 5) // hash
    {
        Scr_AddInt(scriptInst, static_cast<__int32>(ScrVm_GetInt(scriptInst, 0)));
        return;
    }
    ((void(__fastcall*)(int))OFF_Scr_CastInt)(scriptInst);
}

void GSCBuiltins::GScr_nprintln(int scriptInst)
{
    nlog("%s", ScrVm_GetString(0, 1));
}

void GSCBuiltins::GScr_detour(int scriptInst)
{
    if (scriptInst) return;
    ScriptDetours::DetoursEnabled = true;
}

void GSCBuiltins::GScr_relinkDetours(int scriptInst)
{
    if (scriptInst) return;
    ScriptDetours::LinkDetours();
}

void GSCBuiltins::GScr_livesplit(int scriptInst)
{
    if (scriptInst) return;

    HANDLE livesplit = CreateFile("\\\\.\\pipe\\LiveSplit", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_WRITE, NULL, OPEN_EXISTING, 0, NULL);
    if (!livesplit) return;

    const char* message = ScrVm_GetString(0, 1);
    WriteFile(livesplit, message, static_cast<DWORD>(strlen(message)), nullptr, NULL);
    CloseHandle(livesplit);
}

void GSCBuiltins::GScr_patchbyte(int scriptInst)
{
    char* str_file = ScrVm_GetString(0, 1);
    int n_offset = static_cast<int>(ScrVm_GetInt(0, 2));
    int n_value = static_cast<int>(ScrVm_GetInt(0, 3));

    if (!str_file || !n_offset) return;

    auto asset = ScriptDetours::FindScriptParsetree(str_file);
    if (!asset) return;
    
    auto buffer = *(char**)(asset + 0x10);
    if (!buffer) return;

    *(BYTE*)(buffer + n_offset) = static_cast<BYTE>(n_value);
}

void GSCBuiltins::GScr_erasefunc(int scriptInst)
{
    char* str_file = ScrVm_GetString(0, 1);
    int n_namespace = static_cast<int>(ScrVm_GetInt(0, 2));
    int n_func = static_cast<int>(ScrVm_GetInt(0, 3));

    if (!str_file || !n_namespace || !n_func) return;

    auto asset = ScriptDetours::FindScriptParsetree(str_file);
    if (!asset) return;
    
    auto buffer = *(char**)(asset + 0x10);
    if (!buffer) return;

    auto exportsOffset = *(INT32*)(buffer + 0x20);
    auto exports = (INT64)(exportsOffset + buffer);
    auto numExports = *(INT16*)(buffer + 0x3A);
    __t7export* currentExport = (__t7export*)exports;
    bool b_found = false;
    
    for (INT16 i = 0; i < numExports; i++, currentExport++)
    {
        if (currentExport->funcName != n_func) continue;
        if (currentExport->funcNS != n_namespace) continue;
        b_found = true;
        break;
    }

    if (!b_found) return;

    INT32 target = currentExport->bytecodeOffset;
    char* fPos = buffer + currentExport->bytecodeOffset;

    b_found = false;
    currentExport = (__t7export*)exports;
    __t7export* lowest = NULL;
    
    for (INT16 i = 0; i < numExports; i++, currentExport++)
    {
        if (currentExport->bytecodeOffset <= target) continue;
        if (!lowest || (currentExport->bytecodeOffset < lowest->bytecodeOffset))
        {
            lowest = currentExport;
            b_found = true;
        }
    }

    auto code = *(UINT16*)fPos;
    if (code == 0xD || code == 0x200D)
    {
        fPos += 2;
    }
    else
    {
        fPos += 2;
        BYTE numParams = *(BYTE*)fPos;
        fPos += 2;
        for (BYTE i = 0; i < numParams; i++)
        {
            fPos = (char*)((INT64)fPos + 3 & 0xFFFFFFFFFFFFFFFCLL) + 4;
            fPos += 1;
        }
        if ((INT64)fPos & 1)
        {
            fPos++;
        }
    }

    char* fStart = fPos;
    char* fEnd = b_found ? (lowest->bytecodeOffset + buffer) : (fStart + 2);

    while (fStart < fEnd)
    {
        *(UINT16*)fStart = 0x10;
        fStart += 2;
    }
}

char* newVarMemPool = NULL;
void GSCBuiltins::GScr_setmempool(int scriptInst)
{
    UINT64* llpScrVarMemPool = (UINT64*)((char*)OFF_ScrVarGlob + 128 + (scriptInst << 8));
    if (*llpScrVarMemPool == (UINT64)newVarMemPool) return;

    // Get and validate requested size
    const int requestedBytes = static_cast<int>(ScrVm_GetInt(scriptInst, 1));
    if (requestedBytes <= 0) {
        nlog("Invalid memory pool size requested");
        return;
    }

    // Calculate properly aligned size
    constexpr size_t varSize = sizeof(ScrVar_t);
    size_t numBytes = static_cast<size_t>(requestedBytes);
    if (numBytes % varSize != 0) {
        numBytes += varSize - (numBytes % varSize);
    }

    // Ensure minimum size
    const size_t minSize = MEM_SCRVAR_SPACE(scriptInst);
    numBytes = max(numBytes, minSize);

    // Allocate new memory pool
    void* oldPool = newVarMemPool;
    newVarMemPool = (char*)_aligned_malloc(numBytes, 128);
    if (!newVarMemPool) {
        nlog("Failed to allocate %zu bytes for memory pool", numBytes);
        return;
    }

    // Calculate element counts
    const size_t originalCount = minSize / varSize;
    const size_t newCount = numBytes / varSize;

    // Initialize memory and copy existing data
    memset(newVarMemPool, 0, numBytes);
    memcpy(newVarMemPool, (void*)*llpScrVarMemPool, min(minSize, numBytes));

    // Safe pointer access with bounds checking
    if (numBytes >= sizeof(ScrVar_t)) // Ensure we have space for at least one element
    {
        ScrVar_t* currentRef = reinterpret_cast<ScrVar_t*>(newVarMemPool);

        // Mark last original variable as free
        if (originalCount > 0 && originalCount <= newCount) {
            currentRef[originalCount - 1].value.type = VAR_FREE;
            currentRef[originalCount - 1].o.size = static_cast<unsigned int>(originalCount);
        }

        // Initialize new variables
        if (newCount > originalCount) {
            for (size_t i = originalCount; i < newCount; i++) {
                if (i * sizeof(ScrVar_t) < numBytes) { // Explicit bounds check
                    currentRef[i].value.type = VAR_FREE;
                    currentRef[i].o.size = static_cast<unsigned int>(i + 1);
                }
            }
        }

        // Set termination marker
        if (newCount > 0 && newCount * sizeof(ScrVar_t) <= numBytes) {
            currentRef[newCount - 1].o.size = 0;
        }
    }

    // Commit the new memory pool
    *llpScrVarMemPool = (INT64)newVarMemPool;
}

void GSCBuiltins::GScr_debugallocvariables(int scriptInst)
{
    int numVariables = static_cast<int>(ScrVm_GetInt(scriptInst, 1));
    int varIndex = 0;

    ScrVar_t* variables = (ScrVar_t*)*(INT64*)((char*)OFF_ScrVarGlob + 128 + (scriptInst << 8));
    for (int i = 0; i < numVariables; i++)
    {
        varIndex = ScrVar_AllocVariableInternal(scriptInst, 1, 0, varIndex);
        variables[varIndex].value.type = VAR_UNDEFINED;
    }
}

void GSCBuiltins::GScr_runtimedetour(int scriptInst)
{
    char* str_file = ScrVm_GetString(scriptInst, 1);
    int n_namespace = static_cast<int>(ScrVm_GetInt(scriptInst, 2));
    int n_func = static_cast<int>(ScrVm_GetInt(scriptInst, 3));
    auto funcHandle = static_cast<INT64>(ScrVm_GetFunc(scriptInst, 4));

    if (!str_file || !n_namespace || !n_func || !funcHandle) return;

    auto asset = ScriptDetours::FindScriptParsetree(str_file);
    if (!asset) return;
    
    auto buffer = *(char**)(asset + 0x10);
    if (!buffer) return;

    auto exportsOffset = *(INT32*)(buffer + 0x20);
    auto exports = (INT64)(exportsOffset + buffer);
    auto numExports = *(INT16*)(buffer + 0x3A);
    __t7export* currentExport = (__t7export*)exports;
    bool b_found = false;
    
    for (INT16 i = 0; i < numExports; i++, currentExport++)
    {
        if (currentExport->funcName != n_func) continue;
        if (currentExport->funcNS != n_namespace) continue;
        b_found = true;
        break;
    }

    char* fPos = buffer + currentExport->bytecodeOffset;
    ScriptDetours::RegisterRuntimeDetour(funcHandle, n_func, n_namespace, str_file, b_found ? fPos : NULL);
}

void GSCBuiltins::GScr_enableonlinematch(int scriptInst)
{
    *(int32_t*)PTR_sSessionModeState = (*(int32_t*)PTR_sSessionModeState & ~(1 << 14));
}

void GSCBuiltins::GScr_catch_exit(int scriptInst)
{
    if (IS_WINSTORE) return;
    *(__int16*)GSCR_FASTEXIT = 0x6;
}

void GSCBuiltins::GScr_abort(int scriptInst)
{
    ((void(__fastcall*)())REBASE(0, 0))();
}

void GSCBuiltins::nlog(const char* str, ...)
{
    va_list ap;
    HWND notepad, edit;
    char buf[256] = { 0 };

    va_start(ap, str);
    vsprintf_s(buf, sizeof(buf) - 1, str, ap);
    va_end(ap);

    strncat_s(buf, sizeof(buf), "\r\n", _TRUNCATE);
    notepad = FindWindow(NULL, "Untitled - Notepad");
    if (!notepad)
    {
        notepad = FindWindow(NULL, "*Untitled - Notepad");
    }
    if (notepad)
    {
        edit = FindWindowEx(notepad, NULL, "EDIT", NULL);
        if (edit)
        {
            SendMessage(edit, EM_REPLACESEL, TRUE, (LPARAM)buf);
        }
    }
}