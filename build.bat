@echo off
chcp 65001 > nul
setlocal enabledelayedexpansion

echo ========================================================
echo   Travelling at Night Demo - 한국어 패치 빌드 스크립트
echo ========================================================
echo.

set "CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" (
    set "CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)
if not exist "%CSC%" (
    echo [ERROR] .NET Framework C# 컴파일러(csc.exe)를 찾을 수 없습니다.
    pause
    exit /b 1
)

set "STEAM_MANAGED=C:\Program Files (x86)\Steam\steamapps\common\Travelling at Night Demo\travelling_Data\Managed"
if not exist "%STEAM_MANAGED%" (
    echo [WARN] 기본 Steam 설치 경로를 찾을 수 없습니다: %STEAM_MANAGED%
    echo 환경에 맞게 STEAM_MANAGED 경로를 조정해 주세요.
)

set "REPO_ROOT=%~dp0"
set "BIN_DIR=%REPO_ROOT%bin"
if not exist "%BIN_DIR%" mkdir "%BIN_DIR%"

echo [1/4] TravellingKoreanPatch.dll 컴파일 중...
"%CSC%" /nologo /t:library /r:"%STEAM_MANAGED%\netstandard.dll" /r:"%STEAM_MANAGED%\UnityEngine.CoreModule.dll" /r:"%STEAM_MANAGED%\UnityEngine.UI.dll" /r:"%STEAM_MANAGED%\UnityEngine.TextRenderingModule.dll" /r:"%STEAM_MANAGED%\UnityEngine.TextCoreTextEngineModule.dll" /r:"%STEAM_MANAGED%\UnityEngine.TextCoreFontEngineModule.dll" /r:"%STEAM_MANAGED%\Unity.TextMeshPro.dll.bak" /out:"%BIN_DIR%\TravellingKoreanPatch.dll" "%REPO_ROOT%src\TravellingKoreanPatch.cs"
if %errorlevel% neq 0 (
    echo [ERROR] TravellingKoreanPatch.dll 컴파일 실패.
    pause
    exit /b 1
)
echo [PASS] TravellingKoreanPatch.dll 컴파일 완료.

echo.
echo [2/4] MultiInjector.exe 컴파일 중...
"%CSC%" /nologo /t:exe /r:"%REPO_ROOT%lib\Mono.Cecil.dll" /out:"%BIN_DIR%\MultiInjector.exe" "%REPO_ROOT%src\MultiInjector.cs"
if %errorlevel% neq 0 (
    echo [ERROR] MultiInjector.exe 컴파일 실패.
    pause
    exit /b 1
)
echo [PASS] MultiInjector.exe 컴파일 완료.

echo.
echo [3/4] MultiInjector 실행 (게임 어셈블리 패치)...
cd /d "%REPO_ROOT%"
"%BIN_DIR%\MultiInjector.exe" "%STEAM_MANAGED%" "%BIN_DIR%" "%BIN_DIR%"
if %errorlevel% neq 0 (
    echo [ERROR] 어셈블리 인젝션 실패.
    pause
    exit /b 1
)

echo.
echo [4/4] 배포 스크립트 실행 (스팀 폴더 및 배포용 ZIP 생성)...
python "%REPO_ROOT%scripts\deploy.py"

echo.
echo ========================================================
echo   모든 빌드 및 배포 작업이 성공적으로 완료되었습니다!
echo ========================================================
pause
