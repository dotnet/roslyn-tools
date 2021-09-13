## VS Branch Info

Displays information about the Roslyn packages used by specific branches of Visual Studio

eg.
```
$ dotnet run -- rel/d16.11
rel/d16.11:
  Package Version: 3.11.0-4.21403.6
  Commit Sha: ae1fff344d46976624e68ae17164e0607ab68b10
  Source branch: release/dev16.11-vs-deps
  Build: https://dnceng.visualstudio.com/7ea9116e-9fac-403d-b258-b31fcf1bb293/_build/results?buildId=1272921
  Packages:
    PreRelease: https://dnceng.visualstudio.com/_apis/resources/Containers/7771452?itemPath=PackageArtifacts%2FPreRelease&%24format=zip&saveAbsolutePath=false
    Release: https://dnceng.visualstudio.com/_apis/resources/Containers/7771452?itemPath=PackageArtifacts%2FRelease&%24format=zip&saveAbsolutePath=false
```
