
const sourceEl = document.getElementById("source");
const frameEl = document.getElementById("frame");
const errorsEl = document.getElementById("errors");
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
    body: JSON.stringify({ source: sourceEl.value }),
  })).json();
  if (!data.ok) {
    errorsEl.textContent = (data.errors || []).join("\n");
    metaEl.textContent = "failed"; lastOut = "";
    frameEl.srcdoc = "<p style='font-family:sans-serif;padding:1rem;color:#b00020'>Compile failed.</p>";
    return;
  }
  errorsEl.textContent = "";
  lastOut = data.css;
  frameEl.srcdoc = `<!DOCTYPE html><html><head><style>${data.css}</style></head><body>
    <header class="hero"><h1>SoloCSS preview</h1><p>Your stylesheet is live.</p>
    <a class="button" href="#">Primary button</a></header>
    <div class="row"><article class="card"><h3>Card one</h3><p>Nested rules work.</p></article>
    <article class="card"><h3>Card two</h3><p>Media queries too.</p></article></div>
    <nav class="nav"><div class="logo">SoloGem</div><div><a href="#">Docs</a><a href="#">Studio</a><span class="pill">live</span></div></nav>
  </body></html>`;
  metaEl.textContent = "live";
}
function download() {
  if (!lastOut) return compile().then(() => lastOut && trigger());
  trigger();
}
function trigger() {
  const blob = new Blob([lastOut], { type: "text/css;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a"); a.href = url; a.download = `${activeId || "styles"}.css`; a.click();
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
