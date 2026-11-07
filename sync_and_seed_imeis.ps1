$conn = New-Object System.Data.SqlClient.SqlConnection("Server=localhost;Database=IPhoneStoreDb;Trusted_Connection=True;TrustServerCertificate=True")
$conn.Open()

# 1. Ensure all products that have 0 or few IMEIs get realistic InStock IMEIs (up to 5 per product)
$cmd = $conn.CreateCommand()
$cmd.CommandText = @"
SELECT Id, Sku FROM Products WHERE IsActive = 1
"@
$reader = $cmd.ExecuteReader()
$prods = @()
while ($reader.Read()) {
    $prods += @{ Id = $reader.GetInt32(0); Sku = $reader.GetString(1) }
}
$reader.Close()

$insertCmd = $conn.CreateCommand()
foreach ($p in $prods) {
    $pId = $p.Id
    $sku = $p.Sku
    # Check how many InStock IMEIs currently exist
    $checkCmd = $conn.CreateCommand()
    $checkCmd.CommandText = "SELECT COUNT(*) FROM ProductImeis WHERE ProductId = $pId AND Status = 'InStock'"
    $currCount = [int]$checkCmd.ExecuteScalar()

    # If product has fewer than 5 InStock IMEIs, generate up to 5 IMEIs
    if ($currCount -lt 5) {
        $needed = 5 - $currCount
        for ($i = 1; $i -le $needed; $i++) {
            # Generate 15-digit IMEI starting with 35 (Apple GSMA TAC)
            # Prefix 35 + 2-digit ProductId padded + 9 random digits + 2 seq
            $rnd = (Get-Random -Minimum 100000000 -Maximum 999999999).ToString()
            $imei = "35" + $pId.ToString("D3") + $rnd.Substring(0, 8) + $i.ToString("D2")
            $serial = "F2L" + [System.Guid]::NewGuid().ToString("N").Substring(0, 8).ToUpper()
            
            $sql = @"
IF NOT EXISTS (SELECT 1 FROM ProductImeis WHERE Imei = '$imei')
BEGIN
    INSERT INTO ProductImeis (ProductId, Imei, SerialNumber, Status, ImportDate, Notes)
    VALUES ($pId, '$imei', '$serial', 'InStock', GETDATE(), N'Nhập kho chuẩn AAR BomChelsea - Lô hàng thực tế')
END
"@
            $ins = $conn.CreateCommand()
            $ins.CommandText = $sql
            $ins.ExecuteNonQuery() | Out-Null
        }
    }
}

# 2. Synchronize Products.Quantity EXACTLY with COUNT of InStock IMEIs
$syncCmd = $conn.CreateCommand()
$syncCmd.CommandText = @"
UPDATE Products 
SET Quantity = ISNULL((
    SELECT COUNT(*) 
    FROM ProductImeis 
    WHERE ProductImeis.ProductId = Products.Id 
      AND ProductImeis.Status = 'InStock'
), 0)
"@
$affected = $syncCmd.ExecuteNonQuery()
Write-Host "Updated Products.Quantity for $affected products."

# 3. Print verification table
$verifyCmd = $conn.CreateCommand()
$verifyCmd.CommandText = @"
SELECT p.Id, LEFT(p.Name, 30) AS Name, p.Quantity AS TableQty, 
       ISNULL(i.InStockCount, 0) AS InStockImeis,
       ISNULL(i.TotalImeiCount, 0) AS TotalImeis
FROM Products p
LEFT JOIN (
    SELECT ProductId, 
           COUNT(*) AS TotalImeiCount,
           SUM(CASE WHEN Status = 'InStock' THEN 1 ELSE 0 END) AS InStockCount
    FROM ProductImeis
    GROUP BY ProductId
) i ON p.Id = i.ProductId
ORDER BY p.Id
"@
$adapter = New-Object System.Data.SqlClient.SqlDataAdapter($verifyCmd)
$ds = New-Object System.Data.DataSet
$adapter.Fill($ds) | Out-Null
$ds.Tables[0] | Format-Table -AutoSize

$conn.Close()
