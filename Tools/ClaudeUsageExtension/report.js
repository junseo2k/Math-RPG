// ponytail: claude.ai 내부 API와 Chrome 폴더 목록 형식에 의존하므로 둘 중 하나가 바뀌면 고쳐야 함
const DEFAULT_PATH = "C:/Users/이준서/.claude/projects";

const esc = s => String(s ?? "").replace(/[&<>"]/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" })[c]);
const num = n => n.toLocaleString();
const human = n => { for (const u of ["B", "KB", "MB", "GB"]) { if (n < 1024) return u === "B" ? `${n}B` : `${n.toFixed(1)}${u}`; n /= 1024; } return `${n.toFixed(1)}TB`; };

// cols: [키, 머리글, 형식] 형식 = "n"(숫자) | "b"(용량) | "w"(줄바꿈 글자) | "c"(선택 상자, 값 = 키) | 없음
function table(rows, cols) {
  const head = cols.map(c => `<th>${c[1]}</th>`).join("");
  const cell = (r, k, f) =>
    f === "c" ? `<td><input type="checkbox" class="pick" value="${esc(r[k])}"></td>` :
    f === "n" || f === "b" ? `<td class="n" data-v="${r[k]}">${f === "b" ? human(r[k]) : num(r[k])}</td>` :
    `<td class="${f || ""}">${esc(r[k])}</td>`;
  const body = rows.map(r => "<tr>" + cols.map(([k, , f]) => cell(r, k, f)).join("") + "</tr>").join("");
  return `<table><thead><tr>${head}</tr></thead><tbody>${body}</tbody></table>`;
}

document.addEventListener("click", e => {
  const th = e.target.closest("th");
  if (!th) return;
  const i = th.cellIndex, tb = th.closest("table").tBodies[0], desc = th.dataset.d = th.dataset.d !== "1" ? "1" : "0";
  const val = tr => { const c = tr.children[i]; return c.dataset.v !== undefined ? +c.dataset.v : c.textContent; };
  [...tb.rows].sort((a, b) => (val(a) > val(b) ? 1 : -1) * (desc === "1" ? -1 : 1)).forEach(r => tb.append(r));
});

// ---------- claude.ai 채팅 ----------

// 영어 약 4자당 1토큰, 한글 등은 1자당 1토큰으로 어림
const estimate = t => { const a = (t.match(/[\x00-\x7f]/g) || []).length; return Math.floor(a / 4) + t.length - a; };
const textOf = m => {
  const parts = (m.content || []).map(c => c.text || c.thinking || (c.input ? JSON.stringify(c.input) : ""));
  if (!parts.length) parts.push(m.text || "");
  for (const a of m.attachments || []) parts.push(a.extracted_content || "");
  return parts.join("\n");
};
const api = async path => {
  const r = await fetch("https://claude.ai/api" + path, { credentials: "include" });
  if (!r.ok) throw new Error(`${r.status} ${path}`);
  return r.json();
};

