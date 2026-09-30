# ALERT-410 Story Context

## Story
- ID: ALERT-410
- Project: ALERT (inferred from issue key)
- Title: Alert Tagging
- Goal: Operators can attach multiple free-form tags to alerts and filter alert lists by tag.

## Constraints
- Tags are case-insensitively deduplicated.
- Max 10 tags per alert.
- Each tag length is 1-30 chars.
- Tag remove endpoint returns 404 when alert or assignment is missing.
- List endpoint tag filter composes with existing filters.

## Scope Ownership
- Exact files, commands, standards, and unresolved questions are authoritative in implementation-cache.json.
