# this script presupposes the following variables have been passed in/set:
#   $defaultValueSentinel
#   $requiredValueSentinel

function EnsureRequiredValue([string] $friendlyName, [string] $value) {
    if (($value -eq "") -or ($value -eq $requiredValueSentinel)) {
        Write-Host "Missing required value: $friendlyName"
        exit 1
    }
}

function IsDefaultValue([string] $value) {
    return ($value -eq "") -or ($value -eq $defaultValueSentinel)
}

function GetComponentAzdoUri([string] $componentAzdoUri) {
    if (IsDefaultValue $componentAzdoUri) {
        return ""
    } else {
        return "/cburi=$componentAzdoUri"
    }
}

function GetComponentProjectName([string] $componentProjectName) {
    if (IsDefaultValue $componentProjectName) {
        return ""
    } else {
        return "/cbpn=$componentProjectName"
    }
}

function GetComponentGitHubRepoName([string] $componentGitHubRepoName) {
    if (IsDefaultValue $componentGitHubRepoName) {
      return ""
    } else {
      return "/componentgithubreponame=$componentGitHubRepoName"
    }
}

function GetComponentUserName([string] $componentUserName) {
    if (IsDefaultValue $componentUserName) {
        return "/cbu=dn-bot@microsoft.com"
    } else {
        return "/cbu=$componentUserName"
    }
}

function GetComponentPassword([string] $componentPassword) {
    if (IsDefaultValue $componentPassword) {
        return ""
    } else {
        return "/cbp=$componentPassword"
    }
}

function GetBuildQueueName([string] $componentName, [string] $buildQueueName) {
    if (IsDefaultValue $buildQueueName) {
        switch ($componentName) {
            "F#" { return "FSharp-Signed" }
            "Live Unit Testing" { return "TestImpact-Signed" }
            "Project System" { return "DotNet-Project-System" }
            "Razor" { return "razor-ci-official" }
            "Roslyn" { return "Roslyn-Signed" }
            "VS Unit Testing" { return "VSUnitTesting-Signed" }
            default {
                Write-Host "Unable to determine buildQueueName from componentName"
                exit 1
            }
        }
    }
    else {
        return $buildQueueName
    }
}

function GetDropPathFlag([string] $componentName, [string] $dropPath) {
    if (IsDefaultValue $dropPath) {
        switch ($componentName) {
            "F#" { return "/dp=\\cpvsbuild\drops\FSharp" }
            "VS Unit Testing" { return "/dp=server" }
            default { return "" }
        }
    }
    else {
      if ($dropPath.startsWith("/dp=")) {
        return $dropPath
      }
      return "/dp=$dropPath"
    }
}

function GetInsertCore([string] $componentName, [string] $insertCore) {
    if (IsDefaultValue $insertCore) {
        if (($componentName -eq "Live Unit Testing") -or ($componentName -eq "Project System") -or ($componentName -eq "F#")) {
            return "false"
        }
        else {
            return "true"
        }
    }
    else {
        return $insertCore
    }
}

function GetInsertDevDiv([string] $insertDevDiv) {
    if (IsDefaultValue $insertDevDiv) {
        return "false"
    }
    else {
        return $insertDevDiv
    }
}

function GetInsertToolsetFlag([string] $componentName, [string] $insertToolset) {
    if (IsDefaultValue $insertToolset) {
        if (($componentName -eq "Roslyn") -and ($visualStudioBranchName.StartsWith("lab/"))) {
            return "/t"
        }
        else {
            return ""
        }
    }
    elseif ($insertToolset -eq "true") {
        return "/t"
    }
    else {
        return ""
    }
}

function GetCherryPick([string] $cherryPick) {
    if (IsDefaultValue $cherryPick) {
        return ""
    } else {
        return "/cherrypick=$cherryPick"
    }
}

function GetSkipCoreXTPackages([string] $skipCoreXTPackages) {
    if (IsDefaultValue $skipCoreXTPackages) {
        return ""
    } else {
        return "/skipcorextpackages=$skipCoreXTPackages"
    }
}

function GetQueueValidation([string] $visualStudioBranchName, [string] $queueValidation) {
    if (IsDefaultValue $queueValidation) {
        if ($visualStudioBranchName -eq "lab/ml") {
            return "false"
        }
        else {
            return "true"
        }
    }
    else {
        return $queueValidation
    }
}

function GetQueueSpeedometerValidation([string] $queueSpeedometerValidation) {
    if (IsDefaultValue $queueSpeedometerValidation) {
        return ""
    }
    else {
        return "/rs=$queueSpeedometerValidation";
    }
}

function GetSpecificBuildFlag([string] $specificBuild) {
    if (IsDefaultValue $specificBuild) {
        return ""
    }
    else {
        return "/sb=$specificBuild";
    }
}

function GetUpdateAssemblyVersions([string] $componentName, [string] $visualStudioBranchName, [string] $updateAssemblyVersions) {
    if (IsDefaultValue $updateAssemblyVersions) {
        if (($componentName -eq "Project System") -and (-not ($visualStudioBranchName.StartsWith("rel/")))) {
            return "true"
        }
        else {
            return "false"
        }
    }
    else {
        return $updateAssemblyVersions
    }
}

function GetUpdateCoreXTLibraries([string] $componentName, [string] $updateCoreXTLibraries) {
    if (IsDefaultValue $updateCoreXTLibraries) {
        if ($componentName -eq "Roslyn") {
            return "true"
        }
        else {
            return "false"
        }
    }
    else {
        return $updateCoreXTLibraries
    }
}

function GetAutoComplete([string] $autoComplete) {
    if (IsDefaultValue $autoComplete) {
        return "false"
    }
    else {
        return $autoComplete
    }
}

function GetCreateDraftPR([string] $createDraftPR) {
    if (IsDefaultValue $createDraftPR) {
        return "false"
    }
    else {
        return $createDraftPR
    }
}

function GetReviewerGUID([string] $reviewerGUID) {
    if (IsDefaultValue $reviewerGUID) {
        return ""
    }
    else {
        return $reviewerGUID
    }
}

function GetUserName([string] $userName) {
    if (IsDefaultValue $userName) {
        return "/u=dn-bot@microsoft.com"
    } else {
        return "/u=$userName"
    }
}

function GetPassword([string] $password) {
    if (IsDefaultValue $password) {
        return ""
    } else {
        return "/p=$password"
    }
}

function GetClientId([string] $clientId) {
    if (IsDefaultValue $clientId) {
        return ""
    } else {
        return "/ci=$clientId"
    }
}

function GetClientSecret([string] $clientSecret) {
    if (IsDefaultValue $clientSecret) {
        return ""
    } else {
        return "/cs=$clientSecret"
    }
}

function GetExistingPR([string] $existingPR) {
    if (IsDefaultValue $existingPR) {
        return ""
    } else {
        return "/updateexistingpr=$existingPR"
    }
}
