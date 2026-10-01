"""2슬롯 회귀 사이클 (X1칸=X, Y1칸=Y). python tools/sil_e2e/gen_cycles_2slot.py <232|485> <시작번호> <출력>
기대는 판정 폼(Pass Form / Fail Form)만 — 세부 동작은 기존 2슬롯 양산본(1.0.4.4)과 비교로 확인"""
import sys
proto, n, outp = sys.argv[1], int(sys.argv[2]), sys.argv[3]
sfx = '' if proto == '232' else '_485'
pfx = 'VL1-' if proto == '232' else 'VL2-'
def s(i): return f'{pfx}{n + i:05d}'
L = []
def c(name, x, y, scen='-', pre='', exp=''):
    L.append('|'.join([name, x or '-', '-', y or '-', '-', scen, 'Y', pre, exp]))
P, F = 'Pass Form', 'Fail Form&!Pass Form'
c('S01_초', s(1), None, pre='clean;repeat:1', exp=P)
c('S02_양산', s(2), s(1), exp=P)
c('S03_양산_X고장', s(3), s(2), scen=f'scen_x1offset{sfx}.json', exp=F)
c('S04_X고장후_재검', s(3), s(2), exp=P)
c('S05_양산_Y고장', s(4), s(3), scen=f'scen_y1ver{sfx}.json', exp=F)
c('S06_Y고장후_양산', s(5), s(4), exp=P)
c('S07_공통고장_경사계', s(6), s(5), scen='scen_gyro.json', exp=F)
c('S08_공통고장후_양산', s(6), s(5), exp=P)
c('S09_Y중STOP', s(7), s(6), pre='stop:45')
c('S10_STOP후_재검', s(7), s(6), exp=P)
c('S11_반복3회_X고장', s(8), s(7), scen=f'scen_rep{sfx}.json', pre='repeat:3', exp='Fail Form')
c('S12_종', None, s(8), pre='repeat:1')
c('S13_X기록없는_Y', None, s(90), pre='clean')
c('S14_초_재시작', s(9), None, pre='clean', exp=P)
c('S15_같은보드_재투입', s(9), s(9))
c('S16_종', None, s(9))
open(outp, 'w', encoding='utf-8').write('\n'.join(L) + '\n')
print(len(L), 'cycles')
