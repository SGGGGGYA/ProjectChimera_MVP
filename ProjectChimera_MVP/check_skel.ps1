Set-Location "E:\游\ProjectChimera_MVP\ProjectChimera_MVP"
$bytes=[System.IO.File]::ReadAllBytes('Assets\SpineCharacters\DD1\vestal\base\vestal.sprite.idle.skel.bytes')
Write-Host "vestal idle: $($bytes.Length) bytes"
Write-Host "前32字节: $([BitConverter]::ToString($bytes[0..31]))"
$bytes=[System.IO.File]::ReadAllBytes('Assets\SpineCharacters\DD1\vestal\base\vestal.sprite.walk.skel.bytes')
Write-Host "vestal walk: $($bytes.Length) bytes"
Write-Host "前32字节: $([BitConverter]::ToString($bytes[0..31]))"
# 看看能否从特征字节判断版本
# Spine 3.6+ skel 文件头通常是 ASCII "skel" + 4字节小端版本号
$ver=$bytes[4]+$bytes[5]*256+$bytes[6]*65536+$bytes[7]*16777216
Write-Host "推测 skel 版本: $ver (Spine 3.6.x = 3.6.0~3.6.53, 3.7.x = 3.7.0~3.7.94, 3.8.x = 3.8.0+)"
