
Write-Host "Hello Host...";
Write-Debug "Hello Debug...";
Write-Verbose "Hello Verbose...";

Write-Warning "Hello Warning...";

Write-Progress -Id 1 -Activity "Aktivität" -CurrentOperation "Test..." 
Write-Progress -Id 1 -Activity "Aktivität" -CurrentOperation "Test..." -Completed