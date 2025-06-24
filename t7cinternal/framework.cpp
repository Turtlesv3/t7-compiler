#include "framework.h"

#pragma section(".offsets",read,write)
__declspec(allocate(".offsets")) const unsigned char MSELECT[] =
{
    0x48, 0xB8, 0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11,
    0x48, 0x01, 0xC8, // c8 -> d0 for rcx -> rdx
    0xC3
};

// TLS callback implementation
#pragma comment (linker, "/INCLUDE:__tls_used")

// Define the TLS callback array
#pragma section(".CRT$XLY",long,read)
extern "C" __declspec(allocate(".CRT$XLY"))
const PIMAGE_TLS_CALLBACK _tls_callback = tls_callback;

// Define the TLS directory section
#pragma data_seg(".rdata$TLS")
extern "C" __declspec(allocate(".rdata$TLS"))
const IMAGE_TLS_DIRECTORY _tls_used = {
    (ULONGLONG)&_tls_callback,  // Address of callbacks array
    (ULONGLONG)&_tls_callback,  // Address of last callback
    (ULONGLONG)0,               // Size of tls index
    (ULONGLONG)0,               // Size of tls block
    (ULONGLONG)0,               // Characteristics
};
#pragma data_seg()

#pragma optimize("",off)
bool is_tls_initialized = false;
void NTAPI tls_callback(PVOID DllHandle, DWORD dwReason, PVOID)
{
    if (is_tls_initialized || dwReason != DLL_PROCESS_ATTACH)
    {
        return;
    }
    is_tls_initialized = true;

    // Define MSELECT_SIZE here since it's only used in this function
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