# 202 Impact Map

## Primary Source Slice
- AlertService.API/Controllers/AlertsController.cs: add one bulk acknowledgement endpoint and wire request/response types
- AlertService.API/Services/IAlertService.cs: add one bulk acknowledgement operation
- AlertService.API/Services/AlertManagementService.cs: own eligibility rules and result shaping
- AlertService.Data/Interfaces/IAlertRepository.cs: add the minimum bulk data contract needed by the service
- AlertService.Data.SQL/Repositories/AlertRepository.cs: implement bulk retrieval and batch save behavior
- AlertService.Models/Alert.cs: conditional change only if acknowledgement is a persisted state
- AlertService.DTO/Requests/BulkAcknowledgeAlertsRequest.cs: new request contract
- AlertService.DTO/Responses/BulkAcknowledgeAlertsResponse.cs: new result contract

## Focused Tests
- AlertService.API.Tests/Controllers/AlertsControllerTests.cs
- AlertService.API.Tests/Services/AlertManagementServiceTests.cs
- AlertService.Data.SQL.Tests/Repositories/AlertRepositoryTests.cs

## Out-of-Scope Edges
- Any unrelated alert lifecycle refactor
- UI or client changes
- Versioning changes unless separately required
- Changing existing deactivate behavior without clarification