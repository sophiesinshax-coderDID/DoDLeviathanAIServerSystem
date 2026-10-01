@echo off
taskkill f im python.exe t nul 2&1
taskkill f im node.exe t nul 2&1
taskkill f im ollama.exe t nul 2&1
echo Done! All zombie tasks cleared.
timeout t 3