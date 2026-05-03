# Ingest Lifecycle

## Package Discovery

The watcher monitors a configured filesystem path. Application code treats the
path as a plain mounted directory. Local development can use a bind mount; Azure
production can later bind the same path to Azure-backed storage.

## Start Conditions

An ingest package is not eligible for work until `manifest.json` exists. The
manifest is a start signal and metadata source, not the authoritative list of
files to ingest.

## File Enumeration

The scanner must ingest every file physically present in the package directory.
If the manifest omits a file, the file is still ingested and marked as
discovered. If the manifest references a missing file, that is a validation
warning rather than a reason to skip real files.

## Done Marker

A zero-byte done marker indicates upload completion. The system may begin
processing before the marker appears. When the marker appears, reconciliation
rescans the directory, enqueues late files, and allows package finalization once
all required work reaches a terminal state.

## Essence Categories

Initial categories:

- video/source: `.mov`, `.mxf`, `.mp4`
- text: `.srt`, `.txt`, `.vtt`
- audio: `.mp3`, `.wav`
- other: unknown sidecars or future essence types

Classification rules should be easy to extend without rewriting workflow logic.
