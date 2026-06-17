@echo off
echo ========================================
echo  WREX Portable Builder
echo ========================================
echo.

if exist "publish" rmdir /s /q publish

echo [1/4] Сборка проекта (win-x64)...
dotnet publish WREX.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=true -p:PublishTrimmed=false -o publish

if errorlevel 1 (
    echo.
    echo ОШИБКА сборки!
    pause
    exit /b 1
)

echo.
echo [2/4] Копирование ресурсов...
copy "icon.ico" "publish\icon.ico" >nul 2>&1

echo.
echo [3/4] Создание структуры portable...
mkdir "publish\WREX_Data" 2>nul
mkdir "publish\WREX_Data\Logs" 2>nul
mkdir "publish\WREX_Data\Export" 2>nul

echo.
echo [4/4] Создание README...
(
echo WREX - Windows Resource ^& Explorer X
echo ======================================
echo.
echo Портативная версия системного менеджера.
echo.
echo ЗАПУСК:
echo   1. Запустите WREX.exe от имени администратора
echo   2. В WinRE запускайте напрямую - уже SYSTEM
echo.
echo АВТОЗАГРУЗКА:
echo   В настройках нажмите "Добавить в автозагрузку"
echo   При старте Windows программа стартует свернутой в трей
echo.
echo ДАННЫЕ:
echo   Вся информация хранится в папке WREX_Data рядом с exe.
echo   Можно безопасно копировать на флешку и запускать на любом ПК.
echo.
echo WINRE:
echo   1. Скопируйте WREX.exe на флешку
echo   2. Загрузитесь в WinRE (среда восстановления)
echo   3. Откройте командную строку
echo   4. Запустите: X:\path\to\WREX.exe
echo   5. Все функции доступны без установки
echo.
echo ТРЕБОВАНИЯ:
echo   - Windows 7 SP1 или новее
echo   - .NET Runtime НЕ требуется (встроен в exe)
echo   - WinRE / WinPE - работает из коробки
echo.
echo РАЗМЕР:
echo   ~60-80 MB (self-contained с .NET 8)
) > "publish\README.txt"

echo.
echo ========================================
echo  ГОТОВО!
echo  Файлы в папке: publish\
echo ========================================
echo.
dir publish\WREX.exe | find "WREX"
echo.
pause
