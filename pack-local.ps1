# 把两个框架包打包到本地 NuGet 文件夹源（供 FileShare 等项目离线引用）。
# 用法: .\pack-local.ps1 [-Feed D:\StudyNode\nuget-local]
param(
    [string]$Feed = "D:\StudyNode\nuget-local"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $Feed)) {
    New-Item -ItemType Directory -Path $Feed | Out-Null
}

dotnet pack src/Shyd.Prism.Core/Shyd.Prism.Core.csproj -c Release -o $Feed
dotnet pack src/Shyd.Prism.Avalonia/Shyd.Prism.Avalonia.csproj -c Release -o $Feed

Write-Host ""
Write-Host "已打包到本地源: $Feed"
Get-ChildItem $Feed -Filter "Shyd.Prism.*" | Format-Table Name, Length