async function loadChats() {
  const limitsEl = document.getElementById("limits"), chatsEl = document.getElementById("chats");
  let orgs;
  try {
    orgs = await api("/organizations");
  } catch (e) {
    const msg = `<p class="err">claude.ai에 연결하지 못했어요. 이 브라우저에서 claude.ai에 로그인되어 있는지 확인해 주세요. (${esc(e.message)})</p>`;
    limitsEl.innerHTML = chatsEl.innerHTML = msg;
    return;
  }
  const org = (orgs.find(o => (o.capabilities || []).includes("chat")) || orgs[0]).uuid;

  api(`/organizations/${org}/usage`).then(usage => {
    const label = k => ({ five_hour: "5시간", seven_day: "7일" })[k] || k.replace("seven_day_", "7일 · ");
    const items = Object.entries(usage).filter(([, v]) => v && typeof v.utilization === "number");
    limitsEl.innerHTML = items.map(([k, v]) => `<div class="lim"><b>${esc(label(k))}</b><span class="bar"><i style="width:${Math.min(v.utilization, 100)}%"></i></span>` +
      `${v.utilization}%${v.resets_at ? ` · 초기화 ${esc(new Date(v.resets_at).toLocaleString())}` : ""}</div>`).join("") || "<p>사용 한도 정보 없음</p>";
  }).catch(e => { limitsEl.innerHTML = `<p class="err">사용 한도를 가져오지 못했어요. (${esc(e.message)})</p>`; });

  const chats = await api(`/organizations/${org}/chat_conversations`);
  const rows = [];
  let next = 0;
  const worker = async () => {
    while (next < chats.length) {
      const c = chats[next++];
      const row = { id: c.uuid, name: c.name || "(제목 없음)", updated: (c.updated_at || "").slice(0, 10), model: c.model || "", msgs: 0, files: 0, length: 0, processed: 0 };
      try {
        const d = await api(`/organizations/${org}/chat_conversations/${c.uuid}?tree=True&rendering_mode=messages`);
        for (const m of d.chat_messages || []) {
          if (m.sender === "assistant") row.processed += row.length; // 답할 때마다 그때까지의 대화 전체를 다시 읽음
          row.length += estimate(textOf(m));
          row.msgs++;
          row.files += (m.attachments || []).length + (m.files || []).length;
        }
      } catch {
        row.name += " (읽기 실패)";
      }
      rows.push(row);
      chatsEl.textContent = `대화 읽는 중… ${rows.length}/${chats.length}`;
    }
  };
  await Promise.all(Array.from({ length: 4 }, worker));
  rows.sort((a, b) => b.processed - a.processed);
  chatsEl.innerHTML = `<p>대화 ${rows.length}개 · 누적 처리 추정 ${num(rows.reduce((s, r) => s + r.processed, 0))} 토큰 ` +
    `<span class="muted">(글자 수로 어림한 추정치. 누적 처리 = 답할 때마다 그때까지의 대화를 다시 읽은 양의 합)</span></p>` +
    `<p><button id="pickAll">전체 선택/해제</button> <button id="deletePicked">선택한 대화 삭제</button> <span id="deleteMsg"></span></p>` +
    table(rows, [["id", "선택", "c"], ["updated", "마지막 사용"], ["name", "제목", "w"], ["model", "모델"], ["msgs", "메시지", "n"], ["files", "첨부", "n"],
      ["length", "대화 길이", "n"], ["processed", "누적 처리 추정", "n"]]);

  const picks = () => [...chatsEl.querySelectorAll(".pick")];
  document.getElementById("pickAll").onclick = () => {
    const on = picks().some(p => !p.checked);
    picks().forEach(p => (p.checked = on));
  };
  document.getElementById("deletePicked").onclick = async () => {
    const chosen = picks().filter(p => p.checked);
    const msg = document.getElementById("deleteMsg");
    if (!chosen.length) return (msg.textContent = "지울 대화를 먼저 선택해 주세요.");
    if (!confirm(`대화 ${chosen.length}개를 지울까요? 지운 대화는 되돌릴 수 없어요.`)) return;
    let ok = 0;
    for (const p of chosen) {
      const r = await fetch(`https://claude.ai/api/organizations/${org}/chat_conversations/${p.value}`, { method: "DELETE", credentials: "include" });
      if (r.ok) { ok++; p.closest("tr").remove(); }
      msg.textContent = `삭제 중… ${ok}/${chosen.length}`;
    }
    msg.textContent = ok === chosen.length ? `${ok}개 삭제했어요.` : `${ok}/${chosen.length}개 삭제, 나머지는 실패했어요.`;
  };
}

// ---------- Claude Code ----------

// fetch는 file:// 을 지원하지 않아서 XHR을 쓴다
const readFile = url => new Promise((ok, no) => {
  const x = new XMLHttpRequest();
  x.open("GET", url);
  x.onload = () => (x.status === 0 || x.status === 200 ? ok(x.responseText) : no(new Error(x.status)));
  x.onerror = () => no(new Error("읽기 실패"));
  x.send();
});

