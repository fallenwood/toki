import MiniSearch from "minisearch";

console.info("[search] script loaded");

type SearchDoc = {
  id: string;
  title: string;
  url: string;
  description: string;
  date: string;
  tags: string[];
  categories: string[];
  content: string;
  hash: string;
};

type SearchHit = SearchDoc & { score?: number };

export type SearchConfig = {
  enabled: boolean;
  provider: string;
  indexPath: string;
  minChars: number;
  limit: number;
  fuzzy: number;
  baseUrl: string;
};

type RootElements = {
  input: HTMLInputElement;
  panel: HTMLElement;
  list: HTMLElement;
  viewAll?: HTMLAnchorElement;
};

const DATA_ATTRS = {
  enabled: "data-search-enabled",
  provider: "data-search-provider",
  indexPath: "data-search-index",
  minChars: "data-search-min-chars",
  limit: "data-search-limit",
  fuzzy: "data-search-fuzzy",
  baseUrl: "data-base-url",
} as const;

function readConfig(): SearchConfig {
  const body = document.body;
  if (!body) {
    console.error("[search] document.body missing");
  }
  const enabled = body.getAttribute(DATA_ATTRS.enabled) === "true";
  const provider = (body.getAttribute(DATA_ATTRS.provider) ?? "minisearch").toLowerCase();
  const indexPath = body.getAttribute(DATA_ATTRS.indexPath) ?? "search-index.json";
  const minChars = parseInt(body.getAttribute(DATA_ATTRS.minChars) ?? "2", 10);
  const limit = parseInt(body.getAttribute(DATA_ATTRS.limit) ?? "12", 10);
  const fuzzy = parseFloat(body.getAttribute(DATA_ATTRS.fuzzy) ?? "0.2");
  const baseUrl = body.getAttribute(DATA_ATTRS.baseUrl) ?? "/";
  return { enabled, provider, indexPath, minChars, limit, fuzzy, baseUrl };
}

let miniSearch: any | null = null;
let allDocs: SearchDoc[] = [];
let indexPromise: Promise<void> | null = null;

async function ensureIndex(config: SearchConfig) {
  if (miniSearch || allDocs.length) return;
  if (!indexPromise) {
    indexPromise = (async () => {
      const indexUrl = resolveIndexUrl(config);
      console.info("[search] fetching index", indexUrl);
      let res: Response | null = null;
      try {
        res = await fetch(indexUrl, { credentials: "same-origin" });
      } catch (err) {
        console.warn("[search] Fetch failed", err);
      }
      if (!res?.ok) {
        console.error(`[search] Failed to load search index: ${res?.status ?? "(no response)"} from ${indexUrl}`);
        allDocs = [];
        return;
      }
      const rawDocs = (await res.json()) as any[];
      console.info(`[search] loaded ${rawDocs.length} docs`);
      allDocs = rawDocs.map((doc, i) => ({
        id: doc.hash ?? String(i),
        title: doc.title ?? "",
        url: doc.url ?? "",
        description: doc.description ?? "",
        date: doc.date ?? doc.Date ?? "",
        tags: doc.tags ?? [],
        categories: doc.categories ?? [],
        content: doc.content ?? "",
        hash: doc.hash ?? String(i),
      }));

      // Only build MiniSearch when provider is minisearch; otherwise we'll do a simple fallback search
      if (config.provider === "minisearch") {
        miniSearch = new (MiniSearch as any)({
          fields: ["title", "description", "content", "tags", "categories"],
          storeFields: ["title", "url", "description", "date", "tags", "categories", "hash"],
          searchOptions: {
            boost: { title: 4, description: 2, tags: 2, categories: 1.5, content: 1 },
            fuzzy: config.fuzzy,
            prefix: true,
          },
        });

        miniSearch.addAll(allDocs);
      }
    })();
  }
  await indexPromise;
}

function searchDocs(query: string, config: SearchConfig): SearchHit[] {
  if (!allDocs.length) return [];
  if (config.provider === "minisearch" && miniSearch) {
    return (miniSearch.search(query, { prefix: true }) as any[]) ?? [];
  }
  // Lightweight fallback for provider=fuse or when MiniSearch isn't built.
  const needle = query.toLowerCase();
  const scored = allDocs.map((doc) => ({ doc, score: scoreDoc(doc, needle) }))
    .filter((x) => x.score > 0)
    .sort((a, b) => b.score - a.score)
    .map((x) => ({ ...x.doc, score: x.score }));
  return scored;
}

function scoreDoc(doc: SearchDoc, needle: string): number {
  const haystacks: Array<[string, number]> = [
    [doc.title, 5],
    [doc.description, 3],
    [doc.content, 1],
    [(doc.tags ?? []).join(" "), 2],
    [(doc.categories ?? []).join(" "), 1.5],
  ];
  let score = 0;
  for (const [text, weight] of haystacks) {
    if (!text) continue;
    if (text.toLowerCase().includes(needle)) {
      score += weight;
    }
  }
  return score;
}

