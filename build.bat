@echo off
rem Builds Osteon (Release) with the Visual Studio 2022 C++ toolchain. Output: build\Osteon.exe
setlocal
set "VSDIR="
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
rem "call" keeps cmd from mangling the quoted path inside for /f.
rem Visual Studio 2022 (version 17) first: newer versions may be installed without the CMake tools.
if exist "%VSWHERE%" for /f "usebackq tokens=*" %%i in (`call "%VSWHERE%" -latest -version [17.0^,18.0^) -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`) do set "VSDIR=%%i"
if defined VSDIR if not exist "%VSDIR%\VC\Auxiliary\Build\vcvars64.bat" set "VSDIR="
rem Fallback when vswhere is missing or fails: the standard VS 2022 install folders.
if not defined VSDIR for %%e in (Community Professional Enterprise BuildTools) do if exist "%ProgramFiles%\Microsoft Visual Studio\2022\%%e\VC\Auxiliary\Build\vcvars64.bat" set "VSDIR=%ProgramFiles%\Microsoft Visual Studio\2022\%%e"
rem Last resort: the newest Visual Studio with the C++ tools, whatever its version.
if not defined VSDIR if exist "%VSWHERE%" for /f "usebackq tokens=*" %%i in (`call "%VSWHERE%" -latest -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`) do set "VSDIR=%%i"
if not defined VSDIR (echo Visual Studio 2022 with C++ tools not found & exit /b 1)
call "%VSDIR%\VC\Auxiliary\Build\vcvars64.bat" >nul || exit /b 1
where cmake >nul 2>nul || (echo cmake not found after vcvars64 in "%VSDIR%" & exit /b 1)
cmake -S "%~dp0." -B "%~dp0build" -G Ninja -DCMAKE_BUILD_TYPE=Release || exit /b 1
cmake --build "%~dp0build" || exit /b 1
if "%1"=="test" ("%~dp0build\osteon_tests.exe" && "%~dp0build\osteon_editor_tests.exe")
