"""
sqlite_to_access.py

Copy data from a SQLite database into an Access DATA MDB, replacing all tables
listed in the CopyTables table of the Access database.

Usage:
    python sqlite_to_access.py --sqlite <path> --access <path>

Phase 1 (DAO) runs as a subprocess so the OS fully releases all file handles
before Phase 2 (pyodbc) opens the same MDB.

Dependencies:
    pywin32 (win32com, pythoncom)  -- install with: pip install pywin32
    pyodbc                          -- install with: pip install pyodbc
    sqlite3                         -- Python stdlib
    argparse                        -- Python stdlib
"""

import argparse
import json
import os
import shutil
import sqlite3
import subprocess
import sys
import tempfile
import time

import pyodbc
import pythoncom
import win32com.client

BATCH_SIZE = 20000
_LOG = None


def log(msg: str):
    print(msg, flush=True)
    if _LOG:
        _LOG.write(msg + "\n")
        _LOG.flush()


def parse_args():
    parser = argparse.ArgumentParser(
        description="Copy SQLite data into an Access MDB (DATA database)."
    )
    parser.add_argument("--sqlite", required=True, metavar="PATH")
    parser.add_argument("--access", required=True, metavar="PATH")
    return parser.parse_args()


def restore_relations_via_dao(access_path: str, relations: list[dict]):
    """Re-create Relations in Access using a fresh DAO connection."""
    if not relations:
        log("  Restoring 0 relation(s)...")
        return
    log(f"  Restoring {len(relations)} relation(s)...")
    engine = win32com.client.Dispatch("DAO.DBEngine.120")
    workspace = engine.Workspaces(0)
    db = workspace.OpenDatabase(access_path)
    try:
        for rel_def in relations:
            try:
                rel = db.CreateRelation(
                    rel_def["name"],
                    rel_def["table"],
                    rel_def["foreign_table"],
                    rel_def["attributes"],
                )
                for fname, ffname in rel_def["fields"]:
                    fld = rel.CreateField(fname)
                    fld.ForeignName = ffname
                    rel.Fields.Append(fld)
                db.Relations.Append(rel)
            except Exception as exc:
                log(f"  Warning: could not restore relation '{rel_def['name']}': {exc}")
    finally:
        db.Close()
        workspace.Close()
        del db, workspace, engine
        pythoncom.CoUninitialize()


def copy_table_via_pyodbc(
    access_conn: pyodbc.Connection,
    sqlite_cur: sqlite3.Cursor,
    table_name: str,
    common_columns: list[str],
    null_to_empty_idx: list[int],
) -> int:
    col_list_sql = ", ".join(f"[{c}]" for c in common_columns)
    sqlite_cur.execute(f"SELECT {col_list_sql} FROM [{table_name}]")

    col_list_insert = ", ".join(f"[{c}]" for c in common_columns)
    placeholders = ", ".join(["?" for _ in common_columns])
    insert_sql = f"INSERT INTO [{table_name}] ({col_list_insert}) VALUES ({placeholders})"

    row_count = 0
    access_cur = access_conn.cursor()
    while True:
        batch = sqlite_cur.fetchmany(BATCH_SIZE)
        if not batch:
            break
        if null_to_empty_idx:
            coerced = []
            for row in batch:
                row = list(row)
                for idx in null_to_empty_idx:
                    if row[idx] is None:
                        row[idx] = ""
                coerced.append(row)
            batch = coerced
        access_cur.executemany(insert_sql, batch)
        row_count += len(batch)
        if row_count % 50000 == 0:
            log(f"    ... {row_count} rows so far")
    access_conn.commit()
    return row_count


