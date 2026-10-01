---
name: prompt-unit-testing
description: "Compatibility wrapper for the unit-testing SDLC skill."
argument-hint: "<WORK-ID> <mode: standalone|current_story|existing_work_id> <target behavior> <scope anchor>"
---

Use the `/unit-testing` skill with the inputs supplied by the user.

The skill is authoritative. Do not reproduce or independently execute the unit-testing
procedure from this prompt. Follow the skill's prerequisites, cache rules, agent mapping
(`test-author`), outputs, human gate, and stop condition.