from pathlib import Path
from xml.sax.saxutils import escape
from reportlab.pdfgen import canvas
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.lib.colors import HexColor, Color
from reportlab.platypus import Paragraph, Table, TableStyle
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.enums import TA_LEFT

ROOT = Path(__file__).resolve().parent
OUT = ROOT / '태양이_지지_않는_밤_소개서_그림칸.pdf'
pdfmetrics.registerFont(TTFont('KR', 'C:/Windows/Fonts/malgun.ttf'))
pdfmetrics.registerFont(TTFont('KRB', 'C:/Windows/Fonts/malgunbd.ttf'))
W, H = 595.2756, 841.8898
M, CW = 43, W-86
INK, MUTED = HexColor('#202B3B'), HexColor('#647080')
GOLD, LINE = HexColor('#947442'), HexColor('#D5D9DE')
c = canvas.Canvas(str(OUT), pagesize=(W,H), pageCompression=1)
c.setTitle('태양이 지지 않는 밤 | 게임 소개서')
c.setAuthor('Math RPG 프로젝트')
c.setSubject('대학 게임 동아리 피드백용 소개서 · 그림 삽입 공간 포함')
checks=[]

def txt(s,x,y,size=10.3,font='KR',color=INK):
    c.setFillColor(color); c.setFont(font,size); c.drawString(x,H-y-size*.8,s)

def p(s,x,y,w=CW,size=10.3,leading=16.2,color=INK):
    st=ParagraphStyle('body',fontName='KR',fontSize=size,leading=leading,
        textColor=color,wordWrap='CJK',splitLongWords=True,alignment=TA_LEFT)
    ob=Paragraph(escape(s).replace('\n','<br/>'),st)
    _,h=ob.wrap(w,1000)
    ob.drawOn(c,x,H-y-h)
    checks.append((page,s[:24],y+h))
    return y+h

def prose(items,x,y,w=CW,size=10.3,leading=16.2,gap=10):
    for s in items: y=p(s,x,y,w,size,leading)+gap
    return y

def title(s,y=70,sub=None):
    txt(s,M,y,24,'KRB')
    if sub: txt(sub,M,y+36,9.2,color=MUTED)

def heading(s,y,x=M,size=16): txt(s,x,y,size,'KRB')

def slot(x,y,w,h):
    # The interior stays genuinely empty so the user can add their own image.
    c.setStrokeColor(LINE); c.setLineWidth(.55)
    c.rect(x,H-y-h,w,h,stroke=1,fill=0)

def caption(s,y,x=M,w=CW): return p(s,x,y,w,8,11.8,MUTED)

def table(rows,widths,y,font=9.4,rowheight=None):
    st=ParagraphStyle('cell',fontName='KR',fontSize=font,leading=font*1.45,
        wordWrap='CJK',textColor=INK)
    data=[[Paragraph(escape(str(z)),st) for z in row] for row in rows]
    t=Table(data,colWidths=widths,rowHeights=rowheight)
    t.setStyle(TableStyle([
        ('BACKGROUND',(0,0),(-1,0),HexColor('#E9EDF0')),
        ('VALIGN',(0,0),(-1,-1),'MIDDLE'),
        ('LEFTPADDING',(0,0),(-1,-1),10),('RIGHTPADDING',(0,0),(-1,-1),10),
        ('TOPPADDING',(0,0),(-1,-1),7),('BOTTOMPADDING',(0,0),(-1,-1),7),
        ('LINEBELOW',(0,0),(-1,0),.7,HexColor('#AEB8C2')),
        ('LINEBELOW',(0,1),(-1,-1),.35,LINE),
    ]))
    _,h=t.wrap(CW,1000);t.drawOn(c,M,H-y-h)
    checks.append((page,'table',y+h));return y+h

def begin(n,label):
    global page
    page=n
    txt('태양이 지지 않는 밤',M,27,8,'KRB',GOLD)
    c.setFont('KR',8);c.setFillColor(MUTED);c.drawRightString(W-M,H-33,label)
    c.setStrokeColor(LINE);c.setLineWidth(.5);c.line(M,H-47,W-M,H-47)

