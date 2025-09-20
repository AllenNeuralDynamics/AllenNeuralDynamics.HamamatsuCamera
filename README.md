# AllenNeuralDynamics.HamamatsuCamera

A bonsai package to interface with Hamamatsu cameras.

## Build instructions

1. Setup Visual Studio for Bonsai project development by [following the instructions here](https://github.com/bonsai-rx/bonsai?tab=readme-ov-file#building-from-source)
2. Deploy the local Bonsai environment by running `./.bonsai/setup.cmd` once.
3. Open the solution file `src/AllenNeuralDynamics.HamamatsuCamera.sln` in Visual Studio.
4. Build the solution in Release mode to produce a nuget package

## Installing in Bonsai

1. Ensure a nuget package `AllenNeuralDynamics.HamamatsuCamera.<version>.nupkg` exists in the `src/AllenNeuralDynamics.HamamatsuCamera\bin\Release`
2. Copy the nuget package to a different known location (e.g. `C:\BonsaiPackages`)
3. Add this folder to the package manager by [following the instructions here](https://bonsai-rx.org/docs/articles/packages.html#configure-package-sources)
4. Install the package from the package manager as any other Bonsai package.
