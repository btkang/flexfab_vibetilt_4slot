# md2html — 마크다운 → HTML 변환 도구

PLAN/리포트 등 .md 문서를 같은 폴더의 .html로 변환. 검토 시 .html을 더블클릭하면 브라우저에서 바로 열림.

## 셋업 (1회)

1. **Python markdown 패키지 설치** (이미 됨)
   ```powershell
   python -m pip install markdown
   ```

2. **우클릭 메뉴 등록** (선택)
   - `add_md2html_context.reg` 더블클릭 → "예" 클릭
   - 이후 탐색기에서 .md 우클릭 → **"HTML 만들기"** 메뉴 표시

## 사용 방법

### A. 우클릭 메뉴 (셋업 후)
탐색기에서 .md 파일 우클릭 → "HTML 만들기" 클릭 → 같은 폴더에 .html 생성

### B. 드래그앤드롭
.md 파일을 `md2html.cmd` 위로 드래그 → 같은 폴더에 .html 생성

### C. 명령행
```powershell
python tools/md2html/md2html.py "docs/.../file.md"
```

### D. Claude가 자동 생성
Claude가 PLAN/리포트 .md 만들 때 자동으로 .html도 같이 생성됨.

## 제거

우클릭 메뉴를 빼고 싶으면 `remove_md2html_context.reg` 더블클릭.

## 트러블슈팅

| 증상 | 해결 |
|---|---|
| `ModuleNotFoundError: markdown` | `python -m pip install markdown` |
| `python is not recognized` | Python을 PATH에 추가 또는 재설치 |
| 우클릭 메뉴 안 보임 | `add_md2html_context.reg` 더블클릭 누락. 또는 다른 PC면 .reg 안의 경로 수정 필요 |
| 한글 깨짐 | 콘솔에 `chcp 65001` 후 다시 시도 |
