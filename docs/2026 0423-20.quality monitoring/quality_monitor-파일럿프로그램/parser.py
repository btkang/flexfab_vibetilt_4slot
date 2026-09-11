"""Result JSON 파싱 → 평면화된 측정 이벤트 DataFrame."""
from __future__ import annotations

import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Iterable

import pandas as pd

TILT_IDS = {"MOTION_TILT_X_485": "X", "MOTION_TILT_Y_485": "Y"}
VIBE_IDS = {
    "MOTION_VIBE_X_485": "X",
    "MOTION_VIBE_Y_485": "Y",
    "MOTION_VIBE_Z_485": "Z",
}


def _parse_time(raw: str) -> datetime | None:
    if not raw:
        return None
    try:
        return datetime.fromisoformat(raw.replace("Z", "+00:00")).astimezone(timezone.utc)
    except ValueError:
        return None


def _angle_from_item(item: str) -> float | None:
    if not item:
        return None
    for token in item.replace("도", "").split():
        try:
            return float(token.replace("+", ""))
        except ValueError:
            continue
    return None


def _iter_records(doc: dict, source: Path) -> Iterable[dict]:
    sn = doc.get("sn")
    proj = doc.get("Proj")
    ins = doc.get("Ins")
    time = _parse_time(doc.get("time", ""))
    base = {"sn": sn, "project": proj, "inspector": ins, "time": time, "source": str(source)}

    for entry in doc.get("result") or []:
        eid = entry.get("id")
        ename = entry.get("Name")
        overall = entry.get("result")
        data = entry.get("data") or []

        if eid in TILT_IDS:
            axis = TILT_IDS[eid]
            for d in data:
                yield {
                    **base,
                    "kind": "TILT",
                    "axis": axis,
                    "entry_id": eid,
                    "entry_name": ename,
                    "entry_result": overall,
                    "item": d.get("item"),
                    "angle": _angle_from_item(d.get("item", "")),
                    "value": d.get("value"),
                    "gyro": d.get("gyro"),
                    "vl": d.get("vl"),
                    "min": d.get("min"),
                    "max": d.get("max"),
                    "result": d.get("result"),
                }
        elif eid in VIBE_IDS:
            axis = VIBE_IDS[eid]
            for d in data:
                yield {
                    **base,
                    "kind": "VIBE",
                    "axis": axis,
                    "entry_id": eid,
                    "entry_name": ename,
                    "entry_result": overall,
                    "item": d.get("item"),
                    "angle": None,
                    "value": d.get("value"),
                    "gyro": None,
                    "vl": None,
                    "min": d.get("min"),
                    "max": d.get("max"),
                    "result": d.get("result"),
                }


def load_results(result_dir: Path, days: int | None = None) -> pd.DataFrame:
    """result_dir 아래의 모든 *.json을 재귀 스캔해서 평면화된 DataFrame 반환."""
    rows: list[dict] = []
    files = sorted(Path(result_dir).rglob("*.json"))
    for f in files:
        try:
            doc = json.loads(f.read_text(encoding="utf-8"))
        except (json.JSONDecodeError, UnicodeDecodeError):
            continue
        rows.extend(_iter_records(doc, f))

    df = pd.DataFrame(rows)
    if df.empty:
        return df

    df["time"] = pd.to_datetime(df["time"], utc=True)
    for col in ("value", "gyro", "vl", "min", "max", "angle"):
        df[col] = pd.to_numeric(df[col], errors="coerce")

    if days:
        cutoff = pd.Timestamp.now(tz="UTC") - pd.Timedelta(days=days)
        df = df[df["time"] >= cutoff]

    return df.reset_index(drop=True)


def serial_pass_fail(df: pd.DataFrame) -> pd.DataFrame:
    """시리얼별 전체 PASS/FAIL (TILT/VIBE 중 하나라도 FAIL이면 FAIL)."""
    if df.empty:
        return pd.DataFrame(columns=["sn", "project", "time", "overall"])
    g = df.groupby(["sn", "project"], dropna=False)
    overall = g["result"].apply(lambda s: "FAIL" if (s != "OK").any() else "PASS")
    t = g["time"].min()
    return pd.concat([t, overall.rename("overall")], axis=1).reset_index()