function resolveIndexUrl(config: SearchConfig): string {
  const path = config.indexPath.startsWith("/") ? config.indexPath : `/${config.indexPath}`;
  // If baseUrl is absolute and same origin, use it; else fall back to current origin.
  try {
    if (config.baseUrl && /^https?:/i.test(config.baseUrl)) {
      const base = new URL(config.baseUrl);
      const current = window.location;
      if (base.host === current.host) {
        return new URL(path, base).toString();
      }
    }
  } catch (_err) {
    // fall through to origin-relative
  }
  // Default to origin root
  return `${window.location.origin}${path}`;
}

function renderResultItem(result: SearchHit): string {
  const date = result.date ? new Date(result.date).toISOString().split("T")[0] : "";
  const tags = (result.tags ?? []).slice(0, 3).join(", ");
  const desc = result.description || result.content?.slice(0, 140) || "";
  return `
    <li>
      <a class="!justify-start flex flex-col gap-1 px-3 py-2" href="${result.url}">
        <span class="font-medium">${escapeHtml(result.title)}</span>
        <span class="text-xs text-base-content/70">${escapeHtml(desc)}</span>
        <span class="text-[11px] text-base-content/60 flex gap-2">${date ? `<time>${date}</time>` : ""}${tags ? `<span>${escapeHtml(tags)}</span>` : ""}</span>
      </a>
    </li>`;
}

function bindRoot(root: RootElements, config: SearchConfig) {
  let currentResults: SearchHit[] = [];
  let activeIndex = -1;

  const perform = async (query: string) => {
    if (!config.enabled) return;
    const q = query.trim();
    if (q.length < config.minChars) {
      currentResults = [];
      activeIndex = -1;
      root.list.innerHTML = `<li class="px-3 py-2 text-sm text-base-content/70">Type at least ${config.minChars} characters</li>`;
      showPanel(root);
      return;
    }
    await ensureIndex(config);
    if (!allDocs.length) {
      root.list.innerHTML = '<li class="px-3 py-2 text-sm text-base-content/70">Search index unavailable</li>';
      showPanel(root);
      return;
    }
    const hits = searchDocs(q, config) ?? [];
    currentResults = hits.slice(0, config.limit) as SearchHit[];
    if (currentResults.length === 0) {
      root.list.innerHTML = '<li class="px-3 py-2 text-sm text-base-content/70">No results</li>';
    } else {
      root.list.innerHTML = currentResults.map(renderResultItem).join("");
    }
    showPanel(root);
  };

  const onInput = (ev: Event) => {
    const value = (ev.target as HTMLInputElement).value;
    void perform(value);
  };

  // Also trigger on Enter for folks who press Enter without arrows
  const onKeyPress = (ev: KeyboardEvent) => {
    if (ev.key === "Enter") {
      ev.preventDefault();
      const value = (ev.target as HTMLInputElement).value;
      void perform(value);
    }
  };

  const onKeyDown = (ev: KeyboardEvent) => {
    if (ev.key === "Escape") {
      hidePanel(root);
      activeIndex = -1;
      return;
    }
    if (!currentResults.length) return;
    if (["ArrowDown", "ArrowUp"].includes(ev.key)) {
      ev.preventDefault();
      activeIndex = navigate(activeIndex, currentResults.length, ev.key === "ArrowDown" ? 1 : -1);
      highlight(root.list, activeIndex);
    }
    if (ev.key === "Enter" && activeIndex >= 0) {
      ev.preventDefault();
      const link = root.list.querySelectorAll<HTMLAnchorElement>("a")[activeIndex];
      link?.click();
    }
  };

  root.input.addEventListener("input", onInput);
  root.input.addEventListener("keypress", onKeyPress);
  root.input.addEventListener("keydown", onKeyDown);

  // Click outside to close
  document.addEventListener("click", (ev) => {
    if (!root.panel.contains(ev.target as Node) && ev.target !== root.input) {
      hidePanel(root);
    }
  });
}

function showPanel(root: RootElements) {
  root.panel.classList.remove("hidden");
}

function hidePanel(root: RootElements) {
  root.panel.classList.add("hidden");
}

function navigate(current: number, total: number, delta: number): number {
  const next = (current + delta + total) % total;
  return next;
}

function highlight(listEl: HTMLElement, index: number) {
  listEl.querySelectorAll("li").forEach((li, i) => {
    li.classList.toggle("bg-base-200", i === index);
  });
}

function escapeHtml(str: string): string {
  return str.replace(/[&<>"']/g, (c) => ({
    "&": "&amp;",
    "<": "&lt;",
    ">": "&gt;",
    '"': "&quot;",
    "'": "&#39;",
  }[c]!));
}

