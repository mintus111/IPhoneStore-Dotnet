$conn = New-Object System.Data.SqlClient.SqlConnection("Server=localhost;Database=IPhoneStoreDb;Trusted_Connection=True;TrustServerCertificate=True")
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = @"
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
$adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
$ds = New-Object System.Data.DataSet
$adapter.Fill($ds) | Out-Null
$ds.Tables[0] | Format-Table -AutoSize
$conn.Close()
