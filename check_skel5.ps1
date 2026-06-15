[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8
$path = 'E:\游\ProjectChimera_MVP\ProjectChimera_MVP\Assets\SpineCharacters\DD1\vestal\base\vestal.sprite.idle.skel.bytes'
$bytes = [System.IO.File]::ReadAllBytes($path)
Write-Host "vestal idle: $($bytes.Length) bytes"
Write-Host "head16: $([BitConverter]::ToString($bytes[0..15]))"
$ver = $bytes[4] + $bytes[5]*256 + $bytes[6]*65536 + $bytes[7]*16777216
Write-Host "version: $ver"

$path2 = 'E:\游\ProjectChimera_MVP\ProjectChimera_MVP\Assets\SpineCharacters\DD1\vestal\base\vestal.sprite.walk.skel.bytes'
$bytes2 = [System.IO.File]::ReadAllBytes($path2)
Write-Host "vestal walk: $($bytes2.Length) bytes"
Write-Host "head16: $([BitConverter]::ToString($bytes2[0..15]))"
$ver2 = $bytes2[4] + $bytes2[5]*256 + $bytes2[6]*65536 + $bytes2[7]*16777216
Write-Host "version: $ver2"
