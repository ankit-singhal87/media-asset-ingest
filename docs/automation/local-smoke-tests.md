# Local Smoke Tests

## Manifest Ingest E2E

Use `scripts/dev/local-e2e-smoke.sh` to exercise the draft local manifest ingest
path from the repo-root `input/` folder to the repo-root `output/` folder.

Start the API first:

```bash
dotnet run --project src/MediaIngest.Api --urls http://127.0.0.1:5000
```

Then run the smoke script from another terminal:

```bash
sh scripts/dev/local-e2e-smoke.sh
```

The script resets only its selected package directory, posts
`POST /api/ingest/start`, creates `manifest.json` and
`manifest.json.checksum`, and waits for matching files under `output/`.

For dry-run validation without starting the API or changing files:

```bash
sh scripts/dev/local-e2e-smoke.sh --dry-run
```

Optional overrides:

- `MEDIA_INGEST_API_URL` changes the API root URL.
- `SMOKE_PACKAGE_ID` changes the package folder name.
- `SMOKE_TIMEOUT_SECONDS` changes the output polling timeout.
- `SMOKE_INTERVAL_SECONDS` changes the output polling interval.
