# ALERT-201 Implementation Plan

1. Extend `AlertQueryRequest` with nullable `createdFrom` and `createdTo` fields plus request-level validation for invalid ranges.
2. Keep `AlertsController` bound to `AlertQueryRequest` and rely on the existing API validation pipeline for `400` responses.
3. Thread the new date-range values through `AlertManagementService` and `IAlertRepository.GetAllAsync` without changing unrelated query behavior.
4. Apply inclusive `CreatedDate` predicates in `AlertRepository.GetAllAsync` before counting, sorting, and paging.
5. Add focused tests for invalid ranges, service pass-through, and repository lower-bound, upper-bound, and combined-range filtering.
6. Validate with focused test commands first, then broader build/test commands if needed.
