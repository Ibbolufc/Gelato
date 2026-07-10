<div align="center">
   <img width="125" src="logo.png" alt="Gelato logo">
</div>

<div align="center">
  <h1><b>Gelato Universal</b></h1>
  <p><i>Use ordinary Stremio addons directly inside Jellyfin</i></p>
</div>

> [!WARNING]
> This is an early multi-addon test build. Test it before using it on a production Jellyfin server.

Gelato Universal is a fork of [lostb1t/Gelato](https://github.com/lostb1t/Gelato). It keeps Gelato's Jellyfin integration while removing the requirement to place every provider behind AIOStreams.

You can configure separate Stremio addons for metadata, catalogs, streams and subtitles. AIOStreams still works, but it is optional.

## Features

- Jellyfin search powered by a standard Stremio metadata addon
- Multiple ordinary Stremio stream addons queried at the same time
- Multiple subtitle addons
- Automatic manifest capability detection
- Stream deduplication and addon labelling
- Catalog import support
- Per-user addon configurations
- Direct HTTP streams and `infoHash`/`fileIdx` torrent responses
- Streams proxied through Jellyfin

## Requirements

- Jellyfin **10.11.6**
- A search/metadata addon such as Cinemeta
- At least one stream addon
- Two empty folders for Gelato's Movies and Shows libraries

## Install from the Jellyfin dashboard

### 1. Remove the original Gelato source for a clean test

If you already installed the original Gelato plugin, uninstall it and restart Jellyfin first.

In Jellyfin go to:

```text
Dashboard → Plugins → Repositories
```

Remove the original Gelato repository URL while testing this fork. The two builds use the same plugin identity and should not be installed together.

### 2. Add this repository

Add a repository with these details:

```text
Name: Gelato Universal
URL: https://raw.githubusercontent.com/Ibbolufc/Gelato/gh-pages/repository.json
```

Save it.

### 3. Install the plugin

Go to:

```text
Dashboard → Plugins → Catalog
```

Open **Gelato Universal**, install the latest version, then restart Jellyfin.

## Configure the addons

Open:

```text
Dashboard → Plugins → My Plugins → Gelato Universal
```

In the addon URL field, enter one manifest URL per line.

Example:

```text
https://v3-cinemeta.strem.io/manifest.json
https://dragon.valyria.win/manifest.json
```

Keep the metadata/search addon first. Stream-only addons cannot provide Jellyfin search results.

You can add more providers underneath:

```text
https://v3-cinemeta.strem.io/manifest.json
https://first-stream-addon.example/manifest.json
https://second-stream-addon.example/manifest.json
https://subtitle-addon.example/manifest.json
```

Save the configuration and restart Jellyfin after changing the addon list.

## Create the Jellyfin libraries

Create two empty folders that Jellyfin can access. Docker users must ensure the folders exist inside the container through a volume mount.

Example container paths:

```text
/config/gelato/movies
/config/gelato/series
```

Enter those same paths in the Gelato configuration page.

Then add two Jellyfin libraries:

### Movies

```text
Dashboard → Libraries → Add Media Library
Content type: Movies
Folder: /config/gelato/movies
```

### Shows

```text
Dashboard → Libraries → Add Media Library
Content type: Shows
Folder: /config/gelato/series
```

For the Shows library, enable **Gelato missing season/episode fetcher** and move it to the top of the metadata downloader list.

Save both libraries and run **Scan All Libraries**.

## Test the full flow

1. Search Jellyfin for a well-known movie.
2. Confirm the poster, title and description appear.
3. Open the movie and start playback.
4. Confirm streams from the configured stream addon appear.
5. Test pause, resume and seeking.
6. Search for a television series.
7. Confirm seasons and episodes load.
8. Play an episode and test seeking again.
9. Add a second stream addon and confirm results from both providers appear without duplicates.

## Troubleshooting

### The repository does not appear in Jellyfin

Open this URL in a browser:

```text
https://raw.githubusercontent.com/Ibbolufc/Gelato/gh-pages/repository.json
```

It should display JSON. The publishing workflow may still be running if the file is not available yet.

### Search returns no results

- Keep Cinemeta or another searchable metadata addon first.
- Make sure remote search is enabled in Gelato.
- Restart Jellyfin after editing the addon list.
- Confirm the Movies and Shows libraries exist.

### A movie appears but has no streams

Open each stream addon's `manifest.json` URL in a browser and confirm it returns JSON. Then check the latest Jellyfin server log for `Gelato`, `Stremio`, `HTTP` or `timeout`.

### Series appear without seasons or episodes

Edit the Shows library and confirm **Gelato missing season/episode fetcher** is enabled and placed first. Then scan the library again.

## Current limitations

- `behaviorHints.proxyHeaders` are not yet adapted to Jellyfin playback.
- YouTube IDs, external URLs, NZB sources and archive-backed streams are not yet supported.
- Direct HTTP streams and torrent `infoHash` responses are the initial supported playback types.

## Manual test build

Every change to `main` builds automatically under GitHub Actions. The latest workflow also provides a downloadable ZIP artifact for manual installation.

```text
Actions → Publish Jellyfin Test Repository → latest successful run → Artifacts
```

## Credits and licence

This project is based on [lostb1t/Gelato](https://github.com/lostb1t/Gelato) and remains licensed under GPLv3. Original copyright and licence notices are retained.
