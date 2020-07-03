ls *.csv | % {
    $result = (C:\git\collider\Tests\bin\Release\Tests.exe -i $_.FullName) | Out-String
    $result = $result.Replace("`n", "")
    Write-Output "$($_.Name)`t$result"
    }
