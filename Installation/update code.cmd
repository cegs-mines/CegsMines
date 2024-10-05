@echo off
if [%*]==[] %0 "C:\Data\Source\DevStudio\CegsMines\bin\Release"
C:
CD "\Programs\Aeon Laboratories\CegsMines"
copy "%*\*.exe" > nul
copy "%*\*.dll" > nul
copy "%*\*.pdb" > nul
copy "%*\*.deps.json" > nul
copy "%*\*.runtimeconfig.json" > nul
echo *** System software updated *** >> "log\Event log.txt"
exit