function bindSearchPage(config: SearchConfig) {
  const input = document.getElementById("search-input-page") as HTMLInputElement | null;
  const summary = document.getElementById("search-results-summary");
  const resultsEl = document.getElementById("search-results-page");
  if (!input || !resultsEl || !summary) return;

  // Attach a simple spinner indicator
  const spinnerId = "search-spinner";
  let spinner = document.getElementById(spinnerId);
  if (!spinner) {
    spinner = document.createElement("div");
    spinner.id = spinnerId;
    spinner.className = "hidden w-full flex items-center gap-2 text-sm text-base-content/70";
    spinner.innerHTML = '<span class="loading loading-spinner loading-xs"></span><span>Searching…</span>';
    input.parentElement?.parentElement?.appendChild(spinner);
  }
  const setSpinner = (visible: boolean) => {
    spinner?.classList.toggle("hidden", !visible);
  };

  const renderPageResults = (query: string, results: SearchHit[]) => {
    summary.classList.remove("hidden");
    summary.textContent = `${results.length} result${results.length === 1 ? "" : "s"} for "${query}"`;
    resultsEl.innerHTML = results.map((r) => `
      <article class="card bg-base-100 shadow-sm border border-base-200">
        <div class="card-body">
          <a href="${r.url}" class="card-title text-lg font-semibold">${escapeHtml(r.title)}</a>
          <p class="text-sm text-base-content/70">${escapeHtml(r.description || r.content || "")}</p>
          <div class="flex flex-wrap gap-2 mt-3">
            ${(r.tags ?? []).map((t: string) => `<span class="badge badge-outline badge-sm">${escapeHtml(t)}</span>`).join("")}
          </div>
        </div>
      </article>
    `).join("");
  };

  const doSearch = async (q: string) => {
    const query = q.trim();
    if (query.length < config.minChars) {
      summary.classList.remove("hidden");
      summary.textContent = `Type at least ${config.minChars} characters`;
      resultsEl.innerHTML = "";
      return;
    }
    setSpinner(true);
    await ensureIndex(config);
    setSpinner(false);
    if (!allDocs.length) {
      summary.textContent = `Search index unavailable (check network tab)`;
      summary.classList.remove("hidden");
      resultsEl.innerHTML = "";
      return;
    }
    const hits = searchDocs(query, config) ?? [];
    if (!hits.length) {
      summary.classList.remove("hidden");
      summary.textContent = `No results for "${query}"`;
      resultsEl.innerHTML = "";
      return;
    }
    const results = hits.slice(0, Math.max(config.limit, 50)) as SearchHit[];
    renderPageResults(query, results);
  };

  const params = new URLSearchParams(window.location.search);
  const initial = params.get("q");
  if (initial) {
    input.value = initial;
    void doSearch(initial);
  }

  input.addEventListener("input", (e) => void doSearch((e.target as HTMLInputElement).value));
}

export function initSearch() {
  const config = readConfig();
  console.info("[search] init called", config);
  if (!config.enabled) {
    console.info("[search] disabled via config");
    return;
  }

  // Prefetch index early so typing feels instant
  void ensureIndex(config).catch((err) => {
    console.error("[search] ensureIndex failed", err);
  });

  // Desktop nav
  const dropdown = document.getElementById("search-dropdown");
  if (dropdown) {
    const input = document.getElementById("search-input-desktop") as HTMLInputElement | null;
    const panel = document.getElementById("search-results-panel");
    const list = document.getElementById("search-results-list");
    if (!input || !panel || !list) {
      console.warn("[search] desktop elements missing", { input: !!input, panel: !!panel, list: !!list });
    } else {
      const root: RootElements = {
        input,
        panel,
        list,
        viewAll: document.getElementById("search-view-all") as HTMLAnchorElement,
      };
      bindRoot(root, config);
    }
  } else {
    console.warn("[search] desktop dropdown not found");
  }

  // Mobile
  const mobileInput = document.getElementById("search-input-mobile") as HTMLInputElement | null;
  if (mobileInput) {
    const mobilePanel = document.getElementById("search-results-panel-mobile");
    const mobileList = document.getElementById("search-results-list-mobile");
    if (!mobilePanel || !mobileList) {
      console.warn("[search] mobile panel/list missing", { mobilePanel: !!mobilePanel, mobileList: !!mobileList });
    } else {
      const root: RootElements = {
        input: mobileInput,
        panel: mobilePanel,
        list: mobileList,
        viewAll: document.getElementById("search-view-all-mobile") as HTMLAnchorElement,
      };
      bindRoot(root, config);
    }
  }

  // Shortcut: focus search with "/"
  window.addEventListener("keydown", (ev) => {
    if (ev.key === "/" && !isInputActive(ev)) {
      const desktopInput = document.getElementById("search-input-desktop") as HTMLInputElement | null;
      if (desktopInput) {
        ev.preventDefault();
        desktopInput.focus();
      }
    }
  });

  // Search page
  bindSearchPage(config);
}

function isInputActive(ev: KeyboardEvent): boolean {
  const target = ev.target as HTMLElement | null;
  if (!target) return false;
  const formTags = ["INPUT", "TEXTAREA"]; // allow editing fields
  return formTags.includes(target.tagName) || (target as any).isContentEditable;
}
