### [1.3.1](https://github.com/philips-software/roslyn-analyzers/compare/v1.3.0...v1.3.1) (2023-10-25)


### Bug Fixes

* opt-in to solution-level analyzers ([#695](https://github.com/philips-software/roslyn-analyzers/issues/695)) ([95512b7](https://github.com/philips-software/roslyn-analyzers/commit/95512b70a29c16e0935b3724692afc0b9ba99e67))
* PH2134 Support read only arrow properties ([#688](https://github.com/philips-software/roslyn-analyzers/issues/688)) ([3e4f6a9](https://github.com/philips-software/roslyn-analyzers/commit/3e4f6a9f56d3f1be63a92b08f4efffca766c8d22))


### Build Systems

* Bump actions/checkout from 3.5.3 to 4.1.1 ([#691](https://github.com/philips-software/roslyn-analyzers/issues/691)) ([ee0ec4e](https://github.com/philips-software/roslyn-analyzers/commit/ee0ec4ed7996a69173884916e7af46833a9624a8))
* Bump github/codeql-action from 2.22.1 to 2.22.4 ([#692](https://github.com/philips-software/roslyn-analyzers/issues/692)) ([6a30815](https://github.com/philips-software/roslyn-analyzers/commit/6a30815cd6334cd37b9c96d8fede87d4d3ed5c24))



## [1.3.0](https://github.com/philips-software/roslyn-analyzers/compare/v1.2.33...v1.3.0) (2023-10-23)


### Features

* Add Editor config option for PositiveNaming (PH2082) ([#655](https://github.com/philips-software/roslyn-analyzers/issues/655)) ([c290695](https://github.com/philips-software/roslyn-analyzers/commit/c2906951907557a9158a7a8b7b8be0c195efd332))


### Bug Fixes

* false positive on empty method bodies ([#653](https://github.com/philips-software/roslyn-analyzers/issues/653)) ([fafba70](https://github.com/philips-software/roslyn-analyzers/commit/fafba70f85cc79dfb61353c9817d137fa213c20c))
* Performance ci job ([#652](https://github.com/philips-software/roslyn-analyzers/issues/652)) ([a8e85ac](https://github.com/philips-software/roslyn-analyzers/commit/a8e85ac6ca56a28ea745d267fe9fb3cae016ab92))
* Update TestClassMustBePublicAnalyzer ([#679](https://github.com/philips-software/roslyn-analyzers/issues/679)) ([2f5f5a6](https://github.com/philips-software/roslyn-analyzers/commit/2f5f5a6f227475c2670351f1e8326b658a0ec6e3))


### Code Refactoring

* Helper class as central access-point for all helpers ([#657](https://github.com/philips-software/roslyn-analyzers/issues/657)) ([8bca435](https://github.com/philips-software/roslyn-analyzers/commit/8bca4355149911e3961023c68cd617938bff25b9))


### Tests

* improve test coverage of Maintability namespace ([#612](https://github.com/philips-software/roslyn-analyzers/issues/612)) ([6d32341](https://github.com/philips-software/roslyn-analyzers/commit/6d323418f4cf96d293d717b5e1ae70252aa650c0))
* Improve test coverage of Readability ([#613](https://github.com/philips-software/roslyn-analyzers/issues/613)) ([482eb20](https://github.com/philips-software/roslyn-analyzers/commit/482eb20f2c55aab0646f2cd7c0fce714bb668400))


### Build Systems

* Bump actions/checkout from 3.3.0 to 3.5.3 ([#646](https://github.com/philips-software/roslyn-analyzers/issues/646)) ([a961ca1](https://github.com/philips-software/roslyn-analyzers/commit/a961ca17ac619823e6ec811ecb6a738cf4c94c35))
* Bump actions/setup-dotnet from 3.0.3 to 3.2.0 ([#649](https://github.com/philips-software/roslyn-analyzers/issues/649)) ([6497877](https://github.com/philips-software/roslyn-analyzers/commit/6497877f4b2420f65af69937fd6ae9f39d23df36))
* Bump actions/setup-java from 3.10.0 to 3.11.0 ([#633](https://github.com/philips-software/roslyn-analyzers/issues/633)) ([340ea15](https://github.com/philips-software/roslyn-analyzers/commit/340ea15490b41c670707d19552fea2a55f7fd8aa))
* Bump actions/setup-java from 3.11.0 to 3.12.0 ([#661](https://github.com/philips-software/roslyn-analyzers/issues/661)) ([9a4d069](https://github.com/philips-software/roslyn-analyzers/commit/9a4d069c7a78fe476b02fe8fd031c002c9fd6a90))
* Bump actions/setup-java from 3.12.0 to 3.13.0 ([#676](https://github.com/philips-software/roslyn-analyzers/issues/676)) ([5c54a72](https://github.com/philips-software/roslyn-analyzers/commit/5c54a72966f7fc0233b8035344e9946f6a3a8d23))
* Bump actions/upload-artifact from 3.1.2 to 3.1.3 ([#672](https://github.com/philips-software/roslyn-analyzers/issues/672)) ([2566526](https://github.com/philips-software/roslyn-analyzers/commit/25665266a865e7db94f5e04f986c25b6a225bbb9))
* Bump amannn/action-semantic-pull-request from 5.1.0 to 5.2.0 ([#628](https://github.com/philips-software/roslyn-analyzers/issues/628)) ([76ee0d7](https://github.com/philips-software/roslyn-analyzers/commit/76ee0d77c6ba03935341a309d4081e2fa259f717))
* Bump amannn/action-semantic-pull-request from 5.2.0 to 5.3.0 ([#687](https://github.com/philips-software/roslyn-analyzers/issues/687)) ([6d18dcb](https://github.com/philips-software/roslyn-analyzers/commit/6d18dcb6f62e3391a30989dfadb2d0ee3ac1c386))
* Bump EndBug/add-and-commit from 9.1.1 to 9.1.3 ([#651](https://github.com/philips-software/roslyn-analyzers/issues/651)) ([a11aa84](https://github.com/philips-software/roslyn-analyzers/commit/a11aa840ba826347395eaeac13d7b28c961cc08d))
* Bump github/codeql-action from 2.2.6 to 2.20.0 ([#648](https://github.com/philips-software/roslyn-analyzers/issues/648)) ([d3c772f](https://github.com/philips-software/roslyn-analyzers/commit/d3c772f6d550a80ebb254eaae2a547b83737dccf))
* Bump github/codeql-action from 2.20.0 to 2.20.1 ([#650](https://github.com/philips-software/roslyn-analyzers/issues/650)) ([ae0eaf3](https://github.com/philips-software/roslyn-analyzers/commit/ae0eaf3dc50c18a7799ac996b7d8d78df4d7c0f5))
* Bump github/codeql-action from 2.20.1 to 2.20.2 ([#654](https://github.com/philips-software/roslyn-analyzers/issues/654)) ([600a7c6](https://github.com/philips-software/roslyn-analyzers/commit/600a7c6adf88b2a6f42b4b90d3e6e9e575235d51))
* Bump github/codeql-action from 2.20.2 to 2.20.3 ([#656](https://github.com/philips-software/roslyn-analyzers/issues/656)) ([3672e4a](https://github.com/philips-software/roslyn-analyzers/commit/3672e4a8f2e57e6289466e2ba8022485f4f2ab2b))
* Bump github/codeql-action from 2.20.3 to 2.20.4 ([#659](https://github.com/philips-software/roslyn-analyzers/issues/659)) ([e53dc4a](https://github.com/philips-software/roslyn-analyzers/commit/e53dc4ae1ec39118dbd600b5d07eed7a594c8a9d))
* Bump github/codeql-action from 2.20.4 to 2.21.0 ([#660](https://github.com/philips-software/roslyn-analyzers/issues/660)) ([f9aed96](https://github.com/philips-software/roslyn-analyzers/commit/f9aed9696e9fde6f90eb01b7c6f13d6a7f186e8f))
* Bump github/codeql-action from 2.21.0 to 2.21.2 ([#663](https://github.com/philips-software/roslyn-analyzers/issues/663)) ([641a819](https://github.com/philips-software/roslyn-analyzers/commit/641a8195916b58272d3b878ba1401760cf2a2f87))
* Bump github/codeql-action from 2.21.2 to 2.22.1 ([#686](https://github.com/philips-software/roslyn-analyzers/issues/686)) ([333f501](https://github.com/philips-software/roslyn-analyzers/commit/333f501f742414cbff8e934b218a6231518cdacc))
* Bump ncipollo/release-action from 1.12.0 to 1.13.0 ([#668](https://github.com/philips-software/roslyn-analyzers/issues/668)) ([81e3815](https://github.com/philips-software/roslyn-analyzers/commit/81e38156970a5dc37976eefbbe96fe6f46fe168d))
* Bump philips-forks/github-actions-ensure-sha-pinned-actions from 1.0.0 to 1.1.0 ([#627](https://github.com/philips-software/roslyn-analyzers/issues/627)) ([0dccb8d](https://github.com/philips-software/roslyn-analyzers/commit/0dccb8d13ee025356b659f01ecabd5418165342e))


### Continuous Integration

* Update tagversion.yml ([#626](https://github.com/philips-software/roslyn-analyzers/issues/626)) ([cd99e18](https://github.com/philips-software/roslyn-analyzers/commit/cd99e18a3683c4aef427e1296171d50fee34cdac))