def end():
    c.setStrokeColor(LINE);c.line(M,41,W-M,41)
    txt('대학 게임 동아리 피드백용 · 가제',M,H-28,7.5,color=MUTED)
    c.setFont('KR',8);c.setFillColor(MUTED);c.drawRightString(W-M,22,f'{page:02d} / 10')
    c.showPage()

begin(1,'표지 · 한눈에 보기')
txt('태양이 지지 않는 밤',M,72,30,'KRB')
txt('몰락한 태양신이 시간을 다루며 열두 시종과 싸우는 게임',M,119,11,color=MUTED)
slot(M,151,CW,235)
heading('한눈에 보기',421)
prose([
    '「태양이 지지 않는 밤」은 위에서 내려다보는 화면에서 직접 움직이고 공격하는 액션 게임이다. 플레이어는 힘을 빼앗긴 태양신이 되어 밤을 지키는 열두 시종을 상대한다. 시간을 되돌리거나 멈추는 능력을 전투에 이용하고, 방을 통과하며 얻은 강화로 싸우는 방식을 바꾼다.',
    '죽으면 다음 밤에 다시 시작하지만, 되찾은 기억과 영구적인 성장은 남는다. 보스의 공격에 맞았을 때 시간을 되돌리면 상처와 위치를 이전 상태로 돌릴 수 있다. 다만 적에게 입힌 피해도 함께 되돌아간다.'
],M,456)
slot(M,630,CW,105)
caption('탑다운 전투 장면. 이동하면서 적을 겨누고, 공격을 피하며 시간 능력을 사용한다.',744)
end()

begin(2,'한눈에 보기 · 이야기')
title('밤을 이루는 네 장')
slot(M,112,CW,240)
caption('18:00부터 06:00까지 이어지는 시계. 탄생·존재·쇠퇴·종언에 열두 보스가 나뉘어 있다.',362)
p('보스 옆의 시각은 전투 구간의 시작이다. 보스를 쓰러뜨릴 때마다 시계가 다음 시각으로 움직인다. 일반 방을 지나는 동안에는 표시 시각이 바뀌지 않는다.',M,390,size=10,leading=15.5)
heading('낮이 사라진 이유',462)
prose([
    '태양신이 지키던 세계에는 영원한 낮이 있었다. 달의 열두 시종은 자신들이 섬기는 달이 드러날 시간을 원했고, 영원한 밤을 만들기 위해 반역했다.',
    '개기일식이 일어난 날, 시종들은 혼돈의 도움으로 ‘혼돈의 쐐기’를 사용했다. 태양신과 시간 권능의 연결을 끊는 도구였다. 권능은 열두 조각으로 나뉘어 시종들에게 넘어갔고, 태양신은 힘과 기억의 상당 부분을 잃었다.'
],M,496)
slot(M,655,CW,95)
end()

begin(3,'이야기')
title('06:00 다음은 다시 18:00이다')
slot(M,116,CW,255)
caption('낮이 생략된 세계에서 다시 깨어나는 태양신. 결말에서는 밤 뒤에 아침이 찾아온다.',381)
prose([
    '마지막 시종은 ‘시간 순서’라는 권능을 차지했다. 이 힘으로 06:00 다음을 곧바로 18:00에 연결한다. 낮이 통째로 생략되면서 밤만 이어지는 세계가 되었다. 시간은 과거로 돌아가지 않는다. 날짜와 사건의 결과는 이어지고, 시종들도 이전 대결을 기억한다.',
    '태양신에게는 빼앗을 수 없는 힘이 하나 남아 있다. ‘순환’은 몸이 사라져도 다음 18:00에 다시 몸을 만들어 깨어나는 힘이다. 전투에서 죽으면 그 판은 끝나지만, 다음 밤에 다시 도전할 수 있다.',
    '태양신은 자신의 정체와 시종들이 힘을 빼앗았다는 사실은 안다. 반역의 자세한 과정은 기억하지 못한다. 시종들을 쓰러뜨리며 힘과 기억을 되찾는다. 다음 순환에서는 시간 권능이 시종들에게 돌아가지만, 이미 회복한 기억과 영구 성장은 남는다.',
    '마지막 시종을 쓰러뜨리면 낮을 건너뛰던 간섭이 풀린다. 시계는 06:00에 도달하고 결말이 시작된다. 태양신은 다시 영원한 낮을 유지할 수 있지만, 낮과 밤을 열두 시간씩 나누기로 한다. 달이 머물 자리도 남겨 두는 선택이다.'
],M,414,gap=13)
end()

