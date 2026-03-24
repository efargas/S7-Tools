# Documentation Validation Test Fixtures

This directory contains markdown files used to test and validate the
`scripts/validate-*.py` validation suite.

## Structure

| File | Purpose |
|------|---------|
| `valid-document.md` | Well-formed document: correct frontmatter, valid file refs, valid links |
| `invalid-frontmatter.md` | Missing / malformed frontmatter fields |
| `code-blocks.md` | Code blocks with escape chars, inline code, fenced fences |
| `links.md` | Mix of valid, broken, external, anchor, and code-block links |
| `file-references.md` | Mix of existing and missing file path references |

## Usage

```bash
# Run frontmatter-specific tests
pytest scripts/tests -k frontmatter -q

# Run the full validation test suite (covers all fixture scenarios)
pytest scripts/tests -q
```

> **Note:** This directory is excluded from the production validation runs
> (`validate-frontmatter.py` and `validate-documentation.py` both skip `.test-fixtures/`
> when scanning the main docs tree).  Use the pytest suite above to exercise these
> fixtures programmatically.
