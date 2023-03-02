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
Once versions are pinned, dependabot checked for updates.
