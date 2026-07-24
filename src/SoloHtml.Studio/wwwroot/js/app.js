const sourceEl = document.getElementById("source");
const frameEl = document.getElementById("frame");
const errorsEl = document.getElementById("errors");
const metaEl = document.getElementById("meta");
const demoListEl = document.getElementById("demo-list");
const demoTitleEl = document.getElementById("demo-title");

let demos = [];
let activeId = "showcase";
let lastHtml = "";
let timer = null;

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
  await selectDemo(activeId);
}

async function selectDemo(id) {
  activeId = id;
  document.querySelectorAll(".demo-item").forEach((el) => {
    el.classList.toggle("active", el.dataset.id === id);
  });
  const res = await fetch(`/api/demos/${id}`);
  if (!res.ok) return;
  const demo = await res.json();
  demoTitleEl.textContent = demo.title;
  sourceEl.value = demo.source;
  await compile();
}

async function compile() {
  metaEl.textContent = "compiling…";
  const res = await fetch("/api/compile", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ source: sourceEl.value }),
  });
  const data = await res.json();
  if (!data.ok) {
    errorsEl.textContent = (data.errors || []).join("\n");
    metaEl.textContent = "failed";
    frameEl.srcdoc = "<p style='font-family:sans-serif;padding:1rem;color:#b00020'>Compile failed.</p>";
    lastHtml = "";
    return;
  }

  errorsEl.textContent = "";
  lastHtml = data.html;
  frameEl.srcdoc = data.html;
  metaEl.textContent = "live";
}

function downloadHtml() {
  if (!lastHtml) {
    compile().then(() => {
      if (lastHtml) triggerDownload();
    });
    return;
  }
  triggerDownload();
}

function triggerDownload() {
  const blob = new Blob([lastHtml], { type: "text/html;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = `${activeId || "page"}.html`;
  a.click();
  URL.revokeObjectURL(url);
}

document.getElementById("btn-compile").addEventListener("click", compile);
document.getElementById("btn-download").addEventListener("click", downloadHtml);

sourceEl.addEventListener("input", () => {
  clearTimeout(timer);
  timer = setTimeout(compile, 300);
});

sourceEl.addEventListener("keydown", (e) => {
  if ((e.metaKey || e.ctrlKey) && e.key === "Enter") {
    e.preventDefault();
    compile();
  }
  if (e.key === "Tab") {
    e.preventDefault();
    const start = sourceEl.selectionStart;
    const end = sourceEl.selectionEnd;
    sourceEl.value = `${sourceEl.value.substring(0, start)}  ${sourceEl.value.substring(end)}`;
    sourceEl.selectionStart = sourceEl.selectionEnd = start + 2;
  }
});

loadDemos().catch((err) => {
  errorsEl.textContent = String(err);
});
