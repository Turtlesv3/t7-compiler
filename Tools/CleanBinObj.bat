@echo off
setlocal enabledelayedexpansion

echo ========================================
echo Clean Bin and Obj Folders
echo ========================================
echo.

REM Find .sln file - first search forwards (down), then backwards (up)
set "SOLUTION_FILE="
set "CURRENT_DIR=%CD%"

REM Search forwards (down) from current directory
echo Searching for .sln file (forwards)...
for /r "%CURRENT_DIR%" %%F in (*.sln) do (
    if not defined SOLUTION_FILE (
        set "SOLUTION_FILE=%%F"
        echo Found solution: %%F
        goto :FOUND
    )
)

REM If not found forwards, search backwards (up the directory tree)
echo Not found forwards, searching backwards...
set "SEARCH_DIR=%CURRENT_DIR%"
set /a LEVEL=0
:SEARCH_UP
set /a LEVEL+=1
if !LEVEL! gtr 20 goto :NOT_FOUND

if exist "!SEARCH_DIR!\*.sln" (
    for %%F in ("!SEARCH_DIR!\*.sln") do (
        set "SOLUTION_FILE=%%F"
        echo Found solution: %%F
        goto :FOUND
    )
)

REM Move up one level
for %%P in ("!SEARCH_DIR!\..") do set "PARENT_DIR=%%~fP"
if not defined PARENT_DIR goto :NOT_FOUND
if "!PARENT_DIR!"=="!SEARCH_DIR!" goto :NOT_FOUND
set "SEARCH_DIR=!PARENT_DIR!"
goto :SEARCH_UP

:NOT_FOUND
echo.
echo ERROR: No .sln file found!
echo Please run this script from within or near a solution directory.
pause
exit /b 1

:FOUND
REM Get solution directory
for %%F in ("!SOLUTION_FILE!") do set "SOLUTION_DIR=%%~dpF"
REM Remove trailing backslash
if "!SOLUTION_DIR:~-1!"=="\" set "SOLUTION_DIR=!SOLUTION_DIR:~0,-1!"

echo Solution directory: !SOLUTION_DIR!
echo.
echo WARNING: This will delete ALL 'bin', 'obj', 'Debug', 'Development', 'Release', 'x64', 'x32', and 'Win32' folders in: !SOLUTION_DIR!
echo This action cannot be undone!
echo.
set /p CONFIRM="Are you sure you want to continue? (Y/N): "

if /i not "!CONFIRM!"=="Y" (
    echo Operation cancelled.
    pause
    exit /b
)

echo.
echo Scanning for bin, obj, Debug, Development, Release, x64, x32, and Win32 folders...
echo.

set COUNT=0
pushd "!SOLUTION_DIR!" 2>nul
if errorlevel 1 (
    echo ERROR: Could not access directory: !SOLUTION_DIR!
    pause
    exit /b 1
)

REM List all bin and obj folders
for /d /r %%F in (bin obj) do (
    if exist "%%F" (
        set /a COUNT+=1
        for %%C in (!COUNT!) do echo [%%C] "%%F"
    )
)

REM List all Debug folders
for /d /r %%F in (Debug) do (
    if exist "%%F" (
        set /a COUNT+=1
        for %%C in (!COUNT!) do echo [%%C] "%%F"
    )
)

REM List all Development folders
for /d /r %%F in (Development) do (
    if exist "%%F" (
        set /a COUNT+=1
        for %%C in (!COUNT!) do echo [%%C] "%%F"
    )
)

REM List all Release folders
for /d /r %%F in (Release) do (
    if exist "%%F" (
        set /a COUNT+=1
        for %%C in (!COUNT!) do echo [%%C] "%%F"
    )
)

REM List all x64 folders
for /d /r %%F in (x64) do (
    if exist "%%F" (
        set /a COUNT+=1
        for %%C in (!COUNT!) do echo [%%C] "%%F"
    )
)

REM List all x32 folders
for /d /r %%F in (x32) do (
    if exist "%%F" (
        set /a COUNT+=1
        for %%C in (!COUNT!) do echo [%%C] "%%F"
    )
)

