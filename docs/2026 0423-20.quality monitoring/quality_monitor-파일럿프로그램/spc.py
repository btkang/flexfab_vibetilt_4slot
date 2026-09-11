"""SPC(통계적 공정 관리) 계산: 관리한계, Western Electric Rules, Cpk."""
from __future__ import annotations

import pandas as pd

MIN_SAMPLES_FOR_LIMITS = 20


def compute_limits(values: pd.Series, k: float = 3.0) -> dict:
    """PASS 값들의 μ±kσ 관리한계 계산."""
    clean = values.dropna()
    if len(clean) < MIN_SAMPLES_FOR_LIMITS:
        return {"n": len(clean), "mean": None, "std": None, "ucl": None, "lcl": None}
    mean = float(clean.mean())
    std = float(clean.std(ddof=1))
    return {
        "n": len(clean),
        "mean": mean,
        "std": std,
        "ucl": mean + k * std,
        "lcl": mean - k * std,
    }


def detect_western_electric(series: pd.Series, mean: float, std: float) -> pd.DataFrame:
    """Western Electric Rules 간이 구현.

    Rule 1: 1점이 ±3σ 밖
    Rule 2: 연속 2점이 한쪽 ±2σ 밖
    Rule 3: 연속 7점이 한쪽 편
    """
    if series.empty or std is None or std == 0:
        return pd.DataFrame(columns=["index", "rule", "value"])

    z = (series - mean) / std
    events = []

    for i, v in enumerate(z):
        if abs(v) > 3:
            events.append({"index": series.index[i], "rule": "R1 (±3σ 밖)", "value": series.iloc[i]})

    for i in range(1, len(z)):
        pair = z.iloc[i - 1:i + 1]
        if (pair > 2).all() or (pair < -2).all():
            events.append({"index": series.index[i], "rule": "R2 (연속2점 ±2σ 밖)", "value": series.iloc[i]})

    for i in range(6, len(z)):
        window = z.iloc[i - 6:i + 1]
        if (window > 0).all() or (window < 0).all():
            events.append({"index": series.index[i], "rule": "R3 (연속7점 한쪽)", "value": series.iloc[i]})

    return pd.DataFrame(events).drop_duplicates(subset=["index", "rule"]).reset_index(drop=True)


def cpk(values: pd.Series, lsl: float, usl: float) -> float | None:
    clean = values.dropna()
    if len(clean) < MIN_SAMPLES_FOR_LIMITS:
        return None
    mean = float(clean.mean())
    std = float(clean.std(ddof=1))
    if std == 0:
        return None
    return min((usl - mean) / (3 * std), (mean - lsl) / (3 * std))
