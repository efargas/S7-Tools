#!/usr/bin/env python3
"""
Automated Document Archiver

Automatically moves deprecated documents to archive/ and creates redirect stubs.

Features:
- Validates deprecation frontmatter
- Calculates 2-year removal date
- Moves document to archive/
- Creates redirect stub in original location
- Updates cross-references

Usage:
    python auto-archive.py docs/path/to/deprecated-doc.md --reason "Superseded by new-doc.md"
    python auto-archive.py docs/path/to/deprecated-doc.md --superseded-by docs/new-doc.md --dry-run
"""

import sys
import re
import argparse
from pathlib import Path
from datetime import datetime, timedelta
from typing import Optional
import yaml
import shutil


class AutoArchiver:
    """Automatically archives deprecated documents"""

    REDIRECT_TEMPLATE = """---
title: "REDIRECT: {title}"
created: "{created}"
last-updated: "{last_updated}"
version: "{version}"
status: "redirect"
tags:
  - redirect
  - deprecated
deprecated-date: "{deprecated_date}"
superseded-by: "{superseded_by}"
removal-date: "{removal_date}"
---

# ⚠️ DOCUMENT MOVED

This document has been **deprecated** and moved to the archive.

**New Location**: [`{new_location}`]({new_location_link})

**Superseded By**: [`{superseding_title}`]({superseded_by})

**Reason**: {reason}

**Removal Date**: This redirect stub will be removed on **{removal_date}** (2-year retention).

---

## Migration Path

If you were using this document:

1. Read the new {superseding_type} - See [`{superseding_file}`]({superseded_by})
2. Check migration guide - See [`docs/guides/migration/deprecated-patterns.md`](guides/migration/deprecated-patterns.md)
3. Update references - Update any bookmarks or links to point to the new location

For questions or concerns, please file an issue in the repository.

## Related Documentation

---
*This section is auto-generated. Do not edit manually. Last updated: {last_updated}*
"""

    def __init__(self, docs_root: Path, dry_run: bool = False):
        self.docs_root = Path(docs_root).resolve()
        self.archive_root = self.docs_root / 'archive'
        self.dry_run = dry_run

    def archive_document(
        self,
        file_path: Path,
        superseded_by: str,
        reason: str = "Content consolidated into updated documentation"
    ) -> bool:
        """Archive a document and create redirect stub"""

        file_path = Path(file_path).resolve()

        # Validate file exists
        if not file_path.exists():
            print(f"❌ Error: File does not exist: {file_path}")
            return False

        # Read current document
        try:
            content = file_path.read_text(encoding='utf-8')
        except Exception as e:
            print(f"❌ Error reading file: {e}")
            return False

        # Extract frontmatter
        frontmatter_match = re.match(r'^---\s*\n(.*?)\n---\s*\n', content, re.DOTALL)
        if frontmatter_match:
            try:
                frontmatter = yaml.safe_load(frontmatter_match.group(1))
            except yaml.YAMLError as e:
                print(f"❌ Error parsing frontmatter: {e}")
                return False
        else:
            frontmatter = {}

        # Calculate dates
        today = datetime.now().strftime('%Y-%m-%d')
        removal_date = (datetime.now() + timedelta(days=730)).strftime('%Y-%m-%d')

        # Determine archive location
        relative_path = file_path.relative_to(self.docs_root)
        archive_path = self.archive_root / relative_path

        # Update frontmatter for archived version
        frontmatter['status'] = 'deprecated'
        frontmatter['deprecated-date'] = today
        frontmatter['superseded-by'] = superseded_by
        frontmatter['removal-date'] = removal_date
        frontmatter['last-updated'] = today

        if 'tags' in frontmatter:
            if 'deprecated' not in frontmatter['tags']:
                frontmatter['tags'].append('deprecated')
        else:
            frontmatter['tags'] = ['deprecated']

        # Create archived version
        archived_content = self._build_archived_content(frontmatter, content, reason, superseded_by)

        # Create redirect stub
        redirect_stub = self._build_redirect_stub(
            frontmatter, relative_path, archive_path, superseded_by, reason, today, removal_date
        )

        if self.dry_run:
            print("\n" + "=" * 80)
            print("DRY RUN - No files will be modified")
            print("=" * 80)
            print(f"\nWould move: {relative_path}")
            print(f"        to: {archive_path.relative_to(self.docs_root.parent)}")
            print(f"\nWould create redirect stub at: {relative_path}")
            print(f"\nArchived content preview (first 500 chars):")
            print("-" * 80)
            print(archived_content[:500])
            print("\n...")
            print("-" * 80)
            print(f"\nRedirect stub preview (first 500 chars):")
            print("-" * 80)
            print(redirect_stub[:500])
            print("\n...")
            print("-" * 80)
            return True

        # Execute archiving
        try:
            # Create archive directory
            archive_path.parent.mkdir(parents=True, exist_ok=True)

            # Write archived version
            with open(archive_path, 'w', encoding='utf-8') as f:
                f.write(archived_content)

            print(f"✓ Moved to archive: {archive_path.relative_to(self.docs_root.parent)}")

            # Create redirect stub
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(redirect_stub)

            print(f"✓ Created redirect stub: {relative_path}")
            print(f"✓ Removal date set to: {removal_date} (2-year retention)")

            return True

        except Exception as e:
            print(f"❌ Error during archiving: {e}")
            return False

    def _build_archived_content(
        self, frontmatter: dict, original_content: str, reason: str, superseded_by: str
    ) -> str:
        """Build archived document content"""

        # Update title to indicate deprecation
        if 'title' in frontmatter and '(Deprecated)' not in frontmatter['title']:
            frontmatter['title'] = f"{frontmatter['title']} (Deprecated)"

        # Rebuild content with updated frontmatter
        frontmatter_yaml = yaml.dump(frontmatter, default_flow_style=False, sort_keys=False)

        # Extract body (remove old frontmatter)
        body_match = re.match(r'^---\s*\n.*?\n---\s*\n(.*)', original_content, re.DOTALL)
        body = body_match.group(1) if body_match else original_content

        # Add deprecation notice at top
        deprecation_notice = f"""
# ⚠️ DEPRECATED: {frontmatter.get('title', 'Document')}

**This document is deprecated as of {frontmatter['deprecated-date']}.**

**Use instead**: [{superseded_by}]({superseded_by})

**Reason**: {reason}

---

*Original content preserved below for historical reference*

---

"""

        return f"---\n{frontmatter_yaml}---\n{deprecation_notice}{body}"

    def _build_redirect_stub(
        self,
        frontmatter: dict,
        original_path: Path,
        archive_path: Path,
        superseded_by: str,
        reason: str,
        today: str,
        removal_date: str
    ) -> str:
        """Build redirect stub content"""

        new_location = f"archive/{original_path}"
        new_location_link = f"archive/{original_path.name}"

        # Determine superseding file type
        superseding_type = "documentation"
        if superseded_by.endswith('.md'):
            superseding_type = "document"

        return self.REDIRECT_TEMPLATE.format(
            title=frontmatter.get('title', original_path.stem).replace(' (Deprecated)', ''),
            created=frontmatter.get('created', today),
            last_updated=today,
            version=frontmatter.get('version', '1.0.0'),
            deprecated_date=today,
            superseded_by=superseded_by,
            removal_date=removal_date,
            new_location=new_location,
            new_location_link=new_location_link,
            superseding_title=superseded_by.split('/')[-1].replace('.md', '').replace('-', ' ').title(),
            reason=reason,
            superseding_type=superseding_type,
            superseding_file=superseded_by.split('/')[-1]
        )


