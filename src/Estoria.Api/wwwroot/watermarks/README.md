# Watermark assets

Drop `estoria-mark.png` into this folder.

The image-processing pipeline (`MagickImageProcessingService.ApplyWatermark`)
reads the file at runtime, sizes it to ~1/6 of each variant's width (capped at
200 px), and composites it bottom-right at 55% opacity.

When the file is missing, the pipeline falls back to drawing the
`watermark.text` SiteSetting as a text watermark and emits a `WATERMARK_MISSING`
log line so the gap is visible in operations.

The path is configurable via the `watermark.image_path` SiteSetting; defaults
to `/watermarks/estoria-mark.png` (relative to wwwroot).

Source the file from the favicon/branding asset bundle described in
`docs/estoria-favicon-guide.md` — typically a transparent PNG, ~30 KB.
