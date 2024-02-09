// Silence some warnings when using `fopen`
#define _CRT_SECURE_NO_WARNINGS

#include <stdio.h>
#include <vector>
#include <Windows.h>

#include "elf.h"

// Select the correct ELF structures depending on whether the program is 32 or 64 bits.
#ifdef _WIN64
#define ElfW(type) Elf64_##type
#elif defined _WIN32
#define ElfW(type) Elf32_##type
#endif

int main()
{
    // r: Read-only
    // b: Binary mode
    // We have to explicitly specify binary mode, otherwise the OS might do conversions specific to
    // text files that may corrupt our bytes.
    FILE* elfFile = fopen("elf", "rb");

    // Read the ELF header

    ElfW(Ehdr) header;
    fread(&header, sizeof(header), 1, elfFile);

    if (!IS_ELF(header))
    {
        fprintf(stderr, "Not an ELF file.\n");
        return 1;
    }

    std::vector<ElfW(Phdr)> programHeaders(header.e_phnum);
    fread(programHeaders.data(), sizeof(ElfW(Phdr)), header.e_phnum, elfFile);

    // Read the program headers and determine the executable file's address in memory.

    uintptr_t minAddr = -1;
    uintptr_t maxAddr = 0;

    for (const auto& programHeader : programHeaders)
    {
        if (programHeader.p_type != PT_LOAD)
        {
            continue;
        }

        minAddr = min(minAddr, programHeader.p_vaddr);
        maxAddr = max(minAddr, (programHeader.p_vaddr + programHeader.p_memsz));
    }

    // Obtain a Win32 handle to the file

    HANDLE elfFileHandle = CreateFile(L".\\elf",
        GENERIC_READ | GENERIC_EXECUTE,
        FILE_SHARE_READ | FILE_SHARE_WRITE,
        NULL,
        OPEN_EXISTING,
        FILE_ATTRIBUTE_NORMAL,
        NULL);
    
    // Obtain a file mapping object

    HANDLE elfFileMappingHandle = CreateFileMapping(elfFileHandle,
        NULL,
        PAGE_EXECUTE_READ,
        0,
        0,
        NULL);

    // Map the file into memory

    MapViewOfFileEx(
        elfFileMappingHandle,
        FILE_MAP_READ | FILE_MAP_EXECUTE | FILE_MAP_COPY,
        0, 0,
        maxAddr - minAddr,
        (void*)minAddr
    );

    // Set the page protections

    const auto ElfProtectionToWindows = [](ElfW(Word) elfFlags)
    {
        static const int table[2][2][2] =
        {
            // Not executable
            {
                // Not writable
                { PAGE_NOACCESS, PAGE_READONLY },
                // Writable
                { PAGE_WRITECOPY, PAGE_WRITECOPY }
            },
            // Executable
            {
                // Not writable
                { PAGE_EXECUTE, PAGE_EXECUTE_READ },
                // Writable
                { PAGE_EXECUTE_WRITECOPY, PAGE_EXECUTE_WRITECOPY }
            }
        };

        return table[(bool)(elfFlags & PF_X)][(bool)(elfFlags & PF_W)][(bool)(elfFlags & PF_R)];
    };

    for (const auto& programHeader : programHeaders)
    {
        if (programHeader.p_type != PT_LOAD)
        {
            continue;
        }

        unsigned long oldProtect;
        VirtualProtect(
            (void*)programHeader.p_vaddr,
            programHeader.p_memsz,
            ElfProtectionToWindows(programHeader.p_flags),
            &oldProtect
        );
    }

    // Function that takes no parameters and returns an `int`.
    int (*entry)() = (int (*)())header.e_entry;

    return entry();
}
