# Gelato Universal — multi-addon MVP

Configure the existing URL field with one manifest or base URL per line.

```text
https://v3-cinemeta.strem.io/manifest.json
https://your-stream-addon.example/manifest.json
https://your-subtitle-addon.example/manifest.json
```

The first compatible search/meta addon supplies Jellyfin search and metadata. All compatible stream addons are queried concurrently, merged, labelled by addon, and deduplicated. Subtitle responses are merged in the same way.

## MVP limitations

- Direct URL streams and infoHash/fileIdx torrent streams are supported.
- `behaviorHints.proxyHeaders`, YouTube IDs, external URLs, NZB and archive-backed stream sources are not yet adapted to Jellyfin playback.
- Keep a metadata/search addon first. Stream-only addons cannot power Jellyfin search.
- Test on a separate Jellyfin instance before using an existing production database.
