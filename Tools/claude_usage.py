"""Claude Code 세션별 토큰·용량 정리.

사용법:
  python claude_usage.py
  python claude_usage.py --csv usage.csv
  python claude_usage.py --html usage.html              # 내려받기 폴더의 claude-chat-usage.json도 합침
  python claude_usage.py --html usage.html --chat 경로.json
  python claude_usage.py --serve                         # http://localhost:8765 에서 보기
"""
import csv
import html
import http.server
import json
import sys
import urllib.error
import urllib.request
import webbrowser
from collections import defaultdict
from datetime import datetime
from pathlib import Path

PROJECTS = Path.home() / ".claude" / "projects"


def code_sessions():
    sessions = defaultdict(lambda: {"title": "", "project": "", "start": "", "bytes": 0,
                                    "in": 0, "out": 0, "cache_write": 0, "cache_read": 0})
    for f in PROJECTS.glob("**/*.jsonl"):
        # 서브에이전트 기록(<세션ID>/subagents/*.jsonl)은 부모 세션에 합산
        sid = f.parent.parent.name if f.parent.name == "subagents" else f.stem
        s = sessions[sid]
        s["bytes"] += f.stat().st_size
        seen = set()
        with f.open(encoding="utf-8", errors="replace") as fh:
            for line in fh:
                try:
                    e = json.loads(line)
                except json.JSONDecodeError:
                    continue
                t = e.get("type")
                if t == "custom-title":
                    s["title"] = e.get("customTitle", s["title"])
                elif t == "ai-title" and not s["title"]:
                    s["title"] = e.get("aiTitle", "")
                if not s["project"] and e.get("cwd"):
                    s["project"] = Path(e["cwd"]).name
                if e.get("timestamp") and (not s["start"] or e["timestamp"] < s["start"]):
                    s["start"] = e["timestamp"]
                msg = e.get("message") if t == "assistant" else None
                u = msg.get("usage") if isinstance(msg, dict) else None
                if not u:
                    continue
                # 응답 하나가 여러 줄로 나뉘어 같은 usage가 반복 기록되므로 한 번만 센다
                key = (msg.get("id"), e.get("requestId"))
                if key in seen:
                    continue
                seen.add(key)
                s["in"] += u.get("input_tokens", 0)
                s["out"] += u.get("output_tokens", 0)
                s["cache_write"] += u.get("cache_creation_input_tokens", 0)
                s["cache_read"] += u.get("cache_read_input_tokens", 0)
    rows = []
    for sid, s in sessions.items():
        total = s["in"] + s["out"] + s["cache_write"] + s["cache_read"]
        rows.append({"id": sid, "title": s["title"] or "(제목 없음)", "project": s["project"],
                     "date": s["start"][:10], "bytes": s["bytes"], "input": s["in"], "output": s["out"],
                     "cache_write": s["cache_write"], "cache_read": s["cache_read"], "total": total})
    return rows


def human(n):
    for unit in ("B", "KB", "MB", "GB"):
        if n < 1024:
            return f"{n:.0f}{unit}" if unit == "B" else f"{n:.1f}{unit}"
        n /= 1024
    return f"{n:.1f}TB"


def main(argv):
    rows = sorted(code_sessions(), key=lambda r: r["total"], reverse=True)

    print(f"{'날짜':<11}{'총 토큰':>14}{'출력':>11}{'용량':>9}  제목")
    for r in rows:
        print(f"{r['date']:<11}{r['total']:>14,}{r['output']:>11,}{human(r['bytes']):>9}  [{r['project']}] {r['title'][:60]}")
    print(f"\n세션 {len(rows)}개: 총 {sum(r['total'] for r in rows):,} 토큰, {human(sum(r['bytes'] for r in rows))}")

    if "--csv" in argv and rows:
        path = argv[argv.index("--csv") + 1]
        with open(path, "w", newline="", encoding="utf-8-sig") as fh:
            w = csv.DictWriter(fh, fieldnames=list(rows[0]))
            w.writeheader()
            w.writerows(rows)
        print(f"CSV 저장: {path}")

    chat_path = argv[argv.index("--chat") + 1] if "--chat" in argv else None
    if "--html" in argv:
        path = argv[argv.index("--html") + 1]
        Path(path).write_text(build_page(chat_path, rows), encoding="utf-8")
        print(f"HTML 저장: {path}")
    if "--serve" in argv:
        serve(chat_path)


