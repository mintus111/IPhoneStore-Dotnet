$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$sqlFilePath = "c:\Users\Laptop\OneDrive\Desktop\donet\IPhoneStore\ReSeed_Data_UTF8.sql"
$scriptContent = [System.IO.File]::ReadAllText($sqlFilePath, [System.Text.Encoding]::UTF8)

# Tách theo lệnh GO
$batches = [System.Text.RegularExpressions.Regex]::Split($scriptContent, "^\s*GO\s*$", [System.Text.RegularExpressions.RegexOptions]::Multiline -bor [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)

$connStr = "Server=localhost;Database=IPhoneStoreDb;Integrated Security=True;TrustServerCertificate=True;"

# Thử load Microsoft.Data.SqlClient hoặc System.Data.SqlClient
try {
    Add-Type -AssemblyName "System.Data"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    Write-Host "Connected successfully using System.Data.SqlClient!"
} catch {
    Write-Host "Falling back to Microsoft.Data.SqlClient..."
    $conn = New-Object Microsoft.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
}

try {
    foreach ($batch in $batches) {
        $trimmed = $batch.Trim()
        if (-not [string]::IsNullOrWhiteSpace($trimmed)) {
            $cmd = $conn.CreateCommand()
            $cmd.CommandText = $trimmed
            $cmd.ExecuteNonQuery() | Out-Null
        }
    }
    Write-Host "ALL SEED DATA INSERTED IN PERFECT UTF-8 UNICODE!"
} finally {
    $conn.Close()
}