begin(4,'한 판의 흐름')
title('한 판은 이렇게 흐른다')
slot(M,116,CW,351)
caption('18:00에 시작해 일반 방 두 곳과 보스 방 한 곳을 반복한다. 마지막 시종 처치가 결말로 이어진다.',477)
prose([
    '18:00에 깨어나면 역행·정지·가속 가운데 사용할 시간 권능을 고른다. 세 권능은 처음부터 선택할 수 있다. 무기는 처음에 검을 사용할 수 있고, 영구 성장을 통해 활과 망치를 열 수 있다. 고른 무기는 그 판이 끝날 때까지 사용한다.',
    '일반 방에서는 적을 정리하고 다음 전투에 가져갈 강화를 고른다. 이런 방을 두 곳 지나면 보스를 만난다. 보스를 쓰러뜨리면 보상을 얻고 시계가 다음 시각으로 넘어간다. 이 구성을 열두 보스까지 반복하며, 전체는 서른여섯 스테이지다.',
    '예를 들어 검과 역행을 골랐다면, 근접해서 공격하다가 위험한 상황에서 전장을 되돌리는 방식으로 싸울 수 있다. 방을 통과하며 고른 강화가 여기에 더해진다.'
],M,512,gap=12)
end()

begin(5,'한 판의 흐름 · 싸우는 법')
title('다음 밤에 남는 것')
slot(M,115,CW,104)
caption('달은 시종의 권능을 되찾을수록 보름달을 향해 차오른다. 다음 순환에서는 다시 어두워진다.',229)
prose([
    '사망하거나 06:00에 이르면 이번 판이 끝난다. 마지막 시종을 쓰러뜨리면 바로 06:00이 되고 승리의 결말을 본다. 추가로 살아남거나 기다릴 필요는 없다.',
    '판이 끝나면 고른 특성과 모은 유물, 상점에서 쓰는 돈은 사라진다. 되찾은 기억과 영구 성장, 이미 열어 둔 선택지는 남는다. 활을 사용할 수 있게 만든 뒤 쓰러졌다면 다음 판에서도 활을 고를 수 있다.'
],M,261,size=10,leading=15.6,gap=9)
heading('싸우는 법',422)
p('이동 방향과 바라보는 방향을 따로 조작한다. 적을 마우스로 겨눈 채 옆으로 이동하며 공격을 피할 수 있다.',M,453,size=10,leading=15.6)
table([
    ['조작','하는 일'],['WASD','이동'],['마우스 이동','조준'],['좌클릭','기본 공격'],
    ['Space','회피'],['우클릭','시간 권능 사용'],['Q','포션 사용'],['E','통로 이용'],['F','태양핵 설치']
],[150,CW-150],500,font=9.6)
end()

begin(6,'싸우는 법')
title('시간을 쓰는 방식')
gap=13; col=(CW-2*gap)/3
for i in range(3): slot(M+i*(col+gap),113,col,158)
descs=[
    ('역행','전장 전체를 이전 상태로 되돌린다. 공격에 맞은 직후 사용하면 상처와 위치가 돌아가지만, 적에게 입힌 피해도 취소될 수 있다.'),
    ('정지','자신을 제외한 전장의 시간을 멈춘다. 멈춘 적 사이를 움직이며 공격해 두면, 시간이 다시 흐를 때 쌓인 피해가 적용된다.'),
    ('가속','자신의 행동을 빠르게 한다. 이동과 공격, 회피가 빨라져 멀어진 적에게 접근해 공격하거나 위험한 자리에서 빠져나올 수 있다.')]
for i,(name,desc) in enumerate(descs):
    x=M+i*(col+gap);heading(name,286,x,14);p(desc,x,312,col,9.4,14.7)
heading('무기',422)
table([['무기','싸우는 모습'],['검','가까이 붙어 공격하고 적의 반격에 맞춰 빠져나온다.'],
       ['활','거리를 두고 적을 겨누며 공격한다.'],['망치','느린 공격 동작을 감수하고 강한 근접 타격을 노린다.']],
      [75,CW-75],451,font=9.4)
