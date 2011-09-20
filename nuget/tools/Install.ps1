param($installPath, $toolsPath, $package, $project)

($project.ProjectItems |? { $_.Name -eq "Schema" } |% { $_.ProjectItems } |? { $_.Name -eq "Schema.xml" } |% { $_.Properties } |? { $_.Name -eq "BuildAction" }).Value = 3