def main():
    parser = argparse.ArgumentParser(description='Automatically archive deprecated documents')
    parser.add_argument('file', help='Path to document to archive')
    parser.add_argument('--superseded-by', required=True, help='Path to superseding document (relative to docs/)')
    parser.add_argument('--reason', default='Content consolidated into updated documentation',
                        help='Reason for deprecation')
    parser.add_argument('--dry-run', action='store_true', help='Show what would be done without making changes')

    args = parser.parse_args()

    file_path = Path(args.file)

    # Determine docs root (assume file is under docs/)
    if 'docs' in file_path.parts:
        docs_index = file_path.parts.index('docs')
        docs_root = Path(*file_path.parts[:docs_index + 1])
    else:
        print("❌ Error: File must be under docs/ directory")
        sys.exit(1)

    archiver = AutoArchiver(docs_root, dry_run=args.dry_run)

    success = archiver.archive_document(file_path, args.superseded_by, args.reason)

    if success:
        if args.dry_run:
            print("\n✓ Dry run complete - use without --dry-run to execute")
        else:
            print("\n✓ Document archived successfully")
            print("\nNext steps:")
            print("  1. Run: python scripts/generate-cross-references.py docs/")
            print("  2. Run: python scripts/archive-inventory.py docs/")
            print("  3. Commit changes to git")
    else:
        print("\n❌ Archive operation failed")
        sys.exit(1)


if __name__ == '__main__':
    main()
