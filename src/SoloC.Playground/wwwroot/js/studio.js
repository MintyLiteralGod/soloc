const views = {
  landing: document.getElementById("view-landing"),
  studio: document.getElementById("view-studio"),
  html: document.getElementById("view-html"),
  arena: document.getElementById("view-arena"),
};

const sourceEl = document.getElementById("source");
const outputEl = document.getElementById("output");
const diagnosticsEl = document.getElementById("diagnostics");
const demoListEl = document.getElementById("demo-list");
const demoTitleEl = document.getElementById("demo-title");
const runMetaEl = document.getElementById("run-meta");
const arenaLogEl = document.getElementById("arena-log");
const arenaResultEl = document.getElementById("arena-result");

const htmlSourceEl = document.getElementById("html-source");
const htmlDemoListEl = document.getElementById("html-demo-list");
const htmlDemoTitleEl = document.getElementById("html-demo-title");
const htmlPreviewEl = document.getElementById("html-preview");
const htmlDiagnosticsEl = document.getElementById("html-diagnostics");
const htmlMetaEl = document.getElementById("html-meta");

let demos = [];
let htmlDemos = [];
let activeDemoId = "hello";
let activeHtmlDemoId = "showcase";
let htmlPreviewTimer = null;

function showView(name) {
  Object.entries(views).forEach(([key, el]) => {
    el.classList.toggle("active", key === name);
  });
  document.querySelectorAll("[data-nav]").forEach((btn) => {
    btn.classList.toggle("active", btn.getAttribute("data-nav") === name);
  });
  if (name === "arena") {
    syncStatReadouts();
    resetArenaBars();
  }
  if (name === "html") {
    compileHtml();
  }
}

document.querySelectorAll("[data-nav]").forEach((btn) => {
  btn.addEventListener("click", () => showView(btn.getAttribute("data-nav")));
});

async function loadDemos() {
  const res = await fetch("/api/demos");
  demos = await res.json();
  demoListEl.innerHTML = "";
  demos.forEach((demo) => {
    const btn = document.createElement("button");
    btn.type = "button";
    btn.className = "demo-item";
    btn.dataset.id = demo.id;
    btn.innerHTML = `<strong>${demo.title}</strong><span>${demo.blurb}</span>`;
    btn.addEventListener("click", () => selectDemo(demo.id));
    demoListEl.appendChild(btn);
  });
  await selectDemo(activeDemoId);
}

async function loadHtmlDemos() {
  const res = await fetch("/api/html/demos");
  htmlDemos = await res.json();
  htmlDemoListEl.innerHTML = "";
  htmlDemos.forEach((demo) => {
    const btn = document.createElement("button");
    btn.type = "button";
    btn.className = "demo-item";
    btn.dataset.id = demo.id;
    btn.innerHTML = `<strong>${demo.title}</strong><span>${demo.blurb}</span>`;
    btn.addEventListener("click", () => selectHtmlDemo(demo.id));
    htmlDemoListEl.appendChild(btn);
  });
  await selectHtmlDemo(activeHtmlDemoId);
}

async function selectHtmlDemo(id) {
  activeHtmlDemoId = id;
  document.querySelectorAll("#html-demo-list .demo-item").forEach((el) => {
    el.classList.toggle("active", el.dataset.id === id);
  });
  const res = await fetch(`/api/html/demos/${id}`);
  if (!res.ok) return;
  const demo = await res.json();
  htmlDemoTitleEl.textContent = demo.title;
  htmlSourceEl.value = demo.source;
  await compileHtml();
}

async function compileHtml() {
  htmlMetaEl.textContent = "compiling…";
  const res = await fetch("/api/html/compile", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ source: htmlSourceEl.value }),
  });
  const data = await res.json();
  if (!data.ok) {
    htmlDiagnosticsEl.textContent = (data.errors || []).join("\n");
    htmlMetaEl.textContent = "failed";
    htmlPreviewEl.srcdoc = "<p style='font-family:sans-serif;padding:1rem;color:#b00020'>SoloHTML compile failed.</p>";
    return;
  }

  htmlDiagnosticsEl.textContent = "";
  htmlMetaEl.textContent = "live preview";
  htmlPreviewEl.srcdoc = data.html;
}

document.getElementById("btn-html-compile").addEventListener("click", compileHtml);
htmlSourceEl.addEventListener("input", () => {
  clearTimeout(htmlPreviewTimer);
  htmlPreviewTimer = setTimeout(compileHtml, 350);
});
htmlSourceEl.addEventListener("keydown", (e) => {
  if ((e.metaKey || e.ctrlKey) && e.key === "Enter") {
    e.preventDefault();
    compileHtml();
  }
  if (e.key === "Tab") {
    e.preventDefault();
    const start = htmlSourceEl.selectionStart;
    const end = htmlSourceEl.selectionEnd;
    htmlSourceEl.value = `${htmlSourceEl.value.substring(0, start)}  ${htmlSourceEl.value.substring(end)}`;
    htmlSourceEl.selectionStart = htmlSourceEl.selectionEnd = start + 2;
  }
});

loadDemos().catch((err) => {
  diagnosticsEl.textContent = String(err);
});
loadHtmlDemos().catch((err) => {
  htmlDiagnosticsEl.textContent = String(err);
});


