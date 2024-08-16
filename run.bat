set PATH=%PATH%;%cd%\FASM;%cd%\x64dbg\x64;
set INCLUDE=%cd%\FASM\INCLUDE;

:start
set /p command=
%command%
goto start