$CATUTIL='D:\Projects\Core20\Vedph\Catutil\Catutil\bin\Debug\netcoreapp3.1\Catutil.exe'
$LOG='D:\Projects\Core20\Vedph\Catutil\Catutil\bin\Debug\netcoreapp3.1\catutil-log*.txt'
$CODIR='c:\users\dfusi\desktop\co'

del $LOG
Write-Host 'CO CONVERSION'
Write-Host '(1) IMPORT CO TEXT (DRY)'
Invoke-Expression "$CATUTIL import-text $CODIR 1_*.xls catullus -d"
cmd /c pause

Write-Host '(2) IMPORT CO TEXT'
Invoke-Expression "$CATUTIL import-text $CODIR 1_*.xls catullus"
cmd /c pause

Write-Host '(3) PARSE-TXT'
Invoke-Expression "$CATUTIL parse-txt catullus $CODIR\items\"
cmd /c pause

Write-Host '(4) BUILD BIBLIO LOOKUP'
Write-Host "$CATUTIL build-biblio ""$CODIR\4_1 Bibliography.xls"" $CODIR"
Invoke-Expression "$CATUTIL build-biblio ""$CODIR\4_1 Bibliography.xls"" $CODIR"
cmd /c pause

Write-Host '(5) PARSE-APP INTO XSLX'
Invoke-Expression "$CATUTIL parse-app catullus $CODIR\ProteusDump.json"
cmd /c pause

del $LOG
Write-Host '(6) PARSE-APP INTO JSON'
Invoke-Expression "$CATUTIL parse-app catullus $CODIR\ProteusSave.json"
copy -Path $LOG -Destination "$CODIR\log-parse-app.txt"
cmd /c pause

del $LOG
Write-Host '(7) IMPORT JSON (DRY)'
Invoke-Expression "$CATUTIL import-json $CODIR\items\ *.json $CODIR\app\ *.json $CODIR\co-profile.json co -d"
cmd /c pause

del $LOG
Write-Host '(8) IMPORT JSON'
Invoke-Expression "$CATUTIL import-json $CODIR\items\ *.json $CODIR\app\ *.json $CODIR\co-profile.json co"
copy -Path $LOG -Destination "$CODIR\log-import-json.txt"
cmd /c pause
