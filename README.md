# Even Better Posters for Jellyfin

Independent personal Jellyfin plugin for Btttr.cc poster cards.

The plugin adds Btttr.cc as a Jellyfin remote image provider for movies and series. It fetches Btttr poster overlays from IMDb IDs and can optionally resolve a missing IMDb ID from a Jellyfin TMDB ID through Cinemeta before requesting the poster.

## Changes In This Build

- Adds the Btttr website poster controls: trend tags, quality tags, genre, rating, rating source, age rating, and the full language list.
- Keeps automatic Btttr poster fetching disabled by default until enabled in plugin settings.
- Fixes the broken TMDB fallback path by resolving TMDB IDs to IMDb IDs before calling Btttr.cc.
- Stops relying on the missing upstream v2 release asset.
- Builds a local install zip under `artifacts/` and updates the repository-install zip under `dist/`.

## Build

From this repo:

```powershell
.\build.ps1
```

The package will be written to:

```text
artifacts\EvenBetterPosters_2.1.2.zip
dist\EvenBetterPosters_2.1.2.zip
```

## Manual Install

1. Stop Jellyfin.
2. Create this folder:

```text
C:\ProgramData\Jellyfin\Server\plugins\Even Better Posters_2.1.2.0
```

3. Extract `artifacts\EvenBetterPosters_2.1.2.zip` into that folder.
4. Start Jellyfin.
5. Open Dashboard > Plugins > My Plugins > Even Better Posters and enable automatic fetching only if you want Jellyfin scans to request Btttr posters.

## Repository Install

This matches Jellyfin's built-in plugin repository flow. It works after this public repo is pushed because `manifest.json` points at the tracked zip in `dist/`.

### Step 1: Add the Repository

1. Open your Jellyfin Web UI and log in as an administrator.
2. Go to Dashboard > Plugins.
3. Click the Repositories tab.
4. Click Add and enter:

```text
Repository Name: Even Better Posters
Repository URL: https://raw.githubusercontent.com/FearlessNite345/EvenBetterPosters/main/manifest.json
```

5. Click Save.

### Step 2: Install the Plugin

1. Switch to the Catalog tab.
2. Search for `Btttr` or scroll to the Metadata category.
3. Click Even Better Posters.
4. Select version `2.1.2.0` and click Install.
5. Restart Jellyfin after installation.
6. Open Dashboard > Plugins > My Plugins > Even Better Posters and enable automatic fetching only if you want it.

The repository URL above points Jellyfin at the manifest. The manifest then points Jellyfin at the install zip:

```text
https://raw.githubusercontent.com/FearlessNite345/EvenBetterPosters/main/dist/EvenBetterPosters_2.1.2.zip
```
