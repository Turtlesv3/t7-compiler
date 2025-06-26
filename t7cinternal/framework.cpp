#include "framework.h"

#pragma section(".offsets",read,write)
__declspec(allocate(".offsets")) const unsigned char MSELECT[] =
{
    0x48, 0xB8, 0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11,
    0x48, 0x01, 0xC8, // c8 -> d0 for rcx -> rdx
    0xC3
};

// Forward declaration of the TLS callback
extern "C" void NTAPI tls_callback(PVOID DllHandle, DWORD dwReason, PVOID Reserved);

// TLS callback implementation
#pragma comment(linker, "/INCLUDE:__tls_used")

// Define the TLS callback array in the correct CRT section
#pragma section(".CRT$XLB",read)
extern "C" __declspec(allocate(".CRT$XLB"))
const PIMAGE_TLS_CALLBACK _tls_callback = tls_callback;

// TLS directory structure - x64 version
#ifdef _WIN64
#pragma section(".rdata$T",read)
extern "C" __declspec(allocate(".rdata$T"))
const IMAGE_TLS_DIRECTORY64 _tls_used = {
    0,                          // StartAddressOfRawData
    0,                          // EndAddressOfRawData
    0,                          // AddressOfIndex
    (ULONGLONG)&_tls_callback,  // AddressOfCallbacks
    0,                          // SizeOfZeroFill
    0                           // Characteristics
};
#else
// TLS directory structure - x86 version
#pragma section(".rdata$T",read)
extern "C" __declspec(allocate(".rdata$T"))
const IMAGE_TLS_DIRECTORY32 _tls_used = {
    0,                          // StartAddressOfRawData
    0,                          // EndAddressOfRawData
    0,                          // AddressOfIndex
    (ULONG)&_tls_callback,      // AddressOfCallbacks
    0,                          // SizeOfZeroFill
    0                           // Characteristics
};
#endif

#pragma optimize("",off)
bool is_tls_initialized = false;
void NTAPI tls_callback(PVOID DllHandle, DWORD dwReason, PVOID)
{
    if (is_tls_initialized || dwReason != DLL_PROCESS_ATTACH)
    {
        return;
    }
    is_tls_initialized = true;

    constexpr size_t MSELECT_SIZE = 13;
    DWORD oldProtect;
    VirtualProtect((LPVOID)MSELECT, MSELECT_SIZE, PAGE_EXECUTE_READWRITE, &oldProtect);
    auto base_address_game = *(uint64_t*)((uint64_t)(NtCurrentTeb()->ProcessEnvironmentBlock) + 0x10);
    *(uint64_t*)(MSELECT + 2) = base_address_game;

    if (*(uint32_t*)(base_address_game + 0x3C) == 0x1A0) // if we are windows store exe
    {
        *(uint8_t*)(MSELECT + 0xC) = 0xd0; // use rdx
    }
}
#pragma optimize("",on)

void chgmem(__int64 addy, __int32 size, void* copy)
{
    DWORD oldprotect;
    VirtualProtect((void*)addy, size, PAGE_EXECUTE_READWRITE, &oldprotect);
    memcpy((void*)addy, copy, size);
    VirtualProtect((void*)addy, size, oldprotect, &oldprotect);
}