@echo off
REM Clean Bin and Obj Folders
REM This script scans for and deletes all 'bin' and 'obj' folders in the current directory and subdirectories

setlocal enabledelayedexpansion

echo ========================================
echo Clean Bin and Obj Folders
echo ========================================
echo.
echo This will delete ALL 'bin' and 'obj' folders in:
echo %CD%
echo.
echo WARNING: This action cannot be undone!
echo.
set /p CONFIRM="Are you sure you want to continue? (Y/N): "

if /i not "%CONFIRM%"=="Y" (
    echo Operation cancelled.
    pause
    exit /b
)

echo.
echo Scanning for bin and obj folders...
echo.

set COUNT=0
set DELETED=0
set FAILED=0

REM Find and delete bin folders
for /d /r %%F in (bin) do (
    if exist "%%F" (
        set /a COUNT+=1
        echo [!COUNT!] Found: %%F
        set /a DELETED+=1
        rd /s /q "%%F" 2>nul
        if !errorlevel! equ 0 (
            echo      Deleted successfully
        ) else (
            echo      FAILED to delete (may be in use or locked)
            set /a FAILED+=1
            set /a DELETED-=1
        )
    )
)

REM Find and delete obj folders
for /d /r %%F in (obj) do (
    if exist "%%F" (
        set /a COUNT+=1
        echo [!COUNT!] Found: %%F
        set /a DELETED+=1
        rd /s /q "%%F" 2>nul
        if !errorlevel! equ 0 (
            echo      Deleted successfully
        ) else (
            echo      FAILED to delete (may be in use or locked)
            set /a FAILED+=1
            set /a DELETED-=1
        )
    )
)

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

