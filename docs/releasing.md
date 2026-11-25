# Building and publishing EdiMorph packages

GitHub Actions is the release path for all four product packages. Local packing is for development verification only. Samples and test projects are not published.

## CI

`.github/workflows/ci.yml` runs on pull requests targeting `master`, pushes to `master`, and manual dispatch. It restores, builds, runs all unit tests, packs the four libraries, and tests a separate consumer using `PackageReference` against those exact packages. The consumer's restore cache is isolated, and package-source mapping forces every `EdiMorph.*` dependency to come from the candidate feed.

Successful runs upload `nuget-packages`, containing four `.nupkg` files and `SHA256SUMS`. Test reports are uploaded separately. CI candidates use `<base-version>-ci.<run-id>.<attempt>` and are not published to nuget.org.

## One-time publishing setup

1. In GitHub repository settings, create the environment **nuget**. Restrict its deployment tags to the intended release tags (`v*`), and configure reviewers if desired.
2. Add the repository or environment **variable** `NUGET_USER` with the username of the nuget.org account that owns all four packages. This is not a secret or an API key.
3. In that account's nuget.org **Trusted Publishing** settings, create a GitHub policy:

   | Setting | Value |
   | --- | --- |
   | Repository owner | `TuralSuleymani` |
   | Repository | `EdiMorph` |
   | Workflow file | `publish.yml` |
   | Environment | `nuget` |
   | Package scope | `EdiMorph.Abstractions`, `EdiMorph.Specifications`, `EdiMorph.Specifications.X12`, `EdiMorph.X12` |

   Allow publishing new versions of these packages. Ensure the account has ownership of each package. The workflow uses `NuGet/login` and a short-lived OIDC credential; no persistent NuGet API key is required.
4. Merge the workflows to `master`. In branch protection/rulesets, require the **Build, test, and pack** check if changes must pass CI before merging.

See [NuGet trusted publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) for account policy configuration.

## Release a version

1. Change the shared `Version` in `Directory.Build.props`, for example to `0.3.0` or `0.3.0-preview.1`, and merge the reviewed change to `master` after CI passes.
2. Create a GitHub Release targeting that commit with the matching tag, for example **v0.3.0**. For a version with a suffix, mark the GitHub Release as a prerelease.
3. Publish the GitHub Release. A tag push alone does not publish packages.
4. **Publish NuGet** validates the tag/version and checks that the commit is on `master` history. It invokes the same CI pipeline for that release version, downloads the tested packages, verifies names and checksums, then publishes all four packages to nuget.org without rebuilding.

Every package uses the same version. No package is published when a build, unit test, or consumer test fails. Publishing several packages is not atomic: if a push fails after earlier packages succeeded, re-run the failed publish job. `--skip-duplicate` allows the remaining packages to finish; never change source and reuse an already published version.

The workflow files must exist on the release commit. There is no automatic version bump, production deployment, or package publication on an ordinary merge.

## Verify the same pipeline locally

From the repository root in PowerShell 7:

```powershell
./eng/Build-Packages.ps1 -Version 0.3.0-preview.2
```

This runs the checks and creates a local candidate. It never publishes. Keep the package version unique when testing changed content because NuGet packages are immutable and restore caches are version-keyed.

The package-consumer tests deliberately remain outside `EdiMorph.slnx`: they can only restore after packing. `Build-Packages.ps1` runs the solution tests first and the consumer tests second.
