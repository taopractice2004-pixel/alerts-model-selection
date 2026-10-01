---
name: prompt-fix-bugs
description: "Compatibility wrapper for the fix-bugs SDLC skill."
argument-hint: "<BUG-ID> <description> <mode: standalone|current_story|existing_work_id> <scoping hint>"
---

Use the `/fix-bugs` skill with the inputs supplied by the user.

The skill is authoritative. Do not reproduce or independently execute the bug-fix procedure
from this prompt. Follow the skill's prerequisites, cache rules, agent mapping (`bug-fixer`),
outputs, human gate, and stop condition.