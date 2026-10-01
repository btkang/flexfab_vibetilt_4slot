"""SIL 실행 후 결과 폴더 검사 — 잘못된 PASS·중복·보관 경로 확인.

사용: python tools/sil_e2e/scan_result.py <실행 폴더> [<실행 폴더> ...]
출력: Result/ JSON 수·pass_fail 분포·시리얼 중복, Result 아래 FAIL·PASS_DUPLICATE 섞임,
      Result_보관/ FAIL·PASS_DUPLICATE 수
기대(4슬롯 0.2.3.7~): Result/ = 전부 PASS·중복 0·섞임 0, FAIL·이전 결과는 Result_보관/ 에만
"""
import collections
import json
import os
import re
import sys


def lp(p):
    p = os.path.abspath(p)
    return p if os.name != "nt" else "\\\\?\\" + p


def walk_json(top):
    for root, _, files in os.walk(lp(top)):
        for f in files:
            if f.endswith(".json"):
                yield os.path.join(root, f)


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    for run in sys.argv[1:]:
        pf, names, mixed = collections.Counter(), collections.Counter(), 0
        for f in walk_json(os.path.join(run, "Result")):
            d = json.load(open(f, encoding="utf-8-sig"))
            pf[str(d.get("pass_fail"))] += 1
            names[os.path.basename(f)] += 1
            if re.search(r"[\\/](FAIL|PASS_DUPLICATE)[\\/]", f):
                mixed += 1
        dup = sum(1 for v in names.values() if v > 1)
        arch = collections.Counter()
        for f in walk_json(os.path.join(run, "Result_보관")):
            arch[os.path.basename(os.path.dirname(f))] += 1
        print(f"{run}\n  Result json {sum(pf.values())} {dict(pf)}  시리얼중복 {dup}  FAIL/PD 섞임 {mixed}"
              f"\n  Result_보관 {dict(arch) or '없음'}")


if __name__ == "__main__":
    main()
