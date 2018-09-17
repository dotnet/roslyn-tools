# this script has it's own enlistment path, so it shouldn't need the mutex

Set-Location -Path E:\prebuilt\roslyn-tools\RIT

function Do-Insertion(
    $component,
    $queueName,
    $fromBranch,
    $toBranch,
    $insertCore="true",
    $insertDevdiv="true",
    $updatecorextlibraries="false",
    $updateassemblyversions="false",
    $insertToolset="false",
    $queueValidation="true",
    $dropPath="")
{
    Write-Host "Performing $component $fromBranch Insertion into $toBranch..."

    $toolsetFlag = ""
    if ($insertToolset -eq "true") {
        $toolsetFlag = "/t"
    }

    $dropPathFlag = ""
    if ($dropPath) {
        $dropPathFlag = "/dp=$dropPath"
    }

    & .\RIT.exe /ep=E:\VS /in=$component /bn=$fromBranch /vsbn=$toBranch /bq=$queueName /ic=$insertCore /uc=$updatecorextlibraries /ua=$updateassemblyversions /id=$insertDevdiv /qv=$queueValidation $toolsetFlag $dropPathFlag

    $timeStamp = Get-Date -Format o | foreach {$_ -replace ":", "."}
    Move-Item rit.log E:\logs\RIT\rit.$queueName.$timeStamp.log
}

#Do-Insertion -component "Roslyn"            -queueName "Roslyn-Signed"    -fromBranch "dev15.8.x-vs-deps" -toBranch "rel/d15.8" -insertToolset "true" -insertDevdiv "false" -queueValidation "true" -updatecorextlibraries "true"

# Dev15.8 - Servicing only
#Do-Insertion -component "Roslyn"            -queueName "Roslyn-Signed"         -fromBranch "dev15.8.x-vs-deps" -toBranch "rel/d15.8" -insertToolset "false" -insertDevdiv "false" -queueValidation "true" -updatecorextl#ibraries "true"
#Do-Insertion -component "Live Unit Testing" -queueName "TestImpact-Signed"     -fromBranch "dev15.8.x"         -toBranch "rel/d15.8" -insertCore "false" -insertDevdiv "false" -queueValidation "true"
#Do-Insertion -component "Project System"    -queueName "DotNet-Project-System" -fromBranch "dev15.8.x"         -toBranch "rel/d15.8" -insertCore "false" -insertDevdiv "false" -queueValidation "true"
#Do-Insertion -component "F#"                -queueName "FSharp-Signed"         -fromBranch "dev15.8"           -toBranch "rel/d15.8" -insertCore "false" -insertDevdiv "false" -queueValidation "true" -dropPath "\\cpvsbuild\drops\FSharp"
#Do-Insertion -component "VS Unit Testing"   -queueName "VSUnitTesting-Signed"  -fromBranch "dev15.8.x"         -toBranch "rel/d15.8" -insertCore "true"                       -queueValidation "true" -dropPath "server"

# Dev15.9 Preview 3 (until 9/18 6pm)
Do-Insertion -component "Roslyn"            -queueName "Roslyn-Signed"         -fromBranch "dev15.9-preview3-vs-deps" -toBranch "lab/d15.9stg" -insertToolset "false" -insertDevdiv "false" -updatecorextl#ibraries "true"
Do-Insertion -component "Live Unit Testing" -queueName "TestImpact-Signed"     -fromBranch "dev15.9-preview3"         -toBranch "lab/d15.9stg" -insertCore "false"    -insertDevdiv "false" -queueValidation "true"
Do-Insertion -component "Project System"    -queueName "DotNet-Project-System" -fromBranch "dev15.9-preview3"         -toBranch "lab/d15.9stg" -insertCore "false"    -insertDevdiv "false" -queueValidation "true" -updateassemblyversions "false"
Do-Insertion -component "F#"                -queueName "FSharp-Signed"         -fromBranch "dev15.9"                  -toBranch "lab/d15.9stg" -insertCore "false"    -insertDevdiv "false" -queueValidation "true" -dropPath "\\cpvsbuild\drops\FSharp"
Do-Insertion -component "VS Unit Testing"   -queueName "VSUnitTesting-Signed"  -fromBranch "dev15.9-preview3"         -toBranch "lab/d15.9stg" -insertCore "true"                           -queueValidation "true" -dropPath "server"

