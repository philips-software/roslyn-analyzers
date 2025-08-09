# Continuous Integration

## SonarCloud

80% code coverage is enforced, and coverage is currently over 90%. Security and static analysis checks (in addition to NET Analyzers) are checked via [SonarCloud](https://www.sonarsource.com/products/sonarcloud).

## Dogfooding

All analyzers are run on the code as part of the CI process. No analyzers are disabled. Violations block integration.

## Performance

The performance of all analyzers are measured as part of the dogfooding process, and reported in the Job Summary.
![image](https://user-images.githubusercontent.com/57269455/215766356-6d6ee7b8-5c15-474b-b69d-15291b2fa0b0.png)

## GitHub CodeQL

We also run GitHub's [CodeQL](https://codeql.github.com). (This is currently disabled as it has yet to find issues and significantly slows down the CI pipeline.)

## Markdown Links

Markdown links are checked for dead links.

## GitHub Action Version Pinning

All actions are pinned to the git SHA using [ratchet](https://github.com/sethvargo/ratchet).
Once versions are pinned, dependabot checks for updates.

## Prereleases

Every commit will create artifacts, 1 per Analyzer, having a 1 day retention period. The analyzers can be downloaded and tested with a local nuget.config file against repos as desired.  However, Prerelease versions of modified Nuget packages are also published with each commit.

GitHub Prereleases and Tags are not created with each merge to trunk.

# Deployments

## Release Process Overview

To create a new release and publish to nuget.org, follow this comprehensive process:

## Step 1: Prerelease Testing (Recommended)

Before creating a release, it's recommended to test with prerelease packages:

1. **Download Prerelease Artifacts**: Every commit creates artifacts for each analyzer with a 1-day retention period. These can be found in the CI workflow runs under the "Artifacts" section.
2. **Local Testing Options**:
   - **Option A**: Publish the prerelease artifact to NuGet.org as a prerelease version
   - **Option B**: Use a local nuget.config file to test the packages locally
3. **Test Appropriately**: Validate the analyzer packages work as expected in your target projects

## Step 2: Initiate Release Process

1. **Manually run workflow**: Navigate to [Prep for Release](https://github.com/philips-software/roslyn-analyzers/actions/workflows/prep-release.yml) and trigger it manually
2. **Result**: This creates a PR titled "docs: Release x.y.z" with automatic updates to CHANGELOG.md

## Step 3: Activate Workflows (Required Workarounds)

Due to GitHub's security model for bot-created PRs, the following manual steps are required:

1. **Edit the PR title**: Make a small modification (e.g., add/remove whitespace) to trigger the Semantic Commit Check pipeline
2. **Close and re-open the PR**: This is necessary to trigger the other required workflows to run

*Note: These workarounds are necessary because PRs created by GitHub Actions do not automatically trigger other workflows for security reasons.*

## Step 4: Review and Merge

1. **Review the PR**: Ensure the changelog updates are correct
2. **Approve the PR**: Follow your standard review process
3. **Merge the PR**: Only merge if the Merge Queue is empty to avoid including unintended changes

## Step 5: Download and Publish Artifacts

1. **Wait for CD Pipeline**: Upon completion of the merge, the CD pipeline will run automatically
2. **Download Artifacts**: Navigate to the CD workflow run and download the desired package artifacts
3. **Upload to NuGet.org**: Manually upload the packages to nuget.org

## Important Notes

- **Merge Queue**: Only merge the release PR when the merge queue is empty to ensure the changelog accurately reflects the release content
- **Artifact Retention**: CI artifacts are retained for 1 day, while CD artifacts from releases are retained longer
- **Abandon if Outdated**: If the release PR becomes out-of-date, abandon it and delete the branch to start fresh

## Automated Results

Merging the release PR will automatically trigger creation of:
* GitHub Release
* GitHub Tag
* NuGet Package Artifacts (ready for manual upload to nuget.org)
