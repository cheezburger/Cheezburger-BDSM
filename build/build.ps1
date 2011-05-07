properties {
$build_number = if($env:BUILD_NUMBER) {$env:BUILD_NUMBER.Split('.')[2] } else { "0" }
$version = if($env:BUILD_NUMBER) {$env:BUILD_NUMBER} else { "1.0.0.1" }
}

include .\master_build.ps1
include .\test_build.ps1
#include .\stage_application.ps1
#include .\build_msi_installer.ps1
include .\package_deployment.ps1
task default -depends compile, test, nuget_pack #, stage #, test, generate_installer, package
