# Continuous Integration

## SonarCloud

We enforce code coverage and run additional security and static analysis checks (in addition to NET Analyzers) via [SonarCloud](https://www.sonarsource.com/products/sonarcloud)

## GitHub CodeQL

We also run GitHub's [CodeQL](https://codeql.github.com)

## GitHub Action Version Pinning

For security it is advisable to pin all actions to the git SHA that you intend to
use. This is because git tags are mutable and can be changed which brings a risk
of an action being tampered with without your knowledge.

We pin the versions in this repository using a tool called [ratchet](https://github.com/sethvargo/ratchet).
Once versions are pinned they will be updated by dependabot automatically

If you add new workflows or actions you can download and run ratchet locally.

We also run a workflow that check to ensure that all versions in all workflows
are correctly pinned.

