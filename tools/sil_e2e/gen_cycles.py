"""SIL 사이클 목록 생성 (25종 × 블록): python tools/sil_e2e/gen_cycles.py <232|485> <시작번호> <블록수> <출력파일>
한 줄 = 이름|X1|X2|Y1|Y2|scenario|예아니오|준비동작|기대(&구분)
"""
import sys
proto, start, blocks, outp = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
sfx = '' if proto == '232' else '_485'
PFX = 'VL1-' if proto == '232' else 'VL2-'
n = start
def sn():
    global n
    n += 1
    return f'{PFX}{n:05d}'

lines = []
def L(name, x1, x2, y1, y2, scen='-', pre='', exp='', yn='Y'):
    lines.append('|'.join([name, x1 or '-', x2 or '-', y1 or '-', y2 or '-', scen, yn, pre, exp]))

OK2 = '완성 2대 · X통과 2대 대기'
for b in range(1, blocks + 1):
    p = f'N{b:02d}'
    a, bb = sn(), sn()
    L(f'{p}_01_초', a, bb, None, None, pre='clean;repeat:1;fc:0;unskip', exp='X통과 2대 대기&pending=2')
    c, d = sn(), sn();  L(f'{p}_02_양산', c, d, a, bb, exp=OK2 + '&pending=2')
    e, f = sn(), sn();  L(f'{p}_03_양산', e, f, c, d, exp=OK2 + '&pending=2')
    g, h = sn(), sn();  L(f'{p}_04_X1고장', g, h, e, f, scen=f'scen_x1offset{sfx}.json',
                          exp='X1슬롯 FAIL(FAIL #4)&Y1슬롯 완성&Y2슬롯 완성&pending=1')
    i, j = sn(), sn();  L(f'{p}_05_레인1빈양산', i, j, None, h, exp='Y2슬롯 완성&X통과 2대 대기&pending=2')
    k, l = sn(), sn();  L(f'{p}_06_공통FAIL중단', k, l, i, j, scen='scen_gyro.json', exp='FAIL = 1&pending=2')
    m, o = sn(), sn();  L(f'{p}_07_복원후양산', m, o, i, j, exp=OK2 + '&pending=2')
    q, r = sn(), sn();  L(f'{p}_08_Y중STOP', q, r, m, o, pre='stop:28', exp='nopopup&pending=2')
    s, t = sn(), sn();  L(f'{p}_09_STOP후재검', s, t, m, o, exp=OK2 + '&pending=2')
    u, v = sn(), sn();  L(f'{p}_10_Y1고장', u, v, s, t, scen=f'scen_y1ver{sfx}.json',
                          exp='Y1슬롯 FAIL&Y2슬롯 완성&X통과 2대 대기&pending=2')
    w, x = sn(), sn();  L(f'{p}_11_반복3회X1고장', w, x, u, v, scen=f'scen_rep{sfx}.json', pre='repeat:3',
                          exp='X1슬롯 FAIL&Y1슬롯 완성&Y2슬롯 완성&pending=1')
    L(f'{p}_12_종', None, None, None, x, pre='repeat:1', exp='Y2슬롯 완성&X통과 0대 대기&pending=0')
    # 대기 기록 파일 이상 케이스
    z1 = sn(); a1, a2 = sn(), sn()
    L(f'{p}_13_레인교차', a1, a2, None, z1, pre=f'clean;pend:1:{z1}:ok', exp='Y2슬롯 FAIL(X 기록 없음&pending=2')
    a3 = sn(); a4 = sn()
    L(f'{p}_14_같은보드재투입', a3, a4, None, None, pre=f'clean;pend:1:{a3}:ok', exp='X통과 2대 대기&pending=2')
    a5, a6, a7 = sn(), sn(), sn()
    L(f'{p}_15_구형식_잔존inuse', a5, a7, None, None, pre=f'clean;legacy:1;inuse:2:{a6}', exp='X통과 2대 대기&pending=2')
    b1, b2, b3, b4 = sn(), sn(), sn(), sn()
    L(f'{p}_16_무효_ws_25h', b3, b4, None, None, pre=f'clean;pend:1:{b1}:wsbad;pend:2:{b2}:old25h', exp='X통과 2대 대기&pending=2')
    c1, c2, c3, c4 = sn(), sn(), sn(), sn()
    L(f'{p}_17_무효_결과_시리얼', c3, c4, None, None, pre=f'clean;pend:1:{c1}:noresult;pend:2:{c2}:noserial', exp='X통과 2대 대기&pending=2')
    d1, d2 = sn(), sn()
    L(f'{p}_18_pending저장실패', d1, d2, None, None, pre='clean;tmpdir:1', exp='X1슬롯 FAIL(pending 저장 실패)&X2슬롯 X통과(대기)&pending=1')
    e1, e2 = sn(), sn()
    L(f'{p}_19_결과저장실패', None, None, e1, e2, pre=f'clean;rmtmpdir:1;pend:1:{e1}:ok;pend:2:{e2}:ok;jsondir:{e1}',
      exp='Y1슬롯 FAIL(저장 실패)&Y2슬롯 완성&pending=0')
    f1, f2 = sn(), sn()
    L(f'{p}_20_작업자항목제외', f1, f2, None, None, pre=f'clean;rmjsondir:{e1};skiprow:4',
      exp='X1슬롯 X통과(대기)&X2슬롯 X통과(대기)&!FAIL(Skip&pending=2')   # M5(0.2.2.6): 일반 항목 제외는 판정 제외
    g1, g2 = sn(), sn()
    L(f'{p}_21_Q3기울기샘플부족', g1, g2, None, None, scen=f'scen_every_tilt{sfx}.json', pre='clean;unskip',
      exp='X1슬롯 FAIL&X2슬롯 X통과(대기)&pending=1')
    h1, h2 = sn(), sn()   # bat-001: 같은 날 재완성 → Result_보관/PASS_DUPLICATE
    L(f'{p}_22_재완성_초', h1, h2, None, None, pre='clean', exp='X통과 2대 대기&pending=2')
    L(f'{p}_23_재완성_종1', None, None, h1, h2, exp='완성 2대&pending=0')
    L(f'{p}_24_재완성_다시X', h1, h2, None, None, exp='X통과 2대 대기&pending=2')
    L(f'{p}_25_재완성_종2', None, None, h1, h2, exp='완성 2대&pending=0')

open(outp, 'w', encoding='utf-8').write('\n'.join(lines) + '\n')
print(len(lines), 'cycles, last serial', n)
