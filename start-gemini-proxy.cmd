@echo off
setlocal
cd /d "%~dp0"

if "%GEMINI_API_KEY%"=="" (
  echo Please paste your Gemini API key. It will only be used in this window.
  set /p GEMINI_API_KEY=GEMINI_API_KEY: 
)

if "%GEMINI_MODEL%"=="" (
  set GEMINI_MODEL=gemini-2.5-flash
)

node server\gemini-proxy.mjs
pause
