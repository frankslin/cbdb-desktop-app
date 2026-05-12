"""
_dao_phase.py — internal helper invoked as a subprocess by sqlite_to_access.py.

Performs the DAO phase:
  1. Read CopyTables from Access
  2. Topological sort
  3. Export and delete Relations
  4. Inspect column schemas
  5. DELETE all rows from each table
  6. Write results to a JSON file

Usage (internal):
    python _dao_phase.py <sqlite_path> <access_path> <output_json_path>
"""

import json
import sqlite3
import sys

import pythoncom
import win32com.client

DB_OPEN_DYNASET = 2
DB_FAIL_ON_ERROR = 128


def main():
    if len(sys.argv) != 4:
        print("Usage: _dao_phase.py <sqlite> <access> <output_json>", file=sys.stderr)
        sys.exit(2)

    sqlite_path, access_path, out_json = sys.argv[1], sys.argv[2], sys.argv[3]

    print(f"[dao] Opening Access via DAO: {access_path}", flush=True)
    engine = win32com.client.Dispatch("DAO.DBEngine.120")
    workspace = engine.Workspaces(0)
    db = workspace.OpenDatabase(access_path)

    sqlite_conn = sqlite3.connect(sqlite_path)
    sqlite_cur = sqlite_conn.cursor()

    try:
        # 1. Read CopyTables
        rs = db.OpenRecordset("SELECT TableName FROM CopyTables", DB_OPEN_DYNASET)
        tables = []
        while not rs.EOF:
            tables.append(rs.Fields("TableName").Value)
            rs.MoveNext()
        rs.Close()
        print(f"[dao] Tables to copy: {len(tables)}", flush=True)

        # 2. Topological sort via FK relations
        from collections import deque
        table_set = set(tables)
        in_degree = {t: 0 for t in tables}
        children: dict[str, list[str]] = {t: [] for t in tables}
        relations_raw = []
        for rel in db.Relations:
            if rel.Name.startswith("MSys"):
                continue
            fields = [(f.Name, f.ForeignName) for f in rel.Fields]
            relations_raw.append({
                "name": rel.Name,
                "table": rel.Table,
                "foreign_table": rel.ForeignTable,
                "attributes": rel.Attributes,
                "fields": fields,
            })
            parent, child = rel.Table, rel.ForeignTable
            if parent in table_set and child in table_set and child != parent:
                if child not in children[parent]:
                    children[parent].append(child)
                    in_degree[child] += 1

        queue = deque(t for t in tables if in_degree[t] == 0)
        ordered = []
        while queue:
            node = queue.popleft()
            ordered.append(node)
            for c in children[node]:
                in_degree[c] -= 1
                if in_degree[c] == 0:
                    queue.append(c)
        remaining = [t for t in tables if t not in set(ordered)]
        if remaining:
            print(f"[dao] Warning: cycle; appending {len(remaining)} tables: {remaining}", flush=True)
        ordered.extend(remaining)
        print(f"[dao] Copy order: {ordered}", flush=True)

        # 3. Delete Relations
        print(f"[dao] Removing {len(relations_raw)} relation(s)...", flush=True)
        for rel in reversed(relations_raw):
            try:
                db.Relations.Delete(rel["name"])
            except Exception as exc:
                print(f"[dao] Warning: could not delete relation '{rel['name']}': {exc}", flush=True)

        # 4. Inspect schemas + 5. DELETE rows
        print("[dao] Inspecting schemas and clearing tables...", flush=True)
        schema_info = {}
        sqlite_counts = {}
        for table_name in ordered:
            # SQLite columns
            sqlite_cur.execute(f"SELECT * FROM [{table_name}] LIMIT 0")
            sqlite_columns = [d[0] for d in sqlite_cur.description]

            # Access columns + null_to_empty set
            access_rs = db.OpenRecordset(table_name, DB_OPEN_DYNASET)
            access_fields: set[str] = set()
            null_to_empty: set[str] = set()
            for i in range(access_rs.Fields.Count):
                f = access_rs.Fields(i)
                access_fields.add(f.Name)
                if f.Required and f.AllowZeroLength and f.Type == 10:
                    null_to_empty.add(f.Name)
            access_rs.Close()

            common_columns = [c for c in sqlite_columns if c in access_fields]
            skipped = [c for c in sqlite_columns if c not in access_fields]
            null_to_empty_idx = [i for i, c in enumerate(common_columns) if c in null_to_empty]

            if skipped:
                print(f"[dao] {table_name}: skip {len(skipped)} cols: {skipped}", flush=True)

            # SQLite row count
            sqlite_cur.execute(f"SELECT COUNT(*) FROM [{table_name}]")
            sqlite_counts[table_name] = sqlite_cur.fetchone()[0]

            # Clear Access table
            db.Execute(f"DELETE * FROM [{table_name}]", DB_FAIL_ON_ERROR)

            schema_info[table_name] = {
                "common_columns": common_columns,
                "skipped": skipped,
                "null_to_empty_idx": null_to_empty_idx,
            }

        print("[dao] All tables cleared. Writing results...", flush=True)

        result = {
            "ordered_tables": ordered,
            "relations": relations_raw,
            "schema_info": schema_info,
            "sqlite_counts": sqlite_counts,
        }
        with open(out_json, "w", encoding="utf-8") as f:
            json.dump(result, f, ensure_ascii=False, indent=2)
        print(f"[dao] Written: {out_json}", flush=True)

    except Exception as exc:
        import traceback
        print(f"[dao] ERROR: {exc}", file=sys.stderr, flush=True)
        traceback.print_exc()
        sys.exit(1)
    finally:
        sqlite_conn.close()
        try:
            db.Close()
            workspace.Close()
        except Exception:
            pass
        del db, workspace, engine
        pythoncom.CoUninitialize()

    print("[dao] Phase 1 done. Exiting.", flush=True)
    sys.exit(0)


if __name__ == "__main__":
    main()
