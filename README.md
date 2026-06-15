# puhu.gitkraken

A [Puhu](https://github.com/st0o0/puhu) plugin for git repository visualization — commit graph, branch labels, and commit detail views.

## Features

- Interactive commit graph with branch/tag labels
- Commit detail view with file diffs
- Auto-refresh every 30 seconds via tick system
- Powered by LibGit2Sharp

## Building

```bash
dotnet build src/Puhu.GitKraken.slnx
```

## Publishing

This plugin uses **bundle delivery** because it includes LibGit2Sharp and native binaries.

1. Build in Release mode: `dotnet publish src/Puhu.GitKraken/Puhu.GitKraken.csproj -c Release`
2. ZIP the publish output as `Puhu.GitKraken.zip`
3. Create a GitHub Release with the ZIP as asset
4. The manifest declares `"bundle": true` so Puhu extracts the ZIP on install

## Development

This repo uses a git submodule for the main puhu framework:

```bash
git submodule update --init --recursive
```
