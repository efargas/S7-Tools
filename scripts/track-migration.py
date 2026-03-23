#!/usr/bin/env python3
"""
Migration Tracker for S7Tools Documentation

Tracks file migrations from old structure to new consolidated structure.

Usage:
    # Record migration
    python scripts/track-migration.py add --old=<path> --new=<path> [--stub]

    # Generate migration report
    python scripts/track-migration.py report [--output=<file>]

    # Check migration status
    python scripts/track-migration.py status

Requirements:
    Python 3.7+ (no external dependencies)
"""

import sys
import os
import json
from pathlib import Path
from datetime import datetime, timedelta
from dataclasses import dataclass, asdict
from typing import List


@dataclass
class Migration:
    """Represents a file migration"""
    oldPath: str
    newPath: str
    migrationDate: str
    redirectStubCreated: bool
    stubRemovalDate: str
    gitHistoryPreserved: bool


class MigrationTracker:
    """Tracks documentation migrations"""

    LOG_FILE = "docs/.metadata/migration-log.json"

    def __init__(self, repo_root: str = "."):
        self.repo_root = Path(repo_root)
        self.log_path = self.repo_root / self.LOG_FILE
        self.log_data = self.load_log()

    def load_log(self) -> dict:
        """Load migration log from JSON file"""
        if not self.log_path.exists():
            return {"version": "1.0.0", "migrations": []}

        try:
            with open(self.log_path, 'r') as f:
                return json.load(f)
        except Exception as e:
            print(f"Error loading migration log: {e}", file=sys.stderr)
            return {"version": "1.0.0", "migrations": []}

    def save_log(self):
        """Save migration log to JSON file"""
        try:
            self.log_path.parent.mkdir(parents=True, exist_ok=True)
            with open(self.log_path, 'w') as f:
                json.dump(self.log_data, f, indent=2)
        except Exception as e:
            print(f"Error saving migration log: {e}", file=sys.stderr)
            sys.exit(1)

    def add_migration(self, old_path: str, new_path: str, create_stub: bool = False):
        """Add a new migration record"""
        migration_date = datetime.now().strftime('%Y-%m-%d')
        stub_removal_date = (datetime.now() + timedelta(days=90)).strftime('%Y-%m-%d')

        migration = Migration(
            oldPath=old_path,
            newPath=new_path,
            migrationDate=migration_date,
            redirectStubCreated=create_stub,
            stubRemovalDate=stub_removal_date if create_stub else "",
            gitHistoryPreserved=True  # Assume git mv was used
        )

        self.log_data["migrations"].append(asdict(migration))
        self.save_log()

        print(f"✓ Migration recorded: {old_path} → {new_path}")
        if create_stub:
            print(f"  Stub removal date: {stub_removal_date}")

    def generate_report(self, output_file: str = None):
        """Generate migration report"""
        migrations = self.log_data.get("migrations", [])

        if not migrations:
            print("No migrations recorded.")
            return

        # Build report
        report_lines = [
            "# Documentation Migration Report",
            "",
            f"**Generated**: {datetime.now().strftime('%Y-%m-%d')}",
            "",
            "## Summary",
            f"- Files migrated: {len(migrations)}",
            f"- Stubs created: {sum(1 for m in migrations if m['redirectStubCreated'])}",
            f"- Stubs pending removal: {sum(1 for m in migrations if m['redirectStubCreated'] and m['stubRemovalDate'] >= datetime.now().strftime('%Y-%m-%d'))}",
            "",
            "## Migrations",
            ""
        ]

        for migration in migrations:
            report_lines.append(f"### {migration['oldPath']} → {migration['newPath']}")
            report_lines.append(f"- Migration date: {migration['migrationDate']}")
            report_lines.append(f"- Stub created: {'Yes' if migration['redirectStubCreated'] else 'No'}")
            if migration['redirectStubCreated']:
                report_lines.append(f"- Stub removal: {migration['stubRemovalDate']}")
            report_lines.append(f"- Git history preserved: {'Yes' if migration['gitHistoryPreserved'] else 'No'}")
            report_lines.append("")

        report = "\n".join(report_lines)

        if output_file:
            Path(output_file).write_text(report, encoding='utf-8')
            print(f"Report written to {output_file}")
        else:
            print(report)

    def show_status(self):
        """Show migration status"""
        migrations = self.log_data.get("migrations", [])

        print(f"Migration Status:")
        print(f"  Total migrations: {len(migrations)}")

        if not migrations:
            return

        # Count stubs
        stubs_created = sum(1 for m in migrations if m['redirectStubCreated'])
        today = datetime.now().strftime('%Y-%m-%d')
        stubs_pending = sum(1 for m in migrations if m['redirectStubCreated'] and m['stubRemovalDate'] >= today)
        stubs_ready = stubs_created - stubs_pending

        print(f"  Stubs created: {stubs_created}")
        print(f"  Stubs pending removal: {stubs_pending}")
        print(f"  Stubs ready for removal: {stubs_ready}")

        if stubs_ready > 0:
            print(f"\n✓ {stubs_ready} stub(s) can be removed (transition period complete)")


def main():
    if len(sys.argv) < 2:
        print("Usage:")
        print("  python scripts/track-migration.py add --old=<path> --new=<path> [--stub]")
        print("  python scripts/track-migration.py report [--output=<file>]")
        print("  python scripts/track-migration.py status")
        sys.exit(1)

    command = sys.argv[1]
    tracker = MigrationTracker()

    if command == "add":
        old_path = None
        new_path = None
        create_stub = False

        for arg in sys.argv[2:]:
            if arg.startswith("--old="):
                old_path = arg.split("=")[1]
            elif arg.startswith("--new="):
                new_path = arg.split("=")[1]
            elif arg == "--stub":
                create_stub = True

        if not old_path or not new_path:
            print("Error: --old and --new are required for 'add' command", file=sys.stderr)
            sys.exit(1)

        tracker.add_migration(old_path, new_path, create_stub)

    elif command == "report":
        output_file = None
        for arg in sys.argv[2:]:
            if arg.startswith("--output="):
                output_file = arg.split("=")[1]
        tracker.generate_report(output_file)

    elif command == "status":
        tracker.show_status()

    else:
        print(f"Error: Unknown command '{command}'", file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()
