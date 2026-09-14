$SharedScriptPath = Join-Path $PSScriptRoot "..\..\scripts\test_and_cover.ps1"

& $SharedScriptPath -SolutionPath "./Apps/NotificationService/NotificationService.slnx"