def build_page(chat_path=None, rows=None):
    if rows is None:
        rows = sorted(code_sessions(), key=lambda r: r["total"], reverse=True)
    chat_path = chat_path or find_chat_json()
    chat = json.loads(Path(chat_path).read_text(encoding="utf-8")) if chat_path else None
    usage, note = live_usage()
    if usage is None and chat and chat.get("usage"):
        usage, note = chat["usage"], f"{chat.get('generated', '')[:16].replace('T', ' ')} UTC 채팅 데이터 기준 ({note})"
    return to_html(rows, chat, usage, note)


def live_usage():
    """Claude Code 로그인 정보로 현재 사용 한도를 가져온다. 실패하면 (None, 이유)."""
    # ponytail: 공개 문서가 없는 엔드포인트라 바뀌면 실패 메시지만 뜸
    cred = Path.home() / ".claude" / ".credentials.json"
    try:
        token = json.loads(cred.read_text(encoding="utf-8"))["claudeAiOauth"]["accessToken"]
    except (OSError, KeyError, ValueError):
        return None, "Claude Code 로그인 정보를 찾지 못했어요"
    req = urllib.request.Request("https://api.anthropic.com/api/oauth/usage", headers={
        "Authorization": f"Bearer {token}", "anthropic-beta": "oauth-2025-04-20"})
    try:
        with urllib.request.urlopen(req, timeout=10) as r:
            return json.load(r), "지금 기준"
    except urllib.error.HTTPError as e:
        if e.code == 401:
            return None, "로그인이 만료됐어요. Claude Code를 한 번 실행하면 갱신돼요"
        return None, f"한도 조회 실패 ({e.code})"
    except OSError as e:
        return None, f"한도 조회 실패 ({e})"


def serve(chat_path, port=8765):
    class Handler(http.server.BaseHTTPRequestHandler):
        def do_GET(self):
            if self.path != "/":
                return self.send_error(404)
            body = build_page(chat_path).encode("utf-8")  # 새로고침할 때마다 다시 집계
            self.send_response(200)
            self.send_header("Content-Type", "text/html; charset=utf-8")
            self.end_headers()
            self.wfile.write(body)

        def log_message(self, *args):
            pass

    # 이 PC에서만 접속되게 127.0.0.1에만 연다
    server = http.server.ThreadingHTTPServer(("127.0.0.1", port), Handler)
    url = f"http://localhost:{port}/"
    print(f"{url} 에서 보고서를 보여주는 중. 끝내려면 이 창을 닫거나 Ctrl+C")
    webbrowser.open(url)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass


def find_chat_json():
    # claude_chat_report.js가 내려받은 파일 중 가장 최근 것. 같은 이름이면 브라우저가 "(1)"을 붙인다
    files = sorted((Path.home() / "Downloads").glob("claude-chat-usage*.json"), key=lambda f: f.stat().st_mtime)
    return str(files[-1]) if files else None


def table(rows, cols):
    head = "".join(f"<th>{label}</th>" for _, label in cols)
    body = ""
    for r in rows:
        cells = ""
        for key, _ in cols:
            v = r.get(key, "")
            if isinstance(v, int):
                shown = human(v) if key == "bytes" else f"{v:,}"
                cells += f'<td class="n" data-v="{v}">{shown}</td>'
            else:
                cells += f"<td>{html.escape(str(v))}</td>"
        body += f"<tr>{cells}</tr>"
    return f"<table><thead><tr>{head}</tr></thead><tbody>{body}</tbody></table>"