heading('태양핵',583)
p('시간 권능과 별도로 사용하는 설치형 고유 능력이다. 앞에 설치하면 범위 안에서 자신이 강화된다. 태양핵 자체는 적을 공격하지 않는다. 보스 가까이에 설치하고 싸우다가 공격을 피하러 밖으로 나가면 강화도 사라진다.',M,612,size=10,leading=15.4)
heading('일출',686)
p('자정 보스를 처치한 뒤 열리는 능력이다. 태양신의 힘을 불러와 움직임과 공격 방식을 바꾸는 방향으로 잡혀 있다. 구체적인 발동 효과는 아직 정해지지 않았다.',M,715,size=10,leading=15.4)
end()

begin(7,'한 판 안에서 강해지기')
title('보상이 싸우는 방식을 바꾼다')
slot(M,116,CW,204)
caption('태양핵을 동반하며 이동하거나, 설치한 자리에 남은 빛 속에서 공격을 이어갈 수 있다.',330)
prose([
    '‘특성’은 방을 통과한 뒤 골라 능력을 강화하거나 바꾸는 효과다. 같은 특성을 다시 고르면 더 성장한다. ‘유물’은 보스 보상이나 상점에서 구해 추가 효과를 얻는 물건이다. 두 요소는 이번 판이 끝날 때까지 싸우는 방식에 영향을 준다.',
    '‘유랑하는 태양’은 고정된 태양핵이 플레이어를 따라오게 한다. 여기에 범위 안의 이동을 더 빠르게 하는 ‘태양풍’을 더하면, 태양핵을 동반하며 적의 옆으로 돌아 공격하는 조합이 된다.',
    '‘잔존하는 태양’은 태양핵을 더 오래 유지한다. 유물 ‘부서진 해시계’는 태양핵이 사라진 자리에도 강화 효과 일부가 남은 빛을 만든다. 태양핵이 꺼진 뒤에도 그 빛 안에서 공격을 이어갈 수 있다.',
    '‘태양의 성좌’ 유물은 태양핵 범위 밖에서 안으로 들어올 때 일출 게이지를 채운다. 적의 공격을 피해 밖으로 나갔다가 다시 들어오는 움직임이 일출 준비로 이어진다. 일출의 구체적인 발동 효과는 아직 정해지지 않았다.',
    '상점은 방 안에 숨겨진 ‘시간의 통로’를 통해 들어간다. 시간이 멈춘 공간에서 포션과 유물을 살 수 있으며, 특성은 팔지 않는다. 보스전을 앞두고 포션을 마련하거나 이번 판의 능력과 어울리는 유물을 찾는다.',
    '포션은 전투 중 별도의 동작 없이 즉시 체력을 회복한다. 적에게 맞은 뒤 Q를 눌러 사용할 수 있다. 포션이 없거나 체력이 가득 차 있으면 사용되지 않는다.'
],M,362,size=10.1,leading=15.6,gap=9)
end()

begin(8,'열두 시종')
title('밤을 지키는 열두 시종')
slot(M,115,CW,104)
p('시종은 저마다 다른 시간 권능을 지닌다. 아래 시각은 보스 구간이 시작되는 때다. 이야기의 참고 바탕인 ‘암두아트’는 이집트 신화에서 태양신이 밤의 세계를 통과하는 내용을 담고 있다. 개별 보스와 신화의 연결은 구상 중이다.',M,237,size=10,leading=15.5)
rows=[['장','시각','권능','신화의 밤 시간 · 연결은 구상 중'],
['탄생','18:00','축적','첫째 · 저승으로 들어가는 문턱'],
['탄생','19:00','유예','둘째 · 물길과 풍요의 영역'],
['탄생','20:00','역행','셋째 · 물과 재생의 영역'],
['존재','21:00','감속','넷째 · 메마르고 험난한 통로'],
['존재','22:00','공유','다섯째 · 함께 태양의 배를 옮기는 과정'],
['존재','23:00','정지','여섯째 · 태양신과 오시리스의 결합'],
['쇠퇴','00:00','침식','일곱째 · 태양의 항해를 가로막는 위협'],
['쇠퇴','01:00','갱신','여덟째 · 재탄생을 준비하는 존재들'],
['쇠퇴','02:00','가속','아홉째 · 다음 영역으로 나아가는 배'],
['종언','03:00','분기','열째 · 익사한 자의 보호와 눈의 회복'],
['종언','04:00','예지','열한째 · 마지막 통과를 위한 준비'],
['종언','05:00','시간 순서','열두째 · 재생의 관문과 일출']]
y=table(rows,[49,57,70,CW-176],319,font=8.7)
p('최종 시종은 체력이 줄어드는 구간마다 1장, 2장, 3장, 4장에 해당하는 권능을 차례로 사용한다.',M,y+20,size=10,leading=15.5)
end()

