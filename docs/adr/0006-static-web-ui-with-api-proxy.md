# ADR 0006: Static web UI with a same-origin API proxy

- **Status:** Accepted
- **Date:** 2026-11-08

## Context

The dashboard needs an interactive graph and a fast first-load experience. A full SPA toolchain adds build steps, dependency management, and a second deployment concern. The API already exposes everything the UI needs as JSON.

## Decision

- Serve the UI as static HTML, CSS, and JavaScript from `ArchPulse.Web`.
- Use Cytoscape.js from a pinned CDN build for the graph.
- Proxy `/api/*` from the web host to `ArchPulse.Api` using an `HttpClient` configured with Aspire service discovery.
- Keep the browser same-origin, so no CORS policy is required.

## Consequences

- No Node.js or bundler is required to run or contribute to the UI.
- The UI loads fast and is easy to host behind any static server.
- The API proxy is explicit code rather than a full reverse-proxy product; it supports the methods and headers the dashboard needs.
- A future SPA or server-rendered UI can replace the static assets without changing the API contract.
- CDN availability affects the graph library; a future release should vendor the pinned assets.
