// claude.ai 채팅별 사용량 보고서. claude.ai 탭에서 F12 → Console에 붙여 넣고 Enter.
// 대화마다 메시지 수, 추정 토큰, 마지막 사용일과 현재 사용 한도를 HTML로 보여준다.
// ponytail: claude.ai 내부 API를 쓰므로 사이트가 바뀌면 동작하지 않을 수 있음
(async () => {
  if (location.hostname !== "claude.ai") return alert("claude.ai 페이지를 연 탭에서 실행해 주세요.");

  // 영어 약 4자당 1토큰, 한글 등은 1자당 1토큰으로 어림
  const estimate = t => { const a = (t.match(/[\x00-\x7f]/g) || []).length; return Math.floor(a / 4) + t.length - a; };
  const textOf = m => {
    const parts = (m.content || []).map(c => c.text || c.thinking || (c.input ? JSON.stringify(c.input) : ""));
    if (!parts.length) parts.push(m.text || "");
    for (const a of m.attachments || []) parts.push(a.extracted_content || "");
    return parts.join("\n");
  };
  const esc = s => String(s ?? "").replace(/[&<>"]/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" })[c]);
  const getJson = async url => { const r = await fetch(url); if (!r.ok) throw new Error(`${r.status} ${url}`); return r.json(); };

  const orgs = await getJson("/api/organizations");
  const org = (orgs.find(o => (o.capabilities || []).includes("chat")) || orgs[0]).uuid;
  const base = `/api/organizations/${org}`;
  const chats = await getJson(`${base}/chat_conversations`);
  const usage = await getJson(`${base}/usage`).catch(() => null);

  const rows = [];
  let next = 0;
  const worker = async () => {
    while (next < chats.length) {
      const c = chats[next++];
      const row = { name: c.name || "(제목 없음)", updated: (c.updated_at || "").slice(0, 10), model: c.model || "", msgs: 0, length: 0, processed: 0, files: 0 };
      try {
        const d = await getJson(`${base}/chat_conversations/${c.uuid}?tree=True&rendering_mode=messages`);
        for (const m of d.chat_messages || []) {
          const tok = estimate(textOf(m));
          if (m.sender === "assistant") row.processed += row.length; // 답할 때마다 그때까지의 대화 전체를 다시 읽음
          row.length += tok;
          row.msgs++;
          row.files += (m.attachments || []).length + (m.files || []).length;
        }
      } catch (e) {
        row.error = e.message;
      }
      rows.push(row);
      console.log(`${rows.length}/${chats.length} ${row.name}`);
    }
  };
  await Promise.all(Array.from({ length: 4 }, worker));
  rows.sort((a, b) => b.processed - a.processed);

  const limits = usage ? Object.entries(usage).filter(([, v]) => v && typeof v.utilization === "number") : [];
  const limitHtml = limits.length
    ? limits.map(([k, v]) => `<div class="lim"><b>${esc(k)}</b><div class="bar"><i style="width:${Math.min(v.utilization, 100)}%"></i></div>${v.utilization}%${v.resets_at ? ` · 초기화 ${esc(new Date(v.resets_at).toLocaleString())}` : ""}</div>`).join("")
    : "<p>사용 한도 정보를 가져오지 못했어요.</p>";
  const sum = k => rows.reduce((s, r) => s + r[k], 0).toLocaleString();
  const body = rows.map(r => `<tr><td>${esc(r.updated)}</td><td>${esc(r.name)}${r.error ? ` <small>(읽기 실패)</small>` : ""}</td><td>${esc(r.model)}</td>` +
    `<td class="n">${r.msgs}</td><td class="n">${r.files}</td><td class="n">${r.length.toLocaleString()}</td><td class="n">${r.processed.toLocaleString()}</td></tr>`).join("");

  const html = `<!doctype html><meta charset="utf-8"><title>Claude 채팅 사용량</title>
<style>
body{font:14px system-ui,sans-serif;margin:16px;background:#fff;color:#222}
table{border-collapse:collapse;width:100%}th,td{padding:4px 8px;border-bottom:1px solid #ddd;text-align:left;white-space:nowrap}
td:nth-child(2){white-space:normal}th{background:#f3f3f3;position:sticky;top:0}.n{text-align:right;font-variant-numeric:tabular-nums}
.lim{margin:4px 0}.bar{display:inline-block;width:200px;height:10px;background:#ddd;margin:0 8px;vertical-align:middle}.bar i{display:block;height:100%;background:#d97757}
@media (prefers-color-scheme:dark){body{background:#1e1e1e;color:#ddd}th{background:#2c2c2c}th,td{border-color:#444}.bar{background:#444}}
</style>
<h2>Claude 채팅 사용량</h2>
<h3>현재 사용 한도</h3>${limitHtml}
<h3>대화 ${rows.length}개</h3>
<p>대화 길이 합 ${sum("length")} · 누적 처리 추정 합 ${sum("processed")} 토큰. 토큰은 글자 수로 어림한 추정치예요.<br>
누적 처리 = 답할 때마다 그때까지의 대화를 다시 읽은 양의 합. 시스템 프롬프트와 이미지는 빠져 있어요.</p>
<table><thead><tr><th>마지막 사용</th><th>제목</th><th>모델</th><th>메시지</th><th>첨부</th><th>대화 길이</th><th>누적 처리 추정</th></tr></thead><tbody>${body}</tbody></table>`;

  const url = URL.createObjectURL(new Blob([html], { type: "text/html" }));
  const a = Object.assign(document.createElement("a"), { href: url, download: "claude-chat-usage.html" });
  if (!window.open(url)) a.click(); // 팝업이 막히면 파일로 내려받기
  // claude_usage.py가 Claude Code 기록과 합칠 때 읽는 파일
  const data = JSON.stringify({ generated: new Date().toISOString(), usage, chats: rows });
  Object.assign(document.createElement("a"), {
    href: URL.createObjectURL(new Blob([data], { type: "application/json" })), download: "claude-chat-usage.json",
  }).click();
  console.log("보고서를 새 탭에 열었어요. 안 보이면 내려받은 claude-chat-usage.html을 여세요.");
})();