begin(9,'열두 시종 · 탄생')
title('차오르는 힘, 뒤늦게 오는 공격')
heading('축적을 만나면',120)
slot(M,154,164,230)
prose([
    '축적은 시간이 흐를수록 힘을 모으는 보스다. 머리 위 게이지가 가득 차면 주변으로 충격파를 방출한다. 공격해서 축적을 줄일 수는 없다. 게이지와 예고를 보고 멀어지거나 회피로 충격파를 통과해야 한다.',
    '힘을 모으는 동안에는 플레이어를 향해 돌진한다. 경로 옆으로 비켜선 뒤에도 게이지를 살펴야 한다. 방출이 끝나면 보스가 잠깐 움직이지 않는다. 충격파를 피하고 다시 다가가 공격하는 틈이다.'
],M+184,154,CW-184,size=10.1,leading=16,gap=14)
c.setStrokeColor(LINE);c.line(M,H-420,W-M,H-420)
heading('유예를 만나면',448)
slot(M,483,164,267)
prose([
    '유예는 일어날 일을 뒤로 미루는 보스다. 날아오는 투사체를 피했더라도 끝이 아니다. 지나간 자리에 남은 점선 궤적이 나중에 다시 공격 범위가 된다.',
    '투사체에 직접 맞으면 바로 피해를 받는다. 뒤늦게 발동한 궤적에 맞으면 피해가 잠시 보류되고, 머리 위 모래시계가 이를 알린다.',
    '그동안 보스를 공격하면 보류된 피해를 줄일 수 있다. 남은 궤적에서 벗어나면서 반격할 기회를 찾아야 한다. 보스를 쓰러뜨리면 아직 적용되지 않은 유예 피해도 사라진다.'
],M+184,483,CW-184,size=10.1,leading=16,gap=13)
end()

begin(10,'열두 시종 · 현재 개발 상태')
title('지나간 자리가 다시 위험해진다')
heading('역행을 만나면',120)
slot(M,154,164,236)
prose([
    '역행 보스가 바닥을 내리치면 고리 모양의 파동이 사방으로 퍼진다. 멀어진 고리는 다시 보스 쪽으로 오므라든다. 바깥으로 나가는 파동을 피했어도, 돌아오는 파동을 다시 살펴야 한다.',
    '전투가 진행되면 플레이어를 과거에 있던 자리로 되돌리는 공격이 추가된다. 돌아갈 위치에는 잔상이 먼저 나타난다. 현재 위치가 안전해도 잔상 쪽으로 파동이 다가오면 위험하다. 위치만 옮겨지며 체력이 되돌아가지는 않는다.'
],M+184,154,CW-184,size=10.1,leading=16,gap=14)
c.setStrokeColor(LINE);c.line(M,H-420,W-M,H-420)
heading('지금 어디까지 만들었나',450)
prose([
    '현재 1장에 해당하는 아홉 스테이지를 플레이할 수 있다. 일반 방 두 곳과 보스 방 한 곳을 지나며 축적·유예·역행을 차례로 만난다. 현재 플레이 범위에서는 검과 역행을 사용한다.',
    '아트는 아직 확정하지 않았다. 게임 안에서는 캐릭터와 공격을 도형으로 대신 표현하고 있다.'
],M,485,size=10.3,leading=16.2,gap=11)
slot(M,604,CW,146)
end()

bad=[a for a in checks if a[2]>783]
if bad: raise RuntimeError(f'Content exceeds page body: {bad}')
c.save()
print(OUT)
print('Pages: 10; text blocks checked:',len(checks))
