#!/usr/bin/env python3
"""
Repair date-only fields that were stored with a time component.

Background
----------
Datepicker values used to be sent to the API as raw JS ``Date`` objects, which
``JSON.stringify`` serializes via ``toISOString()`` (UTC). For a user in a
positive-offset timezone, local midnight became the *previous* day in the
evening (e.g. UTC+2: 2000-01-01 00:00 -> 1999-12-31 22:00), so the stored date
was a day behind what was picked.

These columns are meant to hold a *date*, so a correct row always has the time
00:00:00. Any other time means the value was shifted.

Repair rules (in order of confidence)
-------------------------------------
1. **History** (preferred). Every drifting save recorded a ParticipantHistories
   row, because the change is detected on the formatted dd.MM.yyyy value. The
   *earliest* OldValue for a column is therefore the date as it was before any
   drift - the original. This is the only method that recovers records which
   drifted more than once (e.g. 03.06 -> 02.06 -> 01.06).

2. **Nearest midnight** (fallback, when no history exists). The exact inverse of
   a single shift for any timezone offset:

       22:00 -> next day (+2h)      # UTC+2
       21:00 -> next day (+3h)      # UTC+3
       05:00 -> same day (truncate) # negative offsets shift forward instead

   A value at exactly 12:00:00 is equidistant and therefore ambiguous; those are
   reported and skipped unless you pick a direction with --ties.

Safety
------
* Dry-run by default - nothing is written unless you pass --apply.
* --apply takes a timestamped backup (SQLite online backup) before writing.
* Close the app first, otherwise SQLite may refuse to write (database locked).

Usage
-----
    python scripts/fix_shifted_dates.py                  # dry run, default DB
    python scripts/fix_shifted_dates.py --apply          # fix + backup
    python scripts/fix_shifted_dates.py --db path.db     # explicit database
    python scripts/fix_shifted_dates.py --apply --ties forward
"""

from __future__ import annotations

import argparse
import os
import sqlite3
import sys
from datetime import datetime, timedelta

TABLE = "Participants"
HISTORY_TABLE = "ParticipantHistories"
KEY_COLUMN = "ParticipantKey"
NAME_COLUMN = "FullName"
DATE_COLUMNS = ("BirthDate", "ProbeOpenDate", "ApprovalDate")
STORAGE_FORMAT = "%Y-%m-%d %H:%M:%S"
HISTORY_FORMAT = "%d.%m.%Y"

# Cyrillic names must survive the console encoding.
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")

DEFAULT_DB = os.path.join(
    os.environ.get("LOCALAPPDATA", ""),
    "ProjectK3PDB", "data", "projectk_3pdb_standalone_v2.db",
)


def parse_stored(value) -> datetime | None:
    """Parse the datetime text EF Core writes into SQLite."""
    if value is None:
        return None

    text = str(value).strip().replace("T", " ").rstrip("Z")
    if not text:
        return None

    text = text.split(".")[0]  # drop fractional seconds
    for fmt in (STORAGE_FORMAT, "%Y-%m-%d %H:%M", "%Y-%m-%d"):
        try:
            return datetime.strptime(text, fmt)
        except ValueError:
            continue
    return None


def nearest_midnight(moment: datetime) -> tuple[datetime, float, bool]:
    """Round to the nearest midnight -> (corrected, hours_moved, is_tie)."""
    same_day = moment.replace(hour=0, minute=0, second=0, microsecond=0)
    next_day = same_day + timedelta(days=1)

    back = (moment - same_day).total_seconds()
    forward = (next_day - moment).total_seconds()

    if back == forward:
        return next_day, forward / 3600, True
    if back < forward:
        return same_day, back / 3600, False
    return next_day, forward / 3600, False


def original_from_history(connection: sqlite3.Connection, columns: list[str]) -> dict[tuple[str, str], datetime]:
    """
    Earliest recorded OldValue per (participant, column) - the value before any drift.

    Soft-deleted history rows are included on purpose: they are still evidence of
    what the date used to be.
    """
    placeholders = ", ".join("?" for _ in columns)
    rows = connection.execute(
        f'SELECT "{KEY_COLUMN}", "PropertyName", "OldValue", "ChangedAt" '
        f'FROM "{HISTORY_TABLE}" WHERE "PropertyName" IN ({placeholders}) '
        f'ORDER BY "ChangedAt" ASC',
        columns,
    ).fetchall()

    earliest: dict[tuple[str, str], datetime] = {}
    for row in rows:
        key = (row[KEY_COLUMN], row["PropertyName"])
        if key in earliest:
            continue  # rows are ordered, so the first one wins
        raw = (row["OldValue"] or "").strip()
        if not raw:
            continue
        try:
            earliest[key] = datetime.strptime(raw, HISTORY_FORMAT)
        except ValueError:
            continue
    return earliest


def existing_columns(connection: sqlite3.Connection) -> set[str]:
    rows = connection.execute(f'PRAGMA table_info("{TABLE}")').fetchall()
    if not rows:
        sys.exit(f"Table {TABLE!r} not found - is this the right database?")
    return {row[1] for row in rows}


def backup_database(path: str) -> str:
    stamp = datetime.now().strftime("%Y-%m-%d_%H-%M-%S")
    target = f"{os.path.splitext(path)[0]}.pre-datefix_{stamp}.db"

    source = sqlite3.connect(path)
    try:
        destination = sqlite3.connect(target)
        try:
            source.backup(destination)
        finally:
            destination.close()
    finally:
        source.close()

    return target


