netsh wlan add profile filename=win-8-act.xml
IF %ERRORLEVEL% NEQ 0 EXIT 1
netsh wlan add profile filename=hpdoa-test.xml
IF %ERRORLEVEL% NEQ 0 EXIT 2