#!/usr/bin/env python3
"""
Archive Inventory Generator

Generates a comprehensive inventory of archived documents including:
- Deprecation dates
- Removal dates (2-year retention)
- Superseding references
- Days until removal

Usage:
    python archive-inventory.py docs/
"""

import sys
import re
from pathlib import Path
from datetime import datetime, timedelta
from typing import Dict, List
import yaml
import json


class ArchiveInventory:
    """Generates inventory of archived documents"""

    def __init__(self, docs_root: Path):
        self.docs_root = Path(docs_root).resolve()
        self.archive_root = self.docs_root / 'archive'

    def generate(self) -> Dict:
        """Generate complete inventory"""
        inventory = {
            'generated': datetime.now().isoformat(),
            'archive_root': str(self.archive_root.relative_to(self.docs_root.parent)),
            'documents': [],
            'statistics': {
                'total_archived': 0,
                'pending_removal': 0,
                'days_until_next_removal': None
            }
        }

        if not self.archive_root.exists():
            return inventory

        archived_files = sorted(self.archive_root.rglob('*.md'))
        inventory['statistics']['total_archived'] = len(archived_files)

        next_removal_days = float('inf')

        for archived_file in archived_files:
            doc_info = self._extract_document_info(archived_file)
            if doc_info:
                inventory['documents'].append(doc_info)

                if doc_info['days_until_removal'] is not None:
                    if doc_info['days_until_removal'] > 0:
                        inventory['statistics']['pending_removal'] += 1
                        next_removal_days = min(next_removal_days, doc_info['days_until_removal'])

        if next_removal_days != float('inf'):
            inventory['statistics']['days_until_next_removal'] = next_removal_days

        return inventory

    def _extract_document_info(self, file_path: Path) -> Dict:
        """Extract information from archived document"""
        try:
            content = file_path.read_text(encoding='utf-8')

            frontmatter_match = re.match(r'^---\s*\n(.*?)\n---\s*\n', content, re.DOTALL)
            if not frontmatter_match:
                return None

            frontmatter = yaml.safe_load(frontmatter_match.group(1))

            # Calculate days until removal
            days_until_removal = None
            if 'removal-date' in frontmatter:
                removal_date = datetime.strptime(str(frontmatter['removal-date']), '%Y-%m-%d')
                days_until_removal = (removal_date - datetime.now()).days

            return {
                'file': str(file_path.relative_to(self.docs_root)),
                'title': frontmatter.get('title', file_path.name),
                'deprecated_date': str(frontmatter.get('deprecated-date', 'unknown')),
                'removal_date': str(frontmatter.get('removal-date', 'unknown')),
                'days_until_removal': days_until_removal,
                'superseded_by': frontmatter.get('superseded-by', None),
                'tags': frontmatter.get('tags', [])
            }

        except Exception as e:
            return {
                'file': str(file_path.relative_to(self.docs_root)),
                'error': str(e)
            }

    def print_inventory(self, inventory: Dict):
        """Print human-readable inventory"""
        print("=" * 80)
        print("ARCHIVE INVENTORY REPORT")
        print("=" * 80)
        print()

        stats = inventory['statistics']
        print(f"Generated: {inventory['generated']}")
        print(f"Archive Root: {inventory['archive_root']}")
        print()

        print("Statistics:")
        print(f"  Total Archived Documents: {stats['total_archived']}")
        print(f"  Pending Removal: {stats['pending_removal']}")
        if stats['days_until_next_removal'] is not None:
            print(f"  Next Removal In: {stats['days_until_next_removal']} days")
        print()

        if inventory['documents']:
            print("Archived Documents:")
            print("-" * 80)

            for doc in sorted(inventory['documents'], key=lambda x: x.get('days_until_removal') or 99999):
                print(f"\n📄 {doc['title']}")
                print(f"   File: {doc['file']}")
                print(f"   Deprecated: {doc['deprecated_date']}")
                print(f"   Removal: {doc['removal_date']}", end="")

                if doc.get('days_until_removal') is not None:
                    days = doc['days_until_removal']
                    if days > 365:
                        print(f" ({days} days / {days//365} years remaining)")
                    elif days > 0:
                        print(f" ({days} days remaining)")
                    else:
                        print(" (⚠️  OVERDUE FOR REMOVAL)")
                else:
                    print()

                if doc.get('superseded_by'):
                    print(f"   Superseded By: {doc['superseded_by']}")

        print()
        print("=" * 80)


def main():
    if len(sys.argv) < 2:
        print("Usage: python archive-inventory.py <docs-directory>")
        sys.exit(1)

    docs_root = Path(sys.argv[1])
    inventory_gen = ArchiveInventory(docs_root)

    inventory = inventory_gen.generate()
    inventory_gen.print_inventory(inventory)

    # Save JSON
    output_file = docs_root / '.metadata' / 'archive-inventory.json'
    output_file.parent.mkdir(parents=True, exist_ok=True)
    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(inventory, f, indent=2)

    print(f"\n✓ Inventory saved to {output_file.relative_to(docs_root.parent)}")


if __name__ == '__main__':
    main()