async function selectDemo(id) {
  activeDemoId = id;
  document.querySelectorAll("#demo-list .demo-item").forEach((el) => {
    el.classList.toggle("active", el.dataset.id === id);
  });
  const res = await fetch(`/api/demos/${id}`);
  if (!res.ok) return;
  const demo = await res.json();
  demoTitleEl.textContent = demo.title;
  sourceEl.value = demo.source;
  outputEl.textContent = "";
  diagnosticsEl.textContent = "";
  runMetaEl.textContent = "";
}

async function runCode() {
  outputEl.textContent = "Running…";
  diagnosticsEl.textContent = "";
  runMetaEl.textContent = "";
  const res = await fetch("/api/run", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ source: sourceEl.value, fileName: `${activeDemoId}.sc` }),
  });
  const data = await res.json();
  outputEl.textContent = data.output || "(no output)";
  diagnosticsEl.textContent = (data.diagnostics || []).join("\n");
  runMetaEl.textContent = data.ok ? `ok · ${data.engine || "interpreter"}` : "failed";
}

document.getElementById("btn-run").addEventListener("click", runCode);
document.getElementById("btn-clear").addEventListener("click", () => {
  outputEl.textContent = "";
  diagnosticsEl.textContent = "";
  runMetaEl.textContent = "";
});

sourceEl.addEventListener("keydown", (e) => {
  if ((e.metaKey || e.ctrlKey) && e.key === "Enter") {
    e.preventDefault();
    runCode();
  }
  if (e.key === "Tab") {
    e.preventDefault();
    const start = sourceEl.selectionStart;
    const end = sourceEl.selectionEnd;
    sourceEl.value = `${sourceEl.value.substring(0, start)}  ${sourceEl.value.substring(end)}`;
    sourceEl.selectionStart = sourceEl.selectionEnd = start + 2;
  }
});

function readout(prefix, atk, def, lck) {
  return `ATK ${atk} · DEF ${def} · LCK ${lck}`;
}

function syncStatReadouts() {
  const heroAtk = Number(document.getElementById("hero-atk").value);
  const heroDef = Number(document.getElementById("hero-def").value);
  const heroLck = Number(document.getElementById("hero-lck").value);
  const foeAtk = Number(document.getElementById("foe-atk").value);
  const foeDef = Number(document.getElementById("foe-def").value);
  const foeLck = Number(document.getElementById("foe-lck").value);
  document.getElementById("hero-stats").textContent = readout("hero", heroAtk, heroDef, heroLck);
  document.getElementById("foe-stats").textContent = readout("foe", foeAtk, foeDef, foeLck);
}

["hero-atk", "hero-def", "hero-lck", "foe-atk", "foe-def", "foe-lck"].forEach((id) => {
  document.getElementById(id).addEventListener("input", syncStatReadouts);
});

function setBar(fillId, textId, snap) {
  const pct = snap.maxHp === 0 ? 0 : Math.round((snap.hp / snap.maxHp) * 100);
  document.getElementById(fillId).style.width = `${pct}%`;
  document.getElementById(textId).textContent = `${snap.hp} / ${snap.maxHp}`;
}

function resetArenaBars() {
  const heroAtk = Number(document.getElementById("hero-atk").value);
  const heroDef = Number(document.getElementById("hero-def").value);
  const foeAtk = Number(document.getElementById("foe-atk").value);
  const foeDef = Number(document.getElementById("foe-def").value);
  const heroMax = 80 + heroAtk * 4 + heroDef * 3;
  const foeMax = 80 + foeAtk * 4 + foeDef * 3;
  setBar("hero-hp", "hero-hp-text", { hp: heroMax, maxHp: heroMax });
  setBar("foe-hp", "foe-hp-text", { hp: foeMax, maxHp: foeMax });
  arenaLogEl.innerHTML = "";
  arenaResultEl.textContent = "";
}

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function fight() {
  const btn = document.getElementById("btn-fight");
  btn.disabled = true;
  arenaLogEl.innerHTML = "";
  arenaResultEl.textContent = "fighting…";

  const payload = {
    heroName: "Kael",
    heroAtk: Number(document.getElementById("hero-atk").value),
    heroDef: Number(document.getElementById("hero-def").value),
    heroLuck: Number(document.getElementById("hero-lck").value),
    foeName: "Crystal Drake",
    foeAtk: Number(document.getElementById("foe-atk").value),
    foeDef: Number(document.getElementById("foe-def").value),
    foeLuck: Number(document.getElementById("foe-lck").value),
  };

  const res = await fetch("/api/arena/battle", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });
  const battle = await res.json();

  for (const ev of battle.events) {
    const line = document.createElement("div");
    line.className = `log-line ${ev.kind}`;
    line.textContent = ev.text;
    arenaLogEl.appendChild(line);
    arenaLogEl.scrollTop = arenaLogEl.scrollHeight;
    setBar("hero-hp", "hero-hp-text", ev.hero);
    setBar("foe-hp", "foe-hp-text", ev.foe);
    await sleep(ev.kind === "banner" ? 350 : 420);
  }

  arenaResultEl.textContent = battle.heroWon ? `${battle.winner} wins ★` : `${battle.winner} wins`;
  btn.disabled = false;
}

document.getElementById("btn-fight").addEventListener("click", fight);
