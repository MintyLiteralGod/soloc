async function load() {
  const res = await fetch("/api/languages");
  const langs = await res.json();
  const grid = document.getElementById("langs");
  grid.innerHTML = "";
  langs.filter((l) => l.id !== "solopage").forEach((lang, i) => {
    const a = document.createElement("a");
    a.className = "card";
    a.href = `http://localhost:${lang.port}`;
    a.target = "_blank";
    a.rel = "noreferrer";
    a.style.animationDelay = `${i * 60}ms`;
    a.innerHTML = `
      <h3>${lang.name}</h3>
      <div class="ext">${lang.ext}</div>
      <p>${lang.blurb}</p>
      <div class="port">Studio :${lang.port}</div>`;
    grid.appendChild(a);
  });
}
load().catch((err) => {
  document.getElementById("langs").textContent = String(err);
});
