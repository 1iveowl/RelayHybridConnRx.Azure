param([string]$betaver)

if ([string]::IsNullOrEmpty($betaver)) {
	$version = [Reflection.AssemblyName]::GetAssemblyName((resolve-path '..\main\RelayHybridConnRx\bin\Release\netstandard2.0\IRelayHybridConnRx.dll')).Version.ToString(3)
	}
else {
	$version = [Reflection.AssemblyName]::GetAssemblyName((resolve-path '..\main\RelayHybridConnRx\bin\Release\netstandard2.0\IRelayHybridConnRx.dll')).Version.ToString(3) + "-" + $betaver
}

.\build.ps1 $version

Nuget.exe push ".\NuGet\RelayHybridConnRx.$version.symbols.nupkg" -Source https://www.nuget.org