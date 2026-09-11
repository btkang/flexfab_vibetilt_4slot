"""
md2html.py — Markdown 파일을 같은 폴더의 HTML로 변환.

Usage:
    python md2html.py <path/to/file.md>

생성: 같은 폴더에 file.html
"""
import sys
from pathlib import Path

try:
    import markdown
except ImportError:
    print("ERROR: markdown 패키지가 없습니다.")
    print("설치: python -m pip install markdown")
    sys.exit(2)


CSS = """
body { font-family: 'Malgun Gothic', '맑은 고딕', sans-serif;
       max-width: 1000px; margin: 30px auto; padding: 0 25px;
       line-height: 1.65; color: #333; background: #fafafa; }
h1 { border-bottom: 3px solid #00695c; padding-bottom: 10px; color: #00695c; }
h2 { border-left: 5px solid #00897b; padding-left: 12px; color: #00695c; margin-top: 35px; }
h3 { color: #424242; margin-top: 22px; }
h4 { color: #555; margin-top: 16px; }
table { border-collapse: collapse; width: 100%; margin: 12px 0; background: white; }
th, td { border: 1px solid #ddd; padding: 8px 12px; text-align: left; vertical-align: top; }
th { background-color: #e0f2f1; font-weight: 600; }
code { background: #f5f5f5; padding: 2px 6px; border-radius: 3px;
       font-family: 'Consolas', monospace; font-size: 0.92em; }
pre { background: #263238; color: #aed581; padding: 15px; border-radius: 5px;
      overflow-x: auto; font-size: 0.85em; line-height: 1.4; }
pre code { background: transparent; color: inherit; padding: 0; }
blockquote { border-left: 4px solid #00897b; padding: 6px 14px; color: #555;
             background: #e0f2f1; margin: 12px 0; }
a { color: #00695c; }
ul, ol { margin: 6px 0 6px 22px; }
li { margin: 3px 0; }
hr { border: none; border-top: 1px solid #ccc; margin: 24px 0; }
"""


def convert(src_path: Path) -> Path:
    if not src_path.exists():
        raise FileNotFoundError(src_path)
    md_text = src_path.read_text(encoding="utf-8")
    html_body = markdown.markdown(
        md_text,
        extensions=["tables", "fenced_code", "sane_lists", "toc"],
    )
    title = src_path.stem
    out_path = src_path.with_suffix(".html")
    out_path.write_text(
        f"""<!DOCTYPE html>
<html lang="ko">
<head>
<meta charset="UTF-8">
<title>{title}</title>
<style>{CSS}</style>
</head>
<body>
{html_body}
</body>
</html>
""",
        encoding="utf-8",
    )
    return out_path


def main():
    if len(sys.argv) < 2:
        print("Usage: python md2html.py <file.md>")
        sys.exit(1)
    src = Path(sys.argv[1])
    out = convert(src)
    print(f"OK -> {out}")


if __name__ == "__main__":
    main()