def main():
    global _LOG

    args = parse_args()

    script_dir = os.path.dirname(os.path.abspath(__file__))
    log_path = os.path.join(script_dir, "sqlite_to_access.log")
    _LOG = open(log_path, "w", encoding="utf-8")

    try:
        log(f"Source SQLite : {args.sqlite}")
        log(f"Target Access : {args.access}")
        log(f"Log file      : {log_path}")

        # Copy the MDB to a local temp directory outside OneDrive so that
        # OneDrive sync cannot hold the .ldb lock file during the sync.
        tmp_dir = tempfile.mkdtemp(prefix="cbdb_sync_")
        mdb_filename = os.path.basename(args.access)
        work_mdb = os.path.join(tmp_dir, mdb_filename)
        log(f"\nCopying MDB to temp location (avoiding OneDrive lock)...")
        log(f"  {args.access}")
        log(f"  -> {work_mdb}")
        shutil.copy2(args.access, work_mdb)
        log(f"  Copy done ({os.path.getsize(work_mdb) // (1024*1024)} MB)")

        # -----------------------------------------------------------------------
        # PHASE 1: run _dao_phase.py as a subprocess so the OS releases all
        # DAO/COM/Jet file handles before we open the MDB with pyodbc.
        # -----------------------------------------------------------------------
        dao_script = os.path.join(script_dir, "_dao_phase.py")
        with tempfile.NamedTemporaryFile(suffix=".json", delete=False, mode="w") as tmp:
            tmp_json = tmp.name

        log(f"\nPhase 1: launching DAO subprocess (on temp MDB)...")
        log(f"  Schema JSON : {tmp_json}")

        python_exe = sys.executable
        result = subprocess.run(
            [python_exe, dao_script, args.sqlite, work_mdb, tmp_json],
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
        )

        # Relay subprocess output to our log
        for line in result.stdout.splitlines():
            log(f"  {line}")
        if result.stderr:
            for line in result.stderr.splitlines():
                log(f"  [stderr] {line}")

        if result.returncode != 0:
            log(f"\nERROR: DAO subprocess exited with code {result.returncode}")
            sys.exit(1)

        log("  DAO subprocess finished successfully.")

        # Wait for the Jet engine to fully release the .ldb lock file on the temp copy
        ldb_path = os.path.splitext(work_mdb)[0] + ".ldb"
        for _i in range(30):
            if not os.path.exists(ldb_path):
                break
            time.sleep(0.5)
        else:
            log(f"  Warning: .ldb still present after 15 s; will try to continue anyway.")
        time.sleep(1)

        # Load schema info written by the DAO subprocess
        with open(tmp_json, encoding="utf-8") as f:
            phase1 = json.load(f)
        os.unlink(tmp_json)

        ordered_tables: list[str] = phase1["ordered_tables"]
        relations: list[dict] = phase1["relations"]
        schema_info: dict = phase1["schema_info"]

        # -----------------------------------------------------------------------
        # PHASE 2: pyodbc bulk-insert
        # -----------------------------------------------------------------------
        log("\nPhase 2: bulk-inserting rows (pyodbc on temp MDB)...")
        conn_str = (
            f"DRIVER={{Microsoft Access Driver (*.mdb, *.accdb)}};"
            f"DBQ={work_mdb};"
        )
        try:
            access_conn = pyodbc.connect(conn_str, autocommit=False)
        except Exception as exc:
            log(f"\nERROR: could not open Access via pyodbc: {exc}")
            sys.exit(1)

        sqlite_conn = sqlite3.connect(args.sqlite)
        sqlite_cur = sqlite_conn.cursor()

        stats: dict[str, int] = {}
        error_occurred = False
        current_table = "(none)"
        try:
            for table_name in ordered_tables:
                current_table = table_name
                info = schema_info[table_name]
                common_columns: list[str] = info["common_columns"]
                null_to_empty_idx: list[int] = info["null_to_empty_idx"]

                log(f"  {table_name}...")
                if not common_columns:
                    log(f"    0 rows (no common columns)")
                    stats[table_name] = 0
                    continue

                count = copy_table_via_pyodbc(
                    access_conn, sqlite_cur,
                    table_name, common_columns, null_to_empty_idx,
                )
                stats[table_name] = count
                log(f"    done: {count} rows")

        except Exception as exc:
            log(f"\nERROR in pyodbc phase ({current_table}): {exc}")
            import traceback
            log(traceback.format_exc())
            try:
                access_conn.rollback()
            except Exception:
                pass
            error_occurred = True
        finally:
            access_conn.close()
            sqlite_conn.close()

        # -----------------------------------------------------------------------
        # PHASE 3: Restore Relations (on temp MDB)
        # -----------------------------------------------------------------------
        log("\nPhase 3: restoring relations...")
        restore_relations_via_dao(work_mdb, relations)

        if error_occurred:
            sys.exit(1)

        log("\nDone. Row counts:")
        total = 0
        for tbl, cnt in stats.items():
            log(f"  {tbl}: {cnt}")
            total += cnt
        log(f"\n  Total rows inserted: {total}")

        # -----------------------------------------------------------------------
        # PHASE 4: Copy temp MDB back to the original (OneDrive) location
        # -----------------------------------------------------------------------
        log(f"\nPhase 4: copying result back to original location...")
        log(f"  {work_mdb}")
        log(f"  -> {args.access}")
        shutil.copy2(work_mdb, args.access)
        log(f"  Copy done ({os.path.getsize(args.access) // (1024*1024)} MB)")
        shutil.rmtree(tmp_dir, ignore_errors=True)
        log("  Temp directory cleaned up.")

    finally:
        if _LOG:
            _LOG.close()


if __name__ == "__main__":
    main()