# Dev15.9 Preview 3 (starting 9/18 6pm)
#Do-Insertion -component "Roslyn"            -queueName "Roslyn-Signed"         -fromBranch "dev15.9-preview3-vs-deps" -toBranch "rel/d15.9" -insertToolset "false" -insertDevdiv "false" -updatecorextl#ibraries "true"
#Do-Insertion -component "Live Unit Testing" -queueName "TestImpact-Signed"     -fromBranch "dev15.9-preview3"         -toBranch "rel/d15.9" -insertCore "false"    -insertDevdiv "false" -queueValidation "true"
#Do-Insertion -component "Project System"    -queueName "DotNet-Project-System" -fromBranch "dev15.9-preview3"         -toBranch "rel/d15.9" -insertCore "false"    -insertDevdiv "false" -queueValidation "true" -updateassemblyversions "false"
#Do-Insertion -component "F#"                -queueName "FSharp-Signed"         -fromBranch "dev15.9"                  -toBranch "rel/d15.9" -insertCore "false"    -insertDevdiv "false" -queueValidation "true" -dropPath "\\cpvsbuild\drops\FSharp"
#Do-Insertion -component "VS Unit Testing"   -queueName "VSUnitTesting-Signed"  -fromBranch "dev15.9-preview3"         -toBranch "rel/d15.9" -insertCore "true"                           -queueValidation "true" -dropPath "server"

# Dev15.9 Preview 4 (starting 9/18 6pm)
#Do-Insertion -component "Roslyn"            -queueName "Roslyn-Signed"         -fromBranch "dev15.9.x-vs-deps" -toBranch "lab/d15.9stg" -insertToolset "false" -insertDevdiv "false" -updatecorextl#ibraries "true"
#Do-Insertion -component "Live Unit Testing" -queueName "TestImpact-Signed"     -fromBranch "dev15.9.x"         -toBranch "lab/d15.9stg" -insertCore "false"    -insertDevdiv "false" -queueValidation "true"
#Do-Insertion -component "Project System"    -queueName "DotNet-Project-System" -fromBranch "dev15.9.x"         -toBranch "lab/d15.9stg" -insertCore "false"    -insertDevdiv "false" -queueValidation "true" -updateassemblyversions "false"
#Do-Insertion -component "F#"                -queueName "FSharp-Signed"         -fromBranch "dev15.9"           -toBranch "lab/d15.9stg" -insertCore "false"    -insertDevdiv "false" -queueValidation "true" -dropPath "\\cpvsbuild\drops\FSharp"
#Do-Insertion -component "VS Unit Testing"   -queueName "VSUnitTesting-Signed"  -fromBranch "dev15.9.x"         -toBranch "lab/d15.9stg" -insertCore "true"                           -queueValidation "true" -dropPath "server"

# Dev16 Preview 1
Do-Insertion -component "Roslyn"            -queueName "Roslyn-Signed"         -fromBranch "dev16.0.x-vs-deps" -toBranch "lab/ml" -insertToolset "true" -insertDevdiv "false"   -updatecorextlibraries "true"
Do-Insertion -component "Live Unit Testing" -queueName "TestImpact-Signed"     -fromBranch "dev16.0.x"         -toBranch "lab/ml" -insertCore "false"   -insertDevdiv "false"
Do-Insertion -component "Project System"    -queueName "DotNet-Project-System" -fromBranch "dev16.0.x"         -toBranch "lab/ml" -insertCore "true"    -insertDevdiv "false"   -updateassemblyversions "true" 
Do-Insertion -component "F#"                -queueName "FSharp-Signed"         -fromBranch "dev16.0"           -toBranch "lab/ml" -insertCore "false"   -insertDevdiv "false"   -dropPath "\\cpvsbuild\drops\FSharp"
Do-Insertion -component "VS Unit Testing"   -queueName "VSUnitTesting-Signed"  -fromBranch "dev16.0.x"         -toBranch "lab/ml" -insertCore "true"    -queueValidation "true" -dropPath "server"
