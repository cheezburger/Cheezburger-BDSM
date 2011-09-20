properties {
	$package_dir = "$build_dir\package\"
	$nuget = "$tool_dir\nuget\NuGet.exe"
	$nuget_deploy_dir = "$base_dir\nuget"
}

task package {
	if (Test-Path $package_dir) { Remove-Item -Recurse -Force $package_dir }
    New-Item $package_dir -ItemType Directory | Out-Null
	Copy-Item "$stage_dir\*.msi" $package_dir
}

task nuget_pack {
	if (Test-Path "$nuget_deploy_dir\lib") { Remove-Item -Recurse -Force "$nuget_deploy_dir\lib" }
	New-Item "$nuget_deploy_dir\lib" -ItemType Directory | Out-Null
	Copy-Item "$build_output_dir\Cheezburger.SchemaManager.dll" "$nuget_deploy_dir\lib\"
	& $nuget pack "$base_dir\nuget\Cheezburger.SchemaManager.nuspec"
}
