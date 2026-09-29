# BUG-301 Implementation Plan

1. Replace direct ordering on the string-backed Severity field with a translated severity-rank expression in AlertRepository.ApplySorting.
2. Strengthen the repository regression test so severity sorting is exercised under a relational EF provider rather than only EF InMemory semantics.
3. Run the narrow severity-sort repository test first, then the repository test suite, then a solution build.