def main() -> int:
    parser = argparse.ArgumentParser(description="Fix dates shifted by the UTC serialization bug.")
    parser.add_argument("--db", default=DEFAULT_DB, help="path to the SQLite database")
    parser.add_argument("--apply", action="store_true", help="write the fixes (default is a dry run)")
    parser.add_argument(
        "--ties",
        choices=("skip", "forward", "back"),
        default="skip",
        help="what to do with ambiguous 12:00:00 values (default: skip)",
    )
    args = parser.parse_args()

    if not os.path.isfile(args.db):
        sys.exit(f"Database not found: {args.db}")

    print(f"Database: {args.db}")
    print(f"Mode:     {'APPLY (will write)' if args.apply else 'DRY RUN (no changes)'}\n")

    connection = sqlite3.connect(args.db)
    connection.row_factory = sqlite3.Row

    columns = existing_columns(connection)
    target_columns = [c for c in DATE_COLUMNS if c in columns]
    for missing in (c for c in DATE_COLUMNS if c not in columns):
        print(f"  note: column {missing} does not exist in this database - skipped")
    if not target_columns:
        sys.exit("None of the expected date columns exist.")

    selected = ", ".join(f'"{c}"' for c in [KEY_COLUMN, NAME_COLUMN, *target_columns])
    rows = connection.execute(f"SELECT {selected} FROM \"{TABLE}\"").fetchall()
    history = original_from_history(connection, target_columns)

    fixes: list[tuple[str, str, str, datetime, datetime, str]] = []  # column, key, name, old, new, method
    ties: list[tuple[str, str, datetime]] = []
    unparsed: list[tuple[str, str]] = []

    for row in rows:
        for column in target_columns:
            raw = row[column]
            if raw is None:
                continue

            moment = parse_stored(raw)
            if moment is None:
                unparsed.append((column, str(raw)))
                continue

            if moment.time() == datetime.min.time():
                continue  # already a clean date

            recovered = history.get((row[KEY_COLUMN], column))
            if recovered is not None:
                fixes.append((column, row[KEY_COLUMN], row[NAME_COLUMN], moment, recovered, "history"))
                continue

            corrected, _, is_tie = nearest_midnight(moment)

            if is_tie:
                if args.ties == "skip":
                    ties.append((column, row[NAME_COLUMN], moment))
                    continue
                corrected = (
                    moment.replace(hour=0, minute=0, second=0) + timedelta(days=1)
                    if args.ties == "forward"
                    else moment.replace(hour=0, minute=0, second=0)
                )

            fixes.append((column, row[KEY_COLUMN], row[NAME_COLUMN], moment, corrected, "rounded"))

    if unparsed:
        print(f"  warning: {len(unparsed)} value(s) could not be parsed and were left alone\n")

    if not fixes and not ties:
        print("No shifted dates found - nothing to do.")
        connection.close()
        return 0

    if fixes:
        print(f"Shifted dates found: {len(fixes)}\n")
        width = max(len(c) for c, *_ in fixes)
        for column, _key, name, old, new, method in fixes:
            drift = (new.date() - old.date()).days
            print(
                f"  {column:<{width}}  {old.strftime('%Y-%m-%d %H:%M:%S')}"
                f"  ->  {new.strftime('%Y-%m-%d')}  ({method}, +{drift}d)   {name}"
            )
        print()

    if ties:
        print(f"Ambiguous (exactly 12:00:00, skipped): {len(ties)}")
        for column, name, moment in ties:
            print(f"  {column}  {moment.strftime('%Y-%m-%d %H:%M:%S')}   {name}")
        print("  Re-run with --ties forward (next day) or --ties back (same day) to resolve.\n")

    per_column: dict[str, int] = {}
    per_method: dict[str, int] = {}
    for column, _key, _name, _old, _new, method in fixes:
        per_column[column] = per_column.get(column, 0) + 1
        per_method[method] = per_method.get(method, 0) + 1

    def summary() -> str:
        columns_part = ", ".join(f"{c}={n}" for c, n in sorted(per_column.items()))
        methods_part = ", ".join(f"{m}={n}" for m, n in sorted(per_method.items()))
        return f"{columns_part} [{methods_part}]"

    if not args.apply:
        print("DRY RUN - no changes written.")
        print(f"Would affect {len(fixes)} record(s): {summary()}")
        print("Re-run with --apply to write the fixes.")
        connection.close()
        return 0

    backup_path = backup_database(args.db)
    print(f"Backup created: {backup_path}")

    try:
        with connection:
            for column, key, _name, _old, new, _method in fixes:
                connection.execute(
                    f'UPDATE "{TABLE}" SET "{column}" = ? WHERE "{KEY_COLUMN}" = ?',
                    (new.strftime(STORAGE_FORMAT), key),
                )
    except sqlite3.OperationalError as error:
        connection.close()
        sys.exit(f"Write failed ({error}). Close the app and try again; "
                 f"your data is untouched, backup at {backup_path}")

    connection.close()

    print(f"\nDone. Affected {len(fixes)} record(s): {summary()}")
    if ties:
        print(f"{len(ties)} ambiguous value(s) left unchanged.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
