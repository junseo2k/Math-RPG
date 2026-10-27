// claude.ai 채팅 일괄 삭제. claude.ai에 로그인한 탭에서 F12 → Console에 붙여 넣고 Enter.
// 제목에 KEEP 단어가 들어간 대화는 남긴다. 지우기 전에 목록을 보여주고 확인을 받는다.
// ponytail: claude.ai 내부 API를 쓰므로 사이트가 바뀌면 동작하지 않을 수 있음
(async () => {
  const KEEP = ["시간", "메카", "Meka", "로그라이크", "태양"];
  if (location.hostname !== "claude.ai") return alert("claude.ai 페이지를 연 탭에서 실행해 주세요.");

  const orgs = await (await fetch("/api/organizations")).json();
  const org = (orgs.find(o => (o.capabilities || []).includes("chat")) || orgs[0]).uuid;
  const base = `/api/organizations/${org}/chat_conversations`;
  const chats = await (await fetch(base)).json();

  const keep = c => KEEP.some(k => (c.name || "").toLowerCase().includes(k.toLowerCase()));
  const targets = chats.filter(c => !keep(c));

  console.log(`남김 ${chats.length - targets.length}개`);
  console.table(chats.filter(keep).map(c => ({ 제목: c.name, 날짜: c.updated_at?.slice(0, 10) })));
  console.log(`삭제 대상 ${targets.length}개`);
  console.table(targets.map(c => ({ 제목: c.name, 날짜: c.updated_at?.slice(0, 10) })));

  if (!targets.length) return console.log("지울 대화가 없어요.");
  // 확인 창이 막히는 경우가 있어서, 목록을 본 뒤 직접 함수를 불러 지우게 한다
  window.deleteChats = async () => {
    let done = 0;
    for (const c of targets) {
      const r = await fetch(`${base}/${c.uuid}`, { method: "DELETE" });
      if (r.ok) done++;
      else console.warn("실패:", c.name, r.status);
    }
    console.log(`${done}/${targets.length}개 삭제 완료`);
  };
  console.log(`목록이 맞으면 deleteChats() 를 입력하고 Enter. 되돌릴 수 없어요.`);
})();