def limits_section(usage, note):
    limits = ""
    for k, v in (usage or {}).items():
        if isinstance(v, dict) and isinstance(v.get("utilization"), (int, float)):
            reset = ""
            if v.get("resets_at"):
                local = datetime.fromisoformat(v["resets_at"].replace("Z", "+00:00")).astimezone()
                reset = f" · 초기화 {local:%m/%d %H:%M}"
            label = {"five_hour": "5시간", "seven_day": "7일"}.get(k, k.replace("seven_day_", "7일 · "))
            limits += (f'<div class="lim"><b>{html.escape(label)}</b><span class="bar"><i style="width:{min(v["utilization"], 100)}%">'
                       f"</i></span>{v['utilization']:.0f}%{reset}</div>")
    return (f"<h2>사용 한도 <small>(채팅·Code 공용, {html.escape(note)})</small></h2>"
            + (limits or "<p>사용 한도 정보 없음</p>"))


def chat_section(chat):
    if not chat:
        return ("<h2>Claude 채팅</h2><p>채팅 데이터가 없어요. 크롬 확장 프로그램을 쓰거나, claude.ai 탭 Console에서 "
                "claude_chat_report.js를 실행해 claude-chat-usage.json을 내려받으면 여기에 나와요.</p>")
    rows = sorted(chat.get("chats") or [], key=lambda r: r.get("processed", 0), reverse=True)
    cols = [("updated", "마지막 사용"), ("name", "제목"), ("model", "모델"), ("msgs", "메시지"), ("files", "첨부"),
            ("length", "대화 길이"), ("processed", "누적 처리 추정")]
    total = sum(r.get("processed", 0) for r in rows)
    when = html.escape(chat.get("generated", "")[:16].replace("T", " "))
    return (f"<h2>Claude 채팅 <small>({when} UTC에 받은 데이터)</small></h2>"
            f"<p>대화 {len(rows)}개 · 누적 처리 추정 {total:,} 토큰 (글자 수로 어림한 추정치)</p>" + table(rows, cols))


def to_html(rows, chat=None, usage=None, usage_note=""):
    cols = [("date", "날짜"), ("project", "프로젝트"), ("title", "제목"), ("total", "총 토큰"),
            ("input", "입력"), ("output", "출력"), ("cache_write", "캐시 쓰기"),
            ("cache_read", "캐시 읽기"), ("bytes", "용량")]
    summary = f"세션 {len(rows)}개 · 총 {sum(r['total'] for r in rows):,} 토큰 · {human(sum(r['bytes'] for r in rows))}"
    return f"""<!doctype html><meta charset="utf-8"><title>Claude 사용량</title>
<style>
body{{font:14px system-ui,sans-serif;margin:16px;background:#fff;color:#222}}
table{{border-collapse:collapse;width:100%}}th,td{{padding:4px 8px;border-bottom:1px solid #ddd;text-align:left;white-space:nowrap}}
td:nth-child(2),td:nth-child(3){{white-space:normal}}th{{cursor:pointer;background:#f3f3f3;position:sticky;top:0}}.n{{text-align:right;font-variant-numeric:tabular-nums}}
.lim{{margin:4px 0}}.bar{{display:inline-block;width:200px;height:10px;background:#ddd;margin:0 8px;vertical-align:middle}}.bar i{{display:block;height:100%;background:#d97757}}
small{{font-weight:normal;color:#888}}
@media (prefers-color-scheme:dark){{body{{background:#1e1e1e;color:#ddd}}th{{background:#2c2c2c}}th,td{{border-color:#444}}.bar{{background:#444}}}}
</style>
<p>머리글을 누르면 정렬 · 새로고침하면 다시 집계</p>
{limits_section(usage, usage_note)}
{chat_section(chat)}
<h2>Claude Code</h2><p>{summary} (기록에 남은 실제 값)</p>
{table(rows, cols)}
<script>
document.querySelectorAll("th").forEach(th => th.onclick = () => {{
  const i = th.cellIndex, tb = th.closest("table").tBodies[0], desc = th.dataset.d = th.dataset.d !== "1" ? "1" : "0";
  const val = tr => {{ const c = tr.children[i]; return c.dataset.v !== undefined ? +c.dataset.v : c.textContent; }};
  [...tb.rows].sort((a, b) => (val(a) > val(b) ? 1 : -1) * (desc === "1" ? -1 : 1)).forEach(r => tb.append(r));
}});
</script>"""


if __name__ == "__main__":
    main(sys.argv[1:])
