# Changelog

## [0.1.0](https://github.com/st0o0/puhu.gitkraken/compare/v0.1.0...v0.1.0) (2026-06-15)


### Features

* add actor messages for git repo queries ([6568d86](https://github.com/st0o0/puhu.gitkraken/commit/6568d86118cc0842ea90fe0a35d3b220a77fca1a))
* add actor messages for staging, write, and remote operations ([f1857cc](https://github.com/st0o0/puhu.gitkraken/commit/f1857cc7d1f4d3be6ecaabc2c769227eb6e7b2cd))
* add BranchInfo models and GitBranchParser ([d3f13ba](https://github.com/st0o0/puhu.gitkraken/commit/d3f13bae6e6428c2705081eacaa93ba19154aa75))
* add CommandRegistry with fuzzy search for command palette ([f1cdace](https://github.com/st0o0/puhu.gitkraken/commit/f1cdace8e8047e78b932dd1ddd4c1344a16812b2))
* add CommitDetailPage and CommitDetailViewModel for drill-down view ([acc2807](https://github.com/st0o0/puhu.gitkraken/commit/acc2807bb2eeca75d308669c3220122ee5648e26))
* add data models for graph commits, commit details, and diffs ([b761d18](https://github.com/st0o0/puhu.gitkraken/commit/b761d18f4894963c0319bc6c8b1b85a05d665f72))
* add GitCliService for git CLI process execution ([527219b](https://github.com/st0o0/puhu.gitkraken/commit/527219b71bd0490700939c67a062c80fcc88d086))
* add GitLogParser for git log output parsing ([5924689](https://github.com/st0o0/puhu.gitkraken/commit/592468979ce2fcbba0edf5825e11e81add204522))
* add GitRemoteActor for push, pull, and fetch operations ([4444b70](https://github.com/st0o0/puhu.gitkraken/commit/4444b70a46b14cf3a268d8fed6d89334049b0f26))
* add GitRepoActor with LibGit2Sharp-based graph and detail queries ([b63121b](https://github.com/st0o0/puhu.gitkraken/commit/b63121bf090de56a4a14af1d4f63d2b6a63e480c))
* add GitStagingActor for working tree status and staging operations ([2ef9b0f](https://github.com/st0o0/puhu.gitkraken/commit/2ef9b0f144fafde5bb9ad04d793a93714f0bb9ec))
* add GitWriteActor for commit, branch, merge, rebase, and stash operations ([a5f04ea](https://github.com/st0o0/puhu.gitkraken/commit/a5f04eafbecc7487ec94f6a8c05add9a9679ac66))
* add GraphPage and GraphViewModel for git graph display ([ac2acc2](https://github.com/st0o0/puhu.gitkraken/commit/ac2acc28d0a56dd9e0ffeb748f20c870c3d5d84d))
* add GraphRenderer with linear and branch/merge graph support ([5dfcfb8](https://github.com/st0o0/puhu.gitkraken/commit/5dfcfb80b70748dbe4ea7d0db350c34969918c95))
* Add local testing script for GitKraken plugin ([4fd8e18](https://github.com/st0o0/puhu.gitkraken/commit/4fd8e18e74860ac8de98ab14d3a2cb7fe0ba07c8))
* add local testing script for Puhu.GitKraken ([2ecb4d6](https://github.com/st0o0/puhu.gitkraken/commit/2ecb4d6ced7509b0319a42594735fa6c4cf94955))
* add TestRepoBuilder helper for deterministic git test repos ([2cf4bfc](https://github.com/st0o0/puhu.gitkraken/commit/2cf4bfce248b9558f3b21068fed266d5e9b401d3))
* add WorkingTreeStatus model and GitStatusParser for porcelain v2 ([d7c02cc](https://github.com/st0o0/puhu.gitkraken/commit/d7c02ccb61d7a55d8a088d9e7c2617612f2a9ec3))
* **models:** Organize GitKraken model namespaces ([53d0355](https://github.com/st0o0/puhu.gitkraken/commit/53d0355c1e313ac7a676df882046b6cd94ad3b8a))
* scaffold GitKraken plugin with puhu submodule ([d66ff61](https://github.com/st0o0/puhu.gitkraken/commit/d66ff61d80fa6c0278afcf8278dd7e532d063112))
* **task-14:** add GitMainViewModel for split-view main page ([882b5e8](https://github.com/st0o0/puhu.gitkraken/commit/882b5e849db9c1b4b546e3d20b4063531b70b9c5))
* **task-15:** add StatusPanelBuilder for working-tree status rendering ([a812c9a](https://github.com/st0o0/puhu.gitkraken/commit/a812c9aab6526fe273a35686d39edd2169bfb21a))
* **task-16:** replace GraphPage with GitMainPage split-view layout ([b4144fa](https://github.com/st0o0/puhu.gitkraken/commit/b4144faca243fc1db169f930c9f4338510497d42))
* **task-17:** add CommandPalettePage and CommandPaletteViewModel ([184a902](https://github.com/st0o0/puhu.gitkraken/commit/184a90236df3d109fad3cffeae9aedc07105841b))
* **task-18:** wire up all actors and routes in GitKrakenPluginw ([7b8e484](https://github.com/st0o0/puhu.gitkraken/commit/7b8e4845e23ccf6a5a22d5a24c43820b2ab6e086))
* update manifest for bundle delivery and GitKraken-specific README ([a4f10b0](https://github.com/st0o0/puhu.gitkraken/commit/a4f10b0cc05ebe9c0b64b5bd7122e4c2eaeb19ca))
* wire up full GitKraken plugin registration with actors, services, and routes ([f04f64a](https://github.com/st0o0/puhu.gitkraken/commit/f04f64a6e7c99266c311bb6a8001d53d269589c9))


### Bug Fixes

* address code review findings in GitRepoActor ([31c580d](https://github.com/st0o0/puhu.gitkraken/commit/31c580d1ff9a45a04d0199f404a20ea392f99eca))


### Refactoring

* migrate GitRepoActor from LibGit2Sharp to GitCliService ([4b89891](https://github.com/st0o0/puhu.gitkraken/commit/4b898910fcf2022411085b775a6e31820e40f18d))
* switch TestRepoBuilder from LibGit2Sharp to git CLI ([7679bb7](https://github.com/st0o0/puhu.gitkraken/commit/7679bb72423683b4622ab68713cbc6b52a773801))