REM List all Win32 folders
for /d /r %%F in (Win32) do (
    if exist "%%F" (
        set /a COUNT+=1
        for %%C in (!COUNT!) do echo [%%C] "%%F"
    )
)

if !COUNT! equ 0 (
    echo No build folders found: bin, obj, Debug, Development, Release, x64, x32, Win32
    popd
    pause
    exit /b
)

echo.
echo Total folders found: !COUNT!
echo.
set /p CONFIRM2="Proceed with deletion? (Y/N): "

if /i not "!CONFIRM2!"=="Y" (
    echo Operation cancelled.
    popd
    pause
    exit /b
)

echo.
echo Deleting folders...
echo.

set DELETED=0
set FAILED=0

REM Delete all bin and obj folders
for /d /r %%F in (bin obj) do (
    if exist "%%F" (
        echo Deleting: "%%F"
        rd /s /q "%%F" 2>nul
        if not exist "%%F" (
            echo   [OK] Deleted successfully
            set /a DELETED+=1
        ) else (
            echo   [FAILED] Could not delete (may be in use or locked)
            set /a FAILED+=1
        )
    )
)

REM Delete all Debug folders
for /d /r %%F in (Debug) do (
    if exist "%%F" (
        echo Deleting: "%%F"
        rd /s /q "%%F" 2>nul
        if not exist "%%F" (
            echo   [OK] Deleted successfully
            set /a DELETED+=1
        ) else (
            echo   [FAILED] Could not delete (may be in use or locked)
            set /a FAILED+=1
        )
    )
)

REM Delete all Development folders
for /d /r %%F in (Development) do (
    if exist "%%F" (
        echo Deleting: "%%F"
        rd /s /q "%%F" 2>nul
        if not exist "%%F" (
            echo   [OK] Deleted successfully
            set /a DELETED+=1
        ) else (
            echo   [FAILED] Could not delete (may be in use or locked)
            set /a FAILED+=1
        )
    )
)

REM Delete all Release folders
for /d /r %%F in (Release) do (
    if exist "%%F" (
        echo Deleting: "%%F"
        rd /s /q "%%F" 2>nul
        if not exist "%%F" (
            echo   [OK] Deleted successfully
            set /a DELETED+=1
        ) else (
            echo   [FAILED] Could not delete (may be in use or locked)
            set /a FAILED+=1
        )
    )
)

REM Delete all x64 folders
for /d /r %%F in (x64) do (
    if exist "%%F" (
        echo Deleting: "%%F"
        rd /s /q "%%F" 2>nul
        if not exist "%%F" (
            echo   [OK] Deleted successfully
            set /a DELETED+=1
        ) else (
            echo   [FAILED] Could not delete (may be in use or locked)
            set /a FAILED+=1
        )
    )
)

REM Delete all x32 folders
for /d /r %%F in (x32) do (
    if exist "%%F" (
        echo Deleting: "%%F"
        rd /s /q "%%F" 2>nul
        if not exist "%%F" (
            echo   [OK] Deleted successfully
            set /a DELETED+=1
        ) else (
            echo   [FAILED] Could not delete (may be in use or locked)
            set /a FAILED+=1
        )
    )
)

REM Delete all Win32 folders
for /d /r %%F in (Win32) do (
    if exist "%%F" (
        echo Deleting: "%%F"
        rd /s /q "%%F" 2>nul
        if not exist "%%F" (
            echo   [OK] Deleted successfully
            set /a DELETED+=1
        ) else (
            echo   [FAILED] Could not delete (may be in use or locked)
            set /a FAILED+=1
        )
    )
)

popd

echo.
echo ========================================
echo Cleanup Complete
echo ========================================
echo Total folders found: !COUNT!
echo Successfully deleted: !DELETED!
if !FAILED! gtr 0 (
    echo Failed to delete: !FAILED!
    echo.
    echo Note: Some folders may be locked by Visual Studio or other processes.
    echo Close Visual Studio and try again if needed.
)
echo.
pause
