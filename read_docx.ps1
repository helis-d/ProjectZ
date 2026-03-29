Add-Type -AssemblyName System.IO.Compression.FileSystem
$path = 'C:\Users\kesim\Desktop\Project_Z_GDD_Final.docx'
$zip = [System.IO.Compression.ZipFile]::OpenRead($path)
$entry = $zip.Entries | Where-Object { $_.FullName -eq 'word/document.xml' }
$stream = $entry.Open()
$reader = New-Object System.IO.StreamReader($stream)
$xml = $reader.ReadToEnd()
$reader.Close()
$zip.Dispose()
$text = ($xml -replace '<[^>]+>','') -replace '[ \t]+',' '
$text | Out-File -FilePath 'c:\Users\kesim\ProjectZ\gdd_text.txt' -Encoding UTF8
Write-Output "Done. Lines: $($text.Split("`n").Count)"
