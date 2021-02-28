@echo off
title Custom Crosshair Compiler
csc /out:"CSCustomCrosshair.exe" /win32icon:"favicon.ico" *.cs
7z a -mx "CSCC Portable.zip" CSCustomCrosshair.exe > NUL
7z a -mx "CSCC Portable.zip" crosshair.ini > NUL
7z a -mx "CSCC Portable.zip" crosshair.png > NUL
