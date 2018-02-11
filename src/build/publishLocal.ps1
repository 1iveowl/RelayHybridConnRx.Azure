param([string]$betaver)

if ([string]::IsNullOrEmpty($betaver)) {
	$version = [Reflection.AssemblyName]::GetAssemblyName((resolve-path '..\main\RelayHybridConnRx\bin\Release\netstandard2.0\IRelayHybridConnRx.dll')).Version.ToString(3)
	}
else {
	$version = [Reflection.AssemblyName]::GetAssemblyName((resolve-path '..\main\RelayHybridConnRx\bin\Release\netstandard2.0\IRelayHybridConnRx.dll')).Version.ToString(3) + "-" + $betaver
}

.\build.ps1 $version

nuget.exe push -Source "1iveowlNuGetRepo" -ApiKey key .\NuGet\RelayHybridConnRx.$version.symbols.nupkg