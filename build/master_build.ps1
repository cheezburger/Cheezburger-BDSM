properties {
	$base_dir = resolve-path ..\
	$tool_dir = "$base_dir\packages"
	$build_dir = "$base_dir\ci"
	$build_output_dir = "$build_dir\work"
	
	$sharedAssemblyInfo = "$base_dir\src\ProjectInfo.cs"
	$sln_file = "$base_dir\Cheezburger.SchemaManager.sln"
	$msbuild = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
}

task clean {
  rm -r -force "$build_dir" -ErrorAction SilentlyContinue
}

task build_dir -depends clean {
	if ((Test-Path $build_dir) -ne $true) {
		new-item $build_dir -itemType directory | Out-Null
	}
}

task version {
	$version_pattern = "\d*\.\d*\.\d*\.\d*"
	$content = Get-Content $sharedAssemblyInfo `
		| ForEach { [regex]::replace($_, $version_pattern, $version) } 

	Set-Content -Value $content -Path $sharedAssemblyInfo
}

task compile -depends build_dir {

	& $msbuild $sln_file /p:OutputPath="$build_output_dir\" /p:Configuration=Release
  
  if($lastExitCode -ne 0) {
		throw "Compile Failed."
	}
}
