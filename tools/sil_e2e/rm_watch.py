"""밤샘 SIL 진행 요약을 레드마인 #4901 댓글로 2시간마다 자동 등록 (Claude 세션과 무관하게 동작)
python rm_watch.py <종료시각 'YYYY-MM-DD HH:MM'> <report파일...>
"""
import os, sys, time, re, datetime
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import rm   # tools/sil_e2e/rm.py (API 키: ~/.claude/redmine.env — 레포에 넣지 말 것)

ISSUE = 4901
INTERVAL = 2 * 3600
end = datetime.datetime.strptime(sys.argv[1], '%Y-%m-%d %H:%M')
reports = sys.argv[2:]

def summarize():
    rows = []
    for rp in reports:
        name = os.path.basename(rp)
        if not os.path.exists(rp):
            rows.append(f'| {name} | 시작 전 | | | | |'); continue
        txt = open(rp, encoding='utf-8', errors='replace').read()
        cycles = txt.count('===== CYCLE')
        ok = txt.count('CHECK OK')
        mm, cur = [], '-'
        for line in txt.splitlines():
            m1 = re.search(r'===== CYCLE (\S+)', line)
            if m1: cur = m1.group(1)
            m2 = re.search(r'CHECK MISMATCH: (.*)', line)
            if m2: mm.append((cur, m2.group(1)))
        nmis = len(mm)
        mem = re.findall(r'\[MEM\] ([^\n]*?)  누적', txt)
        last = re.findall(r'===== CYCLE (\S+)', txt)
        done = '마감 시각' in txt
        rows.append(f'| {name} | {cycles} | {ok} | {nmis} | {mem[-1] if mem else "-"} | {(last[-1] if last else "-")}{" (종료)" if done else ""} |')
        for cyc, why in mm[-5:]:
            rows.append(f'|   불일치 | {cyc} | {why[:120]} | | | |')
    return rows

def post(title, final=False):
    rows = summarize()
    note = f'h3. {title}\n\n|_. 파일 |_. 사이클 |_. OK |_. 불일치 |_. 메모리(마지막) |_. 마지막 사이클 |\n' + '\n'.join(rows)
    note += '\n\n(자동 기록 — tools/sil_e2e/rm_watch.py, 2시간 간격)'
    ups = []
    if final:
        for rp in reports:
            if os.path.exists(rp):
                try: ups.append(rm.upload(rp))
                except Exception: pass
    try:
        body = {'issue': {'notes': note}}
        if ups: body['issue']['uploads'] = ups
        rm.req('PUT', f'/issues/{ISSUE}.json', body)
    except Exception as e:
        open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'rm_watch.err'), 'a', encoding='utf-8').write(f'{datetime.datetime.now()} {e}\n')

post(f'밤샘 SIL(Soak) 시작 {datetime.datetime.now():%m-%d %H:%M} — 종료 예정 {end:%m-%d %H:%M}')
nxt = time.time() + INTERVAL
while datetime.datetime.now() < end:
    time.sleep(60)
    if time.time() >= nxt:
        post(f'밤샘 SIL 진행 {datetime.datetime.now():%m-%d %H:%M}')
        nxt += INTERVAL
post(f'밤샘 SIL 종료 {datetime.datetime.now():%m-%d %H:%M} — 자동 요약 (상세 판정은 별도 결과 문서)', final=True)
