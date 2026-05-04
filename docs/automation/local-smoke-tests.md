# Local Smoke Tests

## Manifest Ingest E2E

Use the manual UI flow or `scripts/dev/local-e2e-smoke.sh` to exercise the local
manifest ingest path from the repo-root `input/` folder to the repo-root
`output/` folder.

Start the API first:

```bash
dotnet run --project src/MediaIngest.Api --urls http://127.0.0.1:5000
```

### Manual UI Flow

Start the React control plane in another terminal:

```bash
cd web/ingest-control-plane
npm run dev
```

The Vite development server proxies `/api` requests to
`http://127.0.0.1:5000`. Open the Vite URL, press **Start ingest**, then create
the manifest package after the watcher has started:

```bash
mkdir -p input/asset-001
printf '%s\n' '{"asset":"asset-001"}' > input/asset-001/manifest.json
printf '%s\n' 'local-demo-checksum' > input/asset-001/manifest.json.checksum
```

The expected output is the matching manifest pair:

```text
output/asset-001/manifest.json
output/asset-001/manifest.json.checksum
```

### Scripted Smoke

Run the smoke script from another terminal after the API is listening:

```bash
sh scripts/dev/local-e2e-smoke.sh
```

The script resets only its selected `input/<asset>/` and `output/<asset>/`
directories, posts `POST /api/ingest/start`, creates `manifest.json` and
`manifest.json.checksum`, and waits for matching files under `output/<asset>/`.

For dry-run validation without starting the API or changing files:

```bash
sh scripts/dev/local-e2e-smoke.sh --dry-run
```

Optional overrides:

- `MEDIA_INGEST_API_URL` changes the API root URL.
- `SMOKE_PACKAGE_ID` changes the package folder name.
- `SMOKE_TIMEOUT_SECONDS` changes the output polling timeout.
- `SMOKE_INTERVAL_SECONDS` changes the output polling interval.
