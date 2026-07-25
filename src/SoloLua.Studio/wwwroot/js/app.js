const sourceEl = document.getElementById("source");
const outputEl = document.getElementById("output");
const errorsEl = document.getElementById("errors");
const notesEl = document.getElementById("notes");
const metaEl = document.getElementById("meta");
const demoListEl = document.getElementById("demo-list");
const demoTitleEl = document.getElementById("demo-title");
let demos = [], activeId = "hello", lastOut = "", timer = null;

async function loadDemos() {
  demos = await (await fetch("/api/demos")).json();
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
  document.querySelectorAll(".demo-item").forEach((el) =>
    el.classList.toggle("active", el.dataset.id === id));
  const demo = await (await fetch(`/api/demos/${id}`)).json();
  demoTitleEl.textContent = demo.title;
  sourceEl.value = demo.source;
  await compile();
}

async function compile() {
  metaEl.textContent = "compiling…";
  const data = await (await fetch("/api/compile", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ source: sourceEl.value, title: activeId }),
  })).json();
  if (!data.ok) {
    errorsEl.textContent = (data.errors || []).join("\n");
    notesEl.textContent = "";
    metaEl.textContent = "failed";
    lastOut = "";
    outputEl.textContent = "";
    return;
  }
  errorsEl.textContent = "";
  lastOut = data.lua;
  outputEl.textContent = data.lua;
  notesEl.textContent = (data.notes || []).map((n) => "· " + n).join("\n");
  metaEl.textContent = `${data.lua.split("\n").length} lines`;
}

document.getElementById("btn-compile").addEventListener("click", compile);
document.getElementById("btn-download").addEventListener("click", () => {
  if (!lastOut) return;
  const blob = new Blob([lastOut], { type: "text/plain" });
  const a = document.createElement("a");
  a.href = URL.createObjectURL(blob);
  a.download = (activeId || "app") + ".lua";
  a.click();
  URL.revokeObjectURL(a.href);
});
sourceEl.addEventListener("input", () => {
  clearTimeout(timer);
  timer = setTimeout(compile, 280);
});
document.addEventListener("keydown", (e) => {
  if ((e.ctrlKey || e.metaKey) && e.key === "Enter") {
    e.preventDefault();
    compile();
  }
});
loadDemos();
