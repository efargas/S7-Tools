import re

files_to_fix = [
    'docs/.metadata/migration-log.json',
    'docs/.metadata/migration-tracking.json'
]

for file_path in files_to_fix:
    with open(file_path, 'r') as f:
        content = f.read()

    # Find the end of the JSON array/object (which ends with } followed by \n\n## Related)
    match = re.search(r'\}\s*\n+## Related Documentation', content)
    if match:
        content = content[:match.start() + 1] + '\n'
        with open(file_path, 'w') as f:
            f.write(content)
        print(f"Fixed {file_path}")
    else:
        print(f"Did not find the markdown section in {file_path}")
