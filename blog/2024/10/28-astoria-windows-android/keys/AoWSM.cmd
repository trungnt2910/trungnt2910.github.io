@ECHO OFF

SETLOCAL

SET ContentsToBeAppended=\0AoWSM
FOR /F "tokens=2,3*" %%G IN ('REG QUERY "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Svchost" /v netsvcs') DO SET EntryContents=%%H
SET NewEntryContents=%EntryContents%%ContentsToBeAppended%

REG ADD "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Svchost" /v netsvcs /t REG_MULTI_SZ /d "%NewEntryContents%" /f

ENDLOCAL
