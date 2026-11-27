@echo off
rem Puente NovaCore <-> lector BBVA. Copie el .jar del SDK de BBVA en lib\
cd /d "%~dp0"
if not exist puente.properties copy puente.properties.ejemplo puente.properties >nul
java -cp "puente-bbva.jar;lib\*" mx.novacore.puente.Main .
pause
