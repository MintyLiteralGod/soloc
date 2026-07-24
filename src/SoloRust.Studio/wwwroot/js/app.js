
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
    btn.type = "button"; btn.className = "demo-item"; btn.dataset.id = demo.id;
    btn.innerHTML = `<strong>${demo.title}</strong><span>${demo.blurb}</span>`;
    btn.addEventListener("click", () => selectDemo(demo.id));
    demoListEl.appendChild(btn);
  });
  await selectDemo(activeId);
}
async function selectDemo(id) {
  activeId = id;
  document.querySelectorAll(".demo-item").forEach((el) => el.classList.toggle("active", el.dataset.id === id));
  const demo = await (await fetch(`/api/demos/${id}`)).json();
  demoTitleEl.textContent = demo.title;
  sourceEl.value = demo.source;
  await compile();
}
async function compile() {
  metaEl.textContent = "compiling…";
  const data = await (await fetch("/api/compile", {
    method: "POST", headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ source: sourceEl.value, title: activeId }),
  })).json();
  if (!data.ok) {
    errorsEl.textContent = (data.errors || []).join("\n");
    notesEl.textContent = (data.notes || []).join("\n");
    metaEl.textContent = "failed"; lastOut = ""; outputEl.textContent = "";
    return;
  }
  errorsEl.textContent = "";
  notesEl.textContent = (data.notes || []).map(n => "note: " + n).join("\n");
  lastOut = data.rust;
  outputEl.textContent = data.rust;
  metaEl.textContent = "rust source";
}
function download() {
  if (!lastOut) return compile().then(() => lastOut && trigger());
  trigger();
}
function trigger() {
  const blob = new Blob([lastOut], { type: "text/plain;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a"); a.href = url; a.download = `${activeId || "main"}.rs`; a.click();
  URL.revokeObjectURL(url);
}
document.getElementById("btn-compile").addEventListener("click", compile);
document.getElementById("btn-download").addEventListener("click", download);
sourceEl.addEventListener("input", () => { clearTimeout(timer); timer = setTimeout(compile, 300); });
sourceEl.addEventListener("keydown", (e) => {
  if ((e.metaKey || e.ctrlKey) && e.key === "Enter") { e.preventDefault(); compile(); }
  if (e.key === "Tab") {
    e.preventDefault();
    const s = sourceEl.selectionStart, en = sourceEl.selectionEnd;
    sourceEl.value = sourceEl.value.substring(0, s) + "  " + sourceEl.value.substring(en);
    sourceEl.selectionStart = sourceEl.selectionEnd = s + 2;
  }
});
loadDemos().catch((err) => errorsEl.textContent = String(err));
