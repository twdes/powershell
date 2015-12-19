

Write-Progress -Id 1 -Activity "ActivityTest" -Status "Status" -CurrentOperation "CurrentOperation";

for($i = 1; $i -le 100; $i++) {
	Write-Progress -Id 1 -Activity "ActivityTest" -Status "StatusTest" -CurrentOperation "CurrentOperationTest" -PercentComplete $i;
	Start-Sleep -Milliseconds 50;
}

Write-Progress -Id 1 -Activity "ActivityTest" -Completed

# Activity
#   Status
#   [Percent]
#   CurrentOperation