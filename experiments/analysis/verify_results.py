"""Kiem tra lai so request thanh cong va error rate tu CSV thuc te.
Dung: python experiments/analysis/verify_results.py experiments/raw-data
Chi ho tro file CSV tung request co cot success/status/result.
"""
import csv
import sys
from pathlib import Path

SUCCESS_NAMES = ("success", "is_success", "issuccess", "status", "result", "ok")
GOOD = {"true", "1", "success", "passed", "pass", "ok"}
BAD = {"false", "0", "failed", "fail", "error", "timeout"}


def check_file(path):
    with path.open("r", encoding="utf-8-sig", newline="") as f:
        rows = csv.DictReader(f)
        if not rows.fieldnames:
            return None
        cols = {x.strip().lower(): x for x in rows.fieldnames}
        name = next((cols[x] for x in SUCCESS_NAMES if x in cols), None)
        if not name:
            return None
        total = 0
        success = 0
        for row in rows:
            flag = (row.get(name) or "").strip().lower()
            if flag not in GOOD and flag not in BAD:
                raise ValueError(f"{path.name}: gia tri '{flag}' khong hop le o cot {name}")
            total += 1
            if flag in GOOD:
                success += 1
        if total == 0:
            return None
        error = (total - success) * 100 / total
        return total, success, error


def main():
    folder = Path(sys.argv[1] if len(sys.argv) > 1 else "experiments/raw-data")
    if not folder.is_dir():
        print("Khong tim thay folder:", folder)
        return 1
    files = sorted(folder.rglob("*.csv"))
    verified = 0
    for file in files:
        try:
            result = check_file(file)
        except (ValueError, UnicodeError) as e:
            print("LOI:", e)
            return 1
        if result is None:
            print("BO QUA (chua dung cot CSV tung request):", file.name)
            continue
        total, success, error = result
        print(f"{file.name}: tong={total}, thanh_cong={success}, error_rate={error:.2f}%")
        verified += 1
    if verified < 3:
        print(f"Chi kiem tra duoc {verified} file, can it nhat 3 luot co CSV thuc te.")
        print("Neu ten cot CSV cua nhom khac, sua SUCCESS_NAMES trong script.")
        return 1
    print("Da kiem tra it nhat 3 luot. Doi chieu voi bang tong hop cua nhom.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
