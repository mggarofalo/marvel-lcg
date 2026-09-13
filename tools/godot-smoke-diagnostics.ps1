function Test-GodotSmokeDiagnostics {
    param([object[]]$Output)

    return [string]::Join([Environment]::NewLine, $Output) -match "ERROR:"
}
