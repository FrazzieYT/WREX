@echo off
echo ========================================
echo  WREX Portable Builder
echo ========================================
echo.

REM Очистка старой сборки
if exist "publish" rmdir /s /q publish

echo [1/3] Сборка проекта...
dotnet publish WREX.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=true -o publish

if errorlevel 1 (
    echo.
    echo ОШИБКА сборки!
    pause
    exit /b 1
)

echo.
echo [2/3] Создание структуры portable...
mkdir "publish\WREX_Data" 2>nul
mkdir "publish\WREX_Data\Logs" 2>nul
mkdir "publish\WREX_Data\Export" 2>nul

echo.
echo [3/3] Создание README...
(
echo WREX - Windows Resource ^& Explorer X
echo ======================================
echo.
echo Портативная версия системного менеджера.
echo.
echo ЗАПУСК:
echo   1. Запустите WREX.exe
echo   2. Для полного функционала - запустите от имени администратора
echo.
echo ДАННЫЕ:
echo   Вся информация хранится в папке WREX_Data рядом с exe.
echo   Можно безопасно копировать на флешку и запускать на любом ПК.
echo.
echo ТРЕБОВАНИЯ:
echo   - Windows 7 SP1 или новее
echo   - .NET Runtime НЕ требуется (встроен в exe)
echo.
echo WinPE:
echo   Работает в среде восстановления Windows.
echo   Для WinRE рекомендуется запуск с правами SYSTEM.
) > "publish\README.txt"

echo.
echo ========================================
echo  ГОТОВО!
echo  Файлы в папке: publish\
echo  Размер: 
for /f "tokens=3" %%a in ('dir publish\WREX.exe ^| find "WREX.exe"') do echo   %%a байт
echo ========================================
echo.
pause