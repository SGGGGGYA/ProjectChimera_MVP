Set-Location "E:\游\ProjectChimera_MVP\ProjectChimera_MVP"
$s='Assets\SpineCharacters\DD1\vestal\base\vestal.sprite.idle.skel.bytes'
$bytes=[System.IO.File]::ReadAllBytes($s)
Write-Host "文件大小: $($bytes.Length) 字节"
Write-Host "前32字节(hex): $([BitConverter]::ToString($bytes[0..31]))"
$head=[System.Text.Encoding]::ASCII.GetString($bytes[0..7])
Write-Host "前8字节(ascii): $head"
Write-Host "--- Spine 版本 ---"
Get-ChildItem 'Assets\Spine' -Directory | ForEach-Object { $_.Name }
$pkg=Get-Content 'Assets\Spine\spine-unity\package.json' -Raw -ErrorAction SilentlyContinue
if($pkg){ $pkg.Substring(0, [Math]::Min(400, $pkg.Length)) }
