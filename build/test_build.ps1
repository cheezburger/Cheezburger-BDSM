properties {
	$testrpt_dir = "$build_dir\TestReports"
	$testrpt_name = "TestReport"
	$tests = @('RIS.CharlesRiver.MessageExporter.Tests.dll') 
	$profile_assemblies = "Ris.CharlesRiver.*"
	
	$nunit = "$tool_dir\NUnit.2.5.9.10348\Tools\nunit-console-x86.exe"
	$ncover3 = "C:\Program Files (x86)\NCover"
	$ncover3Runner = "$ncover3\NCover.Console.exe"
	$ncover3Reporting = "$ncover3\NCover.Reporting.exe"
	$nunitCoverageReport = "nunitcoverageReport.xml"
	$nunitReport = "$testrpt_dir\nunitTestReport.xml"
	
}

task test -depends compile, test_no_coverage, test_coverage 

task test_coverage -precondition {return Test-Path $ncover3} {

	if ($tests.Length -le 0) { 
		Write-Host -ForegroundColor Red 'No tests defined'
		return 
	}
  
	if (Test-Path $testrpt_dir) { Remove-Item -Recurse -Force $testrpt_dir }
	New-Item $testrpt_dir -ItemType directory | Out-Null

	$test_assemblies = $tests | ForEach-Object { "$build_output_dir\$_" }

	& $ncover3Runner $nunit $test_assemblies /noshadow /xml=$nunitReport '//reg' '//a' $profile_assemblies '//x' "'$testrpt_dir\$nunitCoverageReport'"
	Write-Host "##teamcity[importData type='nunit' path='$nunitReport']"

    if($lastExitCode -ne 0) {
		throw "Tests Failed."
	}
	
	& $ncover3Reporting $testrpt_dir"\$nunitCoverageReport" //or FullCoverageReport:Html //op $testrpt_dir\CoverageReport //p 'FileExport - Current'
    
	Write-Host "##teamcity[dotNetCoverage ncover3_home='$ncover3']"
    Write-Host "##teamcity[dotNetCoverage ncover3_reporter_args='//or Summary:Html:{teamcity.report.path}']"
	Write-Host "##teamcity[importData type='dotNetCoverage' tool='ncover3' path='$testrpt_dir\$nunitCoverageReport']"  
	Write-Host "Finished Runnign Tests"
}

task test_no_coverage -precondition {return -not(Test-Path $ncover3)} {

  if ($tests.Length -le 0) { 
     Write-Host -ForegroundColor Red 'No tests defined'
     return 
  }
  
  if (Test-Path $testrpt_dir) { Remove-Item -Recurse -Force $testrpt_dir }
  New-Item $testrpt_dir -ItemType directory | Out-Null

  $test_assemblies = $tests | ForEach-Object { "$build_output_dir\$_" }

  & $nunit $test_assemblies /noshadow /xml=$nunitReport

    if($lastExitCode -ne 0) {
		throw "Tests Failed."
	}
    Write-Host "##teamcity[importData type='nunit' path='$nunitReport']"
	Write-Host "Finished Runnign Tests"
}