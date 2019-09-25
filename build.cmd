@echo off
REM dotnet build isn't used because it can't build net35 tfms

msbuild -v:m -restore -t:Build -p:Configuration=Release dnlib.sln || goto :error
msbuild -v:m -t:Pack -p:Configuration=Release dnlib.sln || goto :error

goto :EOF

:error
exit /b %errorlevel%
