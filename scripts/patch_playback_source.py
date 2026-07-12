from pathlib import Path

provider = Path("Decorators/MediaSourceManagerDecorator.cs")
text = provider.read_text(encoding="utf-8")

old = '''        sources.AddRange(gelatoSources);

        if (sources.Count > 1)
        {
            // remove primary from list when there are streams
            sources = sources
                .Where(k =>
                    !(k.Path?.StartsWith("gelato", StringComparison.OrdinalIgnoreCase) ?? false)
                )
                .Where(k =>
                    !(k.Path?.StartsWith("stremio", StringComparison.OrdinalIgnoreCase) ?? false)
                )
                .ToList();
        }
'''

new = '''        sources.AddRange(gelatoSources);

        if (item.IsGelato() && gelatoSources.Count > 0)
        {
            // Gelato library items use a temporary .strm file only as a database placeholder.
            // Never offer that placeholder to Jellyfin for playback when real addon streams exist.
            sources = gelatoSources;
        }
        else if (sources.Count > 1)
        {
            // Remove legacy virtual sources while retaining genuine local sources in mixed mode.
            sources = sources
                .Where(k =>
                    !(k.Path?.StartsWith("gelato", StringComparison.OrdinalIgnoreCase) ?? false)
                )
                .Where(k =>
                    !(k.Path?.StartsWith("stremio", StringComparison.OrdinalIgnoreCase) ?? false)
                )
                .ToList();
        }
'''

if new in text:
    raise SystemExit(0)

if old not in text:
    raise SystemExit("Expected playback source block was not found")

provider.write_text(text.replace(old, new, 1), encoding="utf-8")
print("Applied Gelato playback source selection fix")
