Param(
    [string]$DamsPath = "./dams.json",
    [string]$SiresPath = "./sires.json",
    [string]$OutputPath = "./parents.json",
    [int]$Count = 1600
)

function Read-JsonArray {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        Write-Error "File not found: $Path"
        return @()
    }

    $raw = Get-Content -Raw -ErrorAction Stop $Path
    try {
        $parsed = ConvertFrom-Json $raw -ErrorAction Stop
    } catch {
        Write-Error "Failed to parse JSON in $Path : $_"
        return @()
    }

    # If top-level array
    if ($parsed -is [System.Collections.IEnumerable] -and -not ($parsed -is [string]) -and -not ($parsed -is [System.Management.Automation.PSCustomObject])) {
        return @($parsed)
    }

    # If PSCustomObject: look for common wrapper properties
    $candidates = @('dams','sires','items','horses','data','result')
    foreach ($name in $candidates) {
        if ($parsed.PSObject.Properties.Name -contains $name) {
            $val = $parsed."$name"
            if ($val -is [System.Collections.IEnumerable]) { return @($val) }
        }
    }

    # fallback: first property that is an array
    foreach ($prop in $parsed.PSObject.Properties) {
        if ($prop.Value -is [System.Collections.IEnumerable] -and -not ($prop.Value -is [string])) {
            return @($prop.Value)
        }
    }

    # If the object itself has array-like shape? try to return as single item array
    return @($parsed)
}

function Extract-Ids {
    param([array]$items)

    $ids = [System.Collections.Generic.List[string]]::new()
    foreach ($it in $items) {
        if ($null -eq $it) { continue }
        if ($it -is [string]) {
            $ids.Add($it)
            continue
        }

        # Try common ID property names (PowerShell is case-insensitive)
        $id = $null
        foreach ($p in 'id','Id','ID','guid','Guid') {
            try { $val = $it.$p } catch { $val = $null }
            if ($null -ne $val) { $id = $val; break }
        }

        if ($null -ne $id) {
            $ids.Add([string]$id)
            continue
        }

        # If item is an object with a single property that is a guid/string, take it
        $props = $it.PSObject.Properties
        if ($props.Count -eq 1) {
            $val = $props[0].Value
            if ($val -is [string]) { $ids.Add($val); continue }
        }

        # Otherwise try ToString
        $ids.Add([string]$it)
    }

    return $ids
}

# Read and extract
$damsRaw = Read-JsonArray -Path $DamsPath
$siresRaw = Read-JsonArray -Path $SiresPath

$dams = Extract-Ids -items $damsRaw
$sires = Extract-Ids -items $siresRaw

if ($dams.Count -eq 0 -or $sires.Count -eq 0) {
    Write-Error "No dam or sire IDs found. Dams: $($dams.Count), Sires: $($sires.Count)"
    exit 1
}

$totalPossible = $dams.Count * $sires.Count
if ($totalPossible -lt $Count) {
    Write-Warning "Requested count ($Count) is greater than available unique combinations ($totalPossible). Output will contain all $totalPossible combinations."
    $Count = $totalPossible
}

# Generate all unique combinations systematically
$parents = New-Object System.Collections.Generic.List[object]
$produced = 0
$di = 0
$si = 0

while ($produced -lt $Count) {
    $parent = @{ damId = $dams[$di]; sireId = $sires[$si] }
    $parents.Add($parent)
    $produced++
    
    # Advance sire index, and when it wraps, advance dam index
    $si++
    if ($si -ge $sires.Count) {
        $si = 0
        $di++
        if ($di -ge $dams.Count) {
            $di = 0  # Wrap around if we've exhausted all combinations
        }
    }
}

# Write output
$outputObj = @{ parents = $parents }
# Ensure pretty JSON
$json = $outputObj | ConvertTo-Json -Depth 5
Set-Content -Path $OutputPath -Value $json -Encoding UTF8 -Force

Write-Output "Wrote $produced parent combinations to '$OutputPath'."