.\Environment.ps1
Invoke-psake .\build\build.ps1
if($lastExitCode -ne 0) {
		throw "Compile Failed."
	}
