(() => {
  "use strict";

  const state = {
    scan: null,
    cy: null,
    pollTimer: null
  };

  const kindColors = {
    WebApp: "#38bdf8",
    Worker: "#c084fc",
    Library: "#34d399",
    AppHost: "#fb7185",
    Test: "#94a3b8",
    Service: "#22d3ee",
    Database: "#fbbf24",
    Cache: "#f472b6",
    Broker: "#f97316",
    External: "#64748b"
  };

  const elements = {
    form: document.getElementById("scan-form"),
    sourceType: document.getElementById("source-type"),
    sourceValue: document.getElementById("source-value"),
    scanButton: document.getElementById("scan-button"),
    scanSpinner: document.getElementById("scan-spinner"),
    demoButton: document.getElementById("demo-button"),
    status: document.getElementById("status"),
    dashboard: document.getElementById("dashboard"),
    scanTitle: document.getElementById("scan-title"),
    scanSource: document.getElementById("scan-source"),
    statusBadge: document.getElementById("scan-status-badge"),
    metricComponents: document.getElementById("metric-components"),
    metricDependencies: document.getElementById("metric-dependencies"),
    metricViolations: document.getElementById("metric-violations"),
    metricViolationsFoot: document.getElementById("metric-violations-foot"),
    metricCycles: document.getElementById("metric-cycles"),
    graphSubtitle: document.getElementById("graph-subtitle"),
    findingsSubtitle: document.getElementById("findings-subtitle"),
    cy: document.getElementById("cy"),
    graphEmpty: document.getElementById("graph-empty"),
    nodeDetail: document.getElementById("node-detail"),
    violations: document.getElementById("violations"),
    reportContent: document.getElementById("report-content"),
    mermaidContent: document.getElementById("mermaid-content"),
    downloadReport: document.getElementById("download-report"),
    downloadMermaid: document.getElementById("download-mermaid"),
    recentScans: document.getElementById("recent-scans")
  };

  init();

  function init() {
    elements.form.addEventListener("submit", (event) => {
      event.preventDefault();
      const sourceValue = elements.sourceValue.value.trim();
      if (!sourceValue) {
        setStatus("Enter a GitHub URL or a local path.", "error");
        return;
      }

      startScan({
        sourceType: elements.sourceType.value,
        sourceValue
      });
    });

    elements.demoButton.addEventListener("click", () => startScan(null, true));

    elements.sourceType.addEventListener("change", () => {
      const isPath = elements.sourceType.value === "path";
      elements.sourceValue.placeholder = isPath
        ? "/home/you/code/my-app"
        : "https://github.com/dotnet/eShop";
    });

    document.querySelectorAll(".tab").forEach((tab) => {
      tab.addEventListener("click", () => setView(tab.dataset.view));
    });

    const linkedScan = new URLSearchParams(window.location.search).get("scan");
    if (linkedScan) {
      openScan(linkedScan);
    }

    loadRecent();
  }

  async function startScan(payload, isDemo = false) {
    clearTimeout(state.pollTimer);
    setBusy(true);
    setStatus(isDemo ? "Queuing the demo scan…" : "Queuing scan…", "working");

    try {
      const options = { method: "POST" };
      if (!isDemo) {
        options.headers = { "Content-Type": "application/json" };
        options.body = JSON.stringify(payload);
      }

      const response = await fetch(isDemo ? "/api/scans/demo" : "/api/scans", options);
      if (!response.ok) {
        throw new Error(await readError(response));
      }

      const scan = await response.json();
      state.scan = scan;
      showDashboard(scan);
      setStatus("Scan queued. Waiting for the worker…", "working");
      poll(scan.id);
    } catch (error) {
      setBusy(false);
      setStatus(error.message || "Could not start the scan.", "error");
    }
  }

  async function poll(id) {
    clearTimeout(state.pollTimer);

    try {
      const response = await fetch(`/api/scans/${id}`, { cache: "no-store" });
      if (!response.ok) {
        throw new Error(await readError(response));
      }

      const scan = await response.json();
      state.scan = scan;
      showDashboard(scan);

      if (scan.status === "Completed") {
        setBusy(false);
        setStatus("Analysis complete. Explore the architecture map.", "ok");
        renderScan(scan);
        loadRecent();
        return;
      }

      if (scan.status === "Failed") {
        setBusy(false);
        setStatus(scan.error || "The scan failed.", "error");
        loadRecent();
        return;
      }

      setStatus(
        scan.status === "Running"
          ? "Analyzing projects and Aspire resources…"
          : "Queued. Waiting for the worker…",
        "working");

      state.pollTimer = setTimeout(() => poll(id), 1200);
    } catch (error) {
      setBusy(false);
      setStatus(error.message || "Lost connection to the API.", "error");
    }
  }

  async function openScan(id) {
    setBusy(true);
    setStatus("Loading scan…", "working");

    try {
      const response = await fetch(`/api/scans/${id}`, { cache: "no-store" });
      if (!response.ok) {
        throw new Error(await readError(response));
      }

      const scan = await response.json();
      state.scan = scan;
      showDashboard(scan);

      if (scan.status === "Completed") {
        setBusy(false);
        setStatus("Scan loaded.", "ok");
        renderScan(scan);
        return;
      }

      if (scan.status === "Failed") {
        setBusy(false);
        setStatus(scan.error || "The scan failed.", "error");
        return;
      }

      poll(scan.id);
    } catch (error) {
      setBusy(false);
      setStatus(error.message || "Could not load the scan.", "error");
    }
  }

  function showDashboard(scan) {
    elements.dashboard.classList.remove("hidden");
    elements.scanSource.textContent = scan.sourceValue || "";
    elements.statusBadge.textContent = scan.status;
    elements.statusBadge.className = `status-badge ${String(scan.status).toLowerCase()}`;
  }

  function renderScan(scan) {
    const summary = scan.summary || {};
    syncScanUrl(scan.id);
    elements.scanTitle.textContent = "Architecture report";
    elements.metricComponents.textContent = summary.components ?? 0;
    elements.metricDependencies.textContent = summary.dependencies ?? 0;
    elements.metricViolations.textContent = summary.violations ?? 0;
    elements.metricCycles.textContent = summary.cycles ?? 0;
    elements.metricViolationsFoot.textContent = summary.errors
      ? `${summary.errors} error${summary.errors === 1 ? "" : "s"}`
      : summary.warnings
        ? `${summary.warnings} warning${summary.warnings === 1 ? "" : "s"}`
        : "none";

    const violationCount = summary.violations ?? 0;
    elements.graphSubtitle.textContent =
      `${summary.components ?? 0} components · ${summary.dependencies ?? 0} dependencies · ${summary.services ?? 0} services`;
    elements.findingsSubtitle.textContent = violationCount
      ? `${violationCount} finding${violationCount === 1 ? "" : "s"} across the scanned repository.`
      : "No rule violations found.";

    renderGraph(scan.graph);
    renderViolations(scan.violations || [], scan.graph);
    elements.reportContent.textContent = scan.reportMarkdown || "Report not available.";
    elements.mermaidContent.textContent = scan.mermaid || "Diagram not available.";
    updateDownloads(scan);
  }

  function renderGraph(graph) {
    const components = graph?.components ?? [];
    const dependencies = graph?.dependencies ?? [];

    if (state.cy) {
      state.cy.destroy();
      state.cy = null;
    }

    if (!window.cytoscape) {
      elements.cy.classList.add("hidden");
      elements.graphEmpty.classList.remove("hidden");
      elements.graphEmpty.textContent = "The graph library could not be loaded.";
      return;
    }

    if (components.length === 0) {
      elements.cy.classList.add("hidden");
      elements.graphEmpty.classList.remove("hidden");
      elements.graphEmpty.textContent = "No components were discovered in this repository.";
      return;
    }

    elements.cy.classList.remove("hidden");
    elements.graphEmpty.classList.add("hidden");

    const elementsData = [
      ...components.map((component) => ({
        data: {
          id: component.id,
          label: component.name,
          kind: component.kind,
          layer: component.layer,
          path: component.projectPath || "",
          meta: component.metadata || {}
        }
      })),
      ...dependencies.map((dependency, index) => ({
        data: {
          id: `edge-${index}`,
          source: dependency.from,
          target: dependency.to,
          kind: dependency.kind,
          evidence: dependency.evidence || ""
        }
      }))
    ];

    state.cy = window.cytoscape({
      container: elements.cy,
      elements: elementsData,
      wheelSensitivity: 0.18,
      style: [
        {
          selector: "node",
          style: {
            "background-color": (element) => kindColors[element.data("kind")] || "#64748b",
            "border-color": "rgba(255,255,255,.35)",
            "border-width": 2,
            "width": 30,
            "height": 30,
            "label": "data(label)",
            "color": "#e6edf7",
            "font-family": "Inter, sans-serif",
            "font-size": 9.5,
            "font-weight": 500,
            "text-valign": "bottom",
            "text-halign": "center",
            "text-margin-y": 9,
            "text-wrap": "wrap",
            "text-max-width": 105,
            "text-outline-width": 2,
            "text-outline-color": "#060b14"
          }
        },
        {
          selector: "node[kind = 'AppHost']",
          style: {
            shape: "diamond",
            width: 36,
            height: 36
          }
        },
        {
          selector: "node[kind = 'Database']",
          style: {
            shape: "cylinder",
            width: 34,
            height: 34
          }
        },
        {
          selector: "node[kind = 'Cache']",
          style: {
            shape: "round-rectangle"
          }
        },
        {
          selector: "node[kind = 'Broker']",
          style: {
            shape: "hexagon"
          }
        },
        {
          selector: "node[kind = 'External']",
          style: {
            "border-style": "dashed",
            opacity: 0.9
          }
        },
        {
          selector: "edge",
          style: {
            width: 1.3,
            "line-color": "rgba(148,163,184,.38)",
            "target-arrow-color": "rgba(148,163,184,.38)",
            "target-arrow-shape": "triangle",
            "curve-style": "bezier",
            "arrow-scale": 0.75,
            opacity: 0.9
          }
        },
        {
          selector: "edge[kind = 'ProjectReference']",
          style: {
            "line-color": "#8b5cf6",
            "target-arrow-color": "#8b5cf6",
            width: 1.6
          }
        },
        {
          selector: "edge[kind = 'PackageReference']",
          style: {
            "line-style": "dashed",
            "line-color": "#64748b",
            "target-arrow-color": "#64748b"
          }
        },
        {
          selector: "edge[kind = 'AspireReference']",
          style: {
            "line-style": "dashed",
            "line-color": "#22d3ee",
            "target-arrow-color": "#22d3ee"
          }
        },
        {
          selector: "node:selected",
          style: {
            "border-width": 4,
            "border-color": "#ffffff"
          }
        }
      ],
      layout: {
        name: "breadthfirst",
        directed: true,
        animate: true,
        animationDuration: 650,
        padding: 44,
        spacingFactor: 1.45,
        avoidOverlap: true,
        grid: false,
        circle: false
      }
    });

    state.cy.on("tap", "node", (event) => {
      showNodeDetail(event.target.data());
    });

    state.cy.on("tap", (event) => {
      if (event.target === state.cy) {
        hideNodeDetail();
      }
    });

    state.cy.fit(undefined, 28);
  }

  function showNodeDetail(data) {
    elements.nodeDetail.replaceChildren();

    const title = document.createElement("h4");
    title.textContent = data.label;
    elements.nodeDetail.append(title);

    const list = document.createElement("dl");
    appendDetail(list, "Kind", data.kind);
    appendDetail(list, "Layer", data.layer);
    if (data.path) {
      appendDetail(list, "Path", data.path);
    }

    Object.entries(data.meta || {}).forEach(([key, value]) => {
      if (value) {
        appendDetail(list, key, value);
      }
    });

    elements.nodeDetail.append(list);
    elements.nodeDetail.classList.remove("hidden");
  }

  function appendDetail(list, key, value) {
    const term = document.createElement("dt");
    term.textContent = key;
    const description = document.createElement("dd");
    description.textContent = value;
    list.append(term, description);
  }

  function hideNodeDetail() {
    elements.nodeDetail.classList.add("hidden");
  }

  function renderViolations(violations, graph) {
    elements.violations.replaceChildren();

    if (violations.length === 0) {
      const empty = document.createElement("div");
      empty.className = "empty-state";
      const title = document.createElement("strong");
      title.textContent = "No violations found";
      const copy = document.createElement("span");
      copy.textContent = "The discovered architecture respects the current rule set.";
      empty.append(title, copy);
      elements.violations.append(empty);
      return;
    }

    const names = new Map((graph?.components ?? []).map((component) => [component.id, component.name]));

    violations.forEach((violation) => {
      const article = document.createElement("article");
      article.className = `violation ${String(violation.severity).toLowerCase()}`;

      const top = document.createElement("div");
      top.className = "violation-top";

      const rule = document.createElement("span");
      rule.className = "rule-id";
      rule.textContent = violation.ruleId;

      const severity = document.createElement("span");
      severity.className = "severity";
      severity.textContent = violation.severity;

      top.append(rule, severity);

      const title = document.createElement("h4");
      title.textContent = violation.title;

      const message = document.createElement("p");
      message.textContent = violation.message;

      article.append(top, title, message);

      const from = violation.from ? names.get(violation.from) || violation.from : null;
      const to = violation.to ? names.get(violation.to) || violation.to : null;

      if (from || to) {
        const path = document.createElement("div");
        path.className = "violation-path";
        path.textContent = [from, to].filter(Boolean).join(" → ");
        article.append(path);
      }

      elements.violations.append(article);
    });
  }

  function updateDownloads(scan) {
    elements.downloadReport.href = `/api/scans/${scan.id}/report`;
    elements.downloadReport.setAttribute("download", `archpulse-${scan.id}.md`);

    if (elements.downloadMermaid.dataset.url) {
      URL.revokeObjectURL(elements.downloadMermaid.dataset.url);
      delete elements.downloadMermaid.dataset.url;
    }

    if (scan.mermaid) {
      const url = URL.createObjectURL(new Blob([scan.mermaid], { type: "text/plain" }));
      elements.downloadMermaid.href = url;
      elements.downloadMermaid.dataset.url = url;
      elements.downloadMermaid.setAttribute("download", `archpulse-${scan.id}.mmd`);
      elements.downloadMermaid.classList.remove("hidden");
    } else {
      elements.downloadMermaid.classList.add("hidden");
    }
  }

  async function loadRecent() {
    try {
      const response = await fetch("/api/scans", { cache: "no-store" });
      if (!response.ok) {
        throw new Error(await readError(response));
      }

      const scans = await response.json();
      elements.recentScans.replaceChildren();

      if (scans.length === 0) {
        const empty = document.createElement("p");
        empty.className = "muted";
        empty.textContent = "No scans yet. Run the demo to see ArchPulse in action.";
        elements.recentScans.append(empty);
        return;
      }

      scans.forEach((scan) => {
        const button = document.createElement("button");
        button.type = "button";
        button.className = "recent-item";

        const dot = document.createElement("span");
        dot.className = `recent-dot ${String(scan.status).toLowerCase()}`;

        const source = document.createElement("span");
        source.className = "recent-source";
        source.textContent = scan.sourceValue;

        const meta = document.createElement("span");
        meta.className = "recent-meta";
        meta.textContent = scan.summary
          ? `${scan.summary.components} components`
          : String(scan.status).toLowerCase();

        const date = document.createElement("span");
        date.className = "recent-date";
        date.textContent = formatDate(scan.createdAt);

        button.append(dot, source, meta, date);
        button.addEventListener("click", () => openScan(scan.id));
        elements.recentScans.append(button);
      });
    } catch {
      elements.recentScans.textContent = "Could not load recent scans.";
    }
  }

  function syncScanUrl(id) {
    const url = new URL(window.location.href);
    url.searchParams.set("scan", id);
    window.history.replaceState(null, "", url);
  }

  function setView(view) {
    document.querySelectorAll(".tab").forEach((tab) => {
      const active = tab.dataset.view === view;
      tab.classList.toggle("active", active);
      tab.setAttribute("aria-selected", String(active));
    });

    ["graph", "report", "mermaid"].forEach((name) => {
      document.getElementById(`view-${name}`).classList.toggle("hidden", name !== view);
    });

    if (view === "graph" && state.cy) {
      setTimeout(() => state.cy.resize(), 0);
    }
  }

  function setBusy(busy) {
    elements.scanButton.disabled = busy;
    elements.demoButton.disabled = busy;
    elements.scanSpinner.classList.toggle("hidden", !busy);
  }

  function setStatus(message, kind = "") {
    elements.status.textContent = message;
    elements.status.dataset.kind = kind;
  }

  async function readError(response) {
    try {
      const body = await response.json();
      if (body.detail) {
        return body.detail;
      }

      if (body.title) {
        return body.title;
      }

      if (body.errors) {
        return Object.values(body.errors).flat().join(" ");
      }
    } catch {
      // Fall through to the generic message.
    }

    return `Request failed (${response.status}).`;
  }

  function formatDate(value) {
    const date = new Date(value);
    return Number.isNaN(date.getTime())
      ? ""
      : date.toLocaleString(undefined, { dateStyle: "medium", timeStyle: "short" });
  }
})();
