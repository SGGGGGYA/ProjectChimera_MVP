$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Text
$path = [System.IO.Path]::GetFullPath('E:\游\ProjectChimera_MVP\ProjectChimera_MVP\Assets\SpineCharacters\DD1\vestal\base\vestal.sprite.idle.skel.bytes')
$bytes = [System.IO.File]::ReadAllBytes($path)
Write-Host "vestal idle: $($bytes.Length) bytes"
Write-Host "前16字节: $([BitConverter]::ToString($bytes[0..15]))"
$ver = $bytes[4] + $bytes[5]*256 + $bytes[6]*65536 + $bytes[7]*16777216
Write-Host "推测 skel 版本: $ver"

$path2 = [System.IO.Path]::GetFullPath('E:\游\ProjectChimera_MVP\ProjectChimera_MVP\Assets\SpineCharacters\DD1\vestal\base\vestal.sprite.walk.skel.bytes')
$bytes2 = [System.IO.File]::ReadAllBytes($path2)
Write-Host "vestal walk: $($bytes2.Length) bytes"
Write-Host "前16字节: $([BitConverter]::ToString($bytes2[0..15]))"
$ver2 = $bytes2[4] + $bytes2[5]*256 + $bytes2[6]*65536 + $bytes2[7]*16777216
Write-Host "推测 skel 版本: $ver2"
