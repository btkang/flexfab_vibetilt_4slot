"""두 프로그램 비교(회귀): 같은 사이클 목록을 기준 프로그램(예: 양산 배포본)과 새 프로그램에 돌린 리포트를 사이클별로 비교
python tools/sil_e2e/compare_runs.py <기준 리포트> <신규 리포트> [<기준 실행폴더> <신규 실행폴더>]
비교: 팝업 문구·종료 후 pending·화면 결과 표, (실행 폴더 주면) Result 아래 결과 파일 목록"""
import os, re, sys
sys.stdout.reconfigure(encoding='utf-8')
LP = chr(92) * 2 + '?' + chr(92)

def parse(path):
    cyc, cur = {}, None
    for line in open(path, encoding='utf-8', errors='replace'):
        line = line.rstrip('\n')
        m = re.search(r'===== CYCLE (\S+)', line)
        if m:
            cur = m.group(1); cyc[cur] = {'popup': [], 'pend': '', 'grid': [], 'check': ''}; continue
        if not cur: continue
        body = line[9:].strip() if len(line) > 9 else line
        if body.startswith('[팝업') or body.startswith('[MessageBox]'):
            cyc[cur]['popup'].append(re.sub(r'\d{2}:\d{2}:\d{2}', '', body))
        elif body.startswith('[종료 후 pending]'):
            cyc[cur]['pend'] = body
        elif body.startswith('GRID ') and not body.startswith('GRID 헤더'):
            cyc[cur]['grid'].append(body)
        elif body.startswith('CHECK'):
            cyc[cur]['check'] = body
    return cyc

def results(run):
    out = set()
    for root, _, fs in os.walk(LP + os.path.abspath(os.path.join(run, 'Result'))):
        for f in fs:
            if f.endswith('.json'): out.add(f)
    return out

b = parse(sys.argv[1])
n = parse(sys.argv[2])
same = diff = 0
for k in b:
    if k not in n: print('누락(신규)', k); diff += 1; continue
    d = []
    for f in ('popup', 'pend', 'grid'):
        if b[k][f] != n[k][f]: d.append(f)
    if d:
        diff += 1
        print(f'== {k} 차이: {d}  기준 {b[k]["check"]} / 신규 {n[k]["check"]}')
        for f in d:
            bs, ns = b[k][f], n[k][f]
            if isinstance(bs, list):
                for x in bs:
                    if x not in ns: print('   기준만:', x[:180])
                for x in ns:
                    if x not in bs: print('   신규만:', x[:180])
            else:
                print('   기준:', bs[:180]); print('   신규:', ns[:180])
    else:
        same += 1
print(f'\n사이클 동일 {same} / 차이 {diff}  (기준 {len(b)}, 신규 {len(n)})')
if len(sys.argv) >= 5:
    rb, rn = results(sys.argv[3]), results(sys.argv[4])
    print('결과 파일 기준', sorted(rb)); print('결과 파일 신규', sorted(rn)); print('결과 파일 동일:', rb == rn)
print('신규 CHECK:', sum(1 for v in n.values() if v['check'].startswith('CHECK OK')), 'OK /', sum(1 for v in n.values() if 'MISMATCH' in v['check']), 'MISMATCH')