// Chrome의 file:// 폴더 목록 페이지는 항목마다 addRow("이름","주소",폴더여부,크기,...) 를 넣는다
async function listDir(url) {
  const html = await readFile(url);
  return [...html.matchAll(/addRow\("((?:[^"\\]|\\.)*)","((?:[^"\\]|\\.)*)",(\d),(-?\d+)/g)]
    .map(m => ({ name: JSON.parse(`"${m[1]}"`), url: url + m[2] + (m[3] === "1" && !m[2].endsWith("/") ? "/" : ""), dir: m[3] === "1", size: +m[4] }))
    .filter(e => e.name !== "..");
}

function newSession() {
  return { title: "", project: "", start: "", bytes: 0, input: 0, output: 0, cache_write: 0, cache_read: 0 };
}

// 기록 파일 하나(jsonl)를 세션 합계에 더한다
function addTranscript(s, text) {
  const seen = new Set();
  for (const line of text.split("\n")) {
    if (!line) continue;
    let e;
    try { e = JSON.parse(line); } catch { continue; }
    if (e.type === "custom-title") s.title = e.customTitle || s.title;
    else if (e.type === "ai-title" && !s.title) s.title = e.aiTitle || "";
    if (!s.project && e.cwd) s.project = e.cwd.split(/[\\/]/).pop();
    if (e.timestamp && (!s.start || e.timestamp < s.start)) s.start = e.timestamp;
    const u = e.type === "assistant" && e.message && e.message.usage;
    if (!u) continue;
    // 응답 하나가 여러 줄로 나뉘어 같은 usage가 반복 기록되므로 한 번만 센다
    const key = `${e.message.id}|${e.requestId}`;
    if (seen.has(key)) continue;
    seen.add(key);
    s.input += u.input_tokens || 0;
    s.output += u.output_tokens || 0;
    s.cache_write += u.cache_creation_input_tokens || 0;
    s.cache_read += u.cache_read_input_tokens || 0;
  }
}

async function loadCode(path) {
  const el = document.getElementById("code");
  const root = encodeURI("file:///" + path.replace(/\\/g, "/").replace(/^\/+/, "").replace(/\/?$/, "/"));
  const files = []; // [세션ID, 파일]
  try {
    for (const proj of (await listDir(root)).filter(e => e.dir)) {
      for (const e of await listDir(proj.url)) {
        if (!e.dir && e.name.endsWith(".jsonl")) files.push([e.name.slice(0, -6), e]);
        else if (e.dir) {
          // 서브에이전트 기록(<세션ID>/subagents/*.jsonl)은 부모 세션에 합산
          const sub = (await listDir(e.url)).find(x => x.dir && x.name === "subagents");
          if (sub) for (const f of await listDir(sub.url)) if (f.name.endsWith(".jsonl")) files.push([e.name, f]);
        }
      }
    }
  } catch (e) {
    el.innerHTML = `<p class="err">기록 폴더를 읽지 못했어요. chrome://extensions에서 이 확장 프로그램의 <b>세부정보 → 파일 URL에 대한 액세스 허용</b>을 켰는지, 폴더 경로가 맞는지 확인해 주세요. (${esc(e.message)})</p>`;
    return;
  }
  const sessions = new Map();
  let done = 0;
  for (const [sid, f] of files) {
    if (!sessions.has(sid)) sessions.set(sid, newSession());
    const s = sessions.get(sid);
    s.bytes += f.size;
    try { addTranscript(s, await readFile(f.url)); } catch { /* 읽는 중 지워진 파일 등은 건너뜀 */ }
    el.textContent = `기록 읽는 중… ${++done}/${files.length}`;
  }
  const rows = [...sessions.values()].map(s => ({ ...s, title: s.title || "(제목 없음)", date: s.start.slice(0, 10),
    total: s.input + s.output + s.cache_write + s.cache_read })).sort((a, b) => b.total - a.total);
  el.innerHTML = `<p>세션 ${rows.length}개 · 총 ${num(rows.reduce((s, r) => s + r.total, 0))} 토큰 · ${human(rows.reduce((s, r) => s + r.bytes, 0))} ` +
    `<span class="muted">(기록에 남은 실제 값)</span></p>` +
    table(rows, [["date", "날짜"], ["project", "프로젝트", "w"], ["title", "제목", "w"], ["total", "총 토큰", "n"], ["input", "입력", "n"],
      ["output", "출력", "n"], ["cache_write", "캐시 쓰기", "n"], ["cache_read", "캐시 읽기", "n"], ["bytes", "용량", "b"]]);
}

// ---------- 시작 ----------

async function main() {
  const input = document.getElementById("path");
  const { path = DEFAULT_PATH } = await chrome.storage.local.get("path");
  input.value = path;
  document.getElementById("savePath").onclick = async () => {
    await chrome.storage.local.set({ path: input.value.trim() });
    loadCode(input.value.trim());
  };
  document.getElementById("reload").onclick = () => location.reload();
  loadChats();
  loadCode(path);
}

if (typeof chrome !== "undefined" && chrome.storage) main();
