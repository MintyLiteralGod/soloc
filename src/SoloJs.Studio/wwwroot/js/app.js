
const sourceEl = document.getElementById("source");
const outputEl = document.getElementById("output");
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
    body: JSON.stringify({ source: sourceEl.value, title: activeId }),
  })).json();
  if (!data.ok) {
    errorsEl.textContent = (data.errors || []).join("\n");
    metaEl.textContent = "failed"; lastOut = ""; outputEl.textContent = "";
    return;
  }
  errorsEl.textContent = "";
  lastOut = data.javaScript;
  outputEl.textContent = data.javaScript;
  const interactive = activeId === "dom";
  frameEl.srcdoc = interactive ? `<!DOCTYPE html><html><body style="font-family:Segoe UI,sans-serif;padding:1.5rem;background:#f4fff8;color:#102018">
    <h1>SoloJS live</h1>
    <p id="out">…</p>
    <p>Score: <strong id="score">0</strong></p>
    <button id="btn" style="padding:.7rem 1rem;border:0;border-radius:.5rem;background:#d8ff3e;font-weight:700;cursor:pointer">Click me</button>
    <script>${data.javaScript.replace(/<\/script/gi,'<\\/script')}<\/script>
  </body></html>` : `<!DOCTYPE html><html><body style="font-family:Segoe UI,sans-serif;padding:1.5rem;background:#102018;color:#e9f8ef">
    <h1>Console preview</h1>
    <pre id="log" style="white-space:pre-wrap"></pre>
    <script>
      const log = document.getElementById("log");
      const old = console.log;
      console.log = (...args) => { log.textContent += args.join(" ") + "\\n"; old(...args); };
      ${data.javaScript}
    <\/script>
  </body></html>`;
  metaEl.textContent = "live";
}
function download() {
  if (!lastOut) return compile().then(() => lastOut && trigger());
  trigger();
}
function trigger() {
  const blob = new Blob([lastOut], { type: "text/javascript;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a"); a.href = url; a.download = `${activeId || "app"}.js`; a.click();
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
