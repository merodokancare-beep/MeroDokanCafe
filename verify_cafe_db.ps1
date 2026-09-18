$path = (Get-Item "bin\Debug\net48\MeroDokanCafe.exe").FullName
$bytes = [System.IO.File]::ReadAllBytes($path)
$asm = [System.Reflection.Assembly]::Load($bytes)
$t = $asm.GetType("MeroDokan.DatabaseHelper")
$m = $t.GetMethod("InitializeDatabase")
$m.Invoke($null, $null)

$conn = New-Object System.Data.SqlClient.SqlConnection("Server=(localdb)\MSSQLLocalDB;Database=MeroDokanCafeDB;Integrated Security=True;")
$conn.Open()

Write-Host "--- STAFF LIST ---"
$cmd = New-Object System.Data.SqlClient.SqlCommand("SELECT Id, Name, Role FROM Staff", $conn)
$rdr = $cmd.ExecuteReader()
while ($rdr.Read()) {
    Write-Host "$($rdr['Id']): $($rdr['Name']) ($($rdr['Role']))"
}
$rdr.Close()

Write-Host "--- PRODUCTS LIST ---"
$cmd2 = New-Object System.Data.SqlClient.SqlCommand("SELECT Code, Name, Category, SalesPrice FROM Products", $conn)
$rdr2 = $cmd2.ExecuteReader()
while ($rdr2.Read()) {
    Write-Host "$($rdr2['Code']) | $($rdr2['Name']) | $($rdr2['Category']) | ₹$($rdr2['SalesPrice'])"
}
$rdr2.Close()
$conn.Close()
