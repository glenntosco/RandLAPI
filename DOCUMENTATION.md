# RandLAPI - R&L Carrier BOL Integration Documentation

## Table of Contents
- [Overview](#overview)
- [System Architecture](#system-architecture)
- [Technical Specifications](#technical-specifications)
- [Data Flow](#data-flow)
- [API Integration](#api-integration)
- [Configuration](#configuration)
- [Code Structure](#code-structure)
- [Error Handling](#error-handling)
- [Logging](#logging)
- [Deployment](#deployment)
- [Monitoring](#monitoring)
- [Troubleshooting](#troubleshooting)

## Overview

RandLAPI is a .NET 9 background service that automates the creation of Bills of Lading (BOLs) with R&L Carriers for shipments managed by P4 Warehouse (P4W). The service continuously monitors P4W for eligible pick tickets, creates BOLs with R&L Carriers, and updates the pick tickets with the returned tracking numbers (ProNumbers).

### Key Features
- **Automated BOL Creation**: Seamlessly integrates P4W with R&L Carriers
- **Background Processing**: Runs as a Windows service with configurable intervals
- **Error Resilience**: Comprehensive error handling and retry logic
- **Configurable**: Easily adaptable to different environments and requirements
- **Monitoring Ready**: Extensive logging and Windows Event Log integration

## System Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   P4 Warehouse  │    │    RandLAPI     │    │  R&L Carriers   │
│    (Source)     │◄──►│   (Service)     │◄──►│ (Destination)   │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         │                       │                       │
    ┌────▼────┐             ┌────▼────┐             ┌────▼────┐
    │OData API│             │Background│             │BOL API  │
    │         │             │ Worker  │             │         │
    └─────────┘             └─────────┘             └─────────┘
```

### Component Responsibilities

| Component | Responsibility |
|-----------|----------------|
| **Worker** | Main background service orchestrating the integration flow |
| **PickTicketService** | Handles all P4W API interactions (GET eligible tickets, POST updates) |
| **RLCarrierService** | Manages R&L Carrier API communication for BOL creation |
| **Models** | Data transfer objects for API requests/responses |
| **Settings** | Configuration classes for service and external API settings |

## Technical Specifications

### Technology Stack
- **Runtime**: .NET 9 (Long-Term Support)
- **Service Type**: Hosted Background Service
- **HTTP Client**: Built-in `HttpClient` with dependency injection
- **JSON Serialization**: Newtonsoft.Json
- **Logging**: Microsoft.Extensions.Logging with Windows Event Log
- **Configuration**: Microsoft.Extensions.Configuration with Options pattern

### Dependencies
```xml
<PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.7" />
<PackageReference Include="Microsoft.Extensions.Hosting.WindowsServices" Version="9.0.7" />
<PackageReference Include="Microsoft.Extensions.Http" Version="9.0.7" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

## Data Flow

### 1. Pick Ticket Retrieval
```
GET /odata/PickTicket?$filter=(Carrier eq 'R&L CARRIERS' and 
    (PickTicketState eq 'ReadyToPick' or PickTicketState eq 'Waved') and 
    (ProNumber eq null or length(ProNumber) eq 0))
```

**Response Structure**:
```json
{
  "value": [
    {
      "Id": "guid",
      "PickTicketNumber": "string",
      "ProNumber": null,
      "Carrier": "R&L CARRIERS",
      "PickTicketState": "ReadyToPick"
    }
  ]
}
```

### 2. BOL Creation
```
POST https://apisandbox.rlc.com/BillOfLading
```

**Request Structure**:
```json
{
  "BillOfLading": {
    "BOLDate": "07/18/2025",
    "Shipper": {
      "CompanyName": "P4 Software Inc.",
      "AddressLine1": "3755 Breakthrough Way",
      "City": "Las Vegas",
      "StateOrProvince": "NV",
      "ZipOrPostalCode": "89135",
      "CountryCode": "USA",
      "PhoneNumber": "702-555-0101"
    },
    "Consignee": {
      "CompanyName": "Evergreen Logistics",
      "AddressLine1": "8400 NW 25th St",
      "City": "Doral",
      "StateOrProvince": "FL",
      "ZipOrPostalCode": "33198",
      "CountryCode": "USA"
    },
    "Items": [
      {
        "Class": "70",
        "Pieces": 8,
        "Weight": 960,
        "PackageType": "PLT",
        "Description": "Zebra Barcode Scanners and Mobile Computers"
      }
    ]
  }
}
```

**Response Structure**:
```json
{
  "ProNumber": "123456789",
  "Code": 0
}
```

### 3. P4W Update
```
POST /api/PickTicketApi/CreateOrUpdate
```

**Request Structure**:
```json
{
  "Id": "guid",
  "ProNumber": "123456789"
}
```

## API Integration

### P4W Integration (PickTicketService)

#### Authentication
- **Method**: API Key in request header
- **Header**: `ApiKey: {your-api-key}`

#### Key Methods
- `GetEligiblePickTicketsAsync()`: Retrieves pick tickets needing BOL creation
- `UpdatePickTicketProNumberAsync()`: Updates pick ticket with ProNumber

#### Filter Logic
The service filters pick tickets using OData query parameters:
- `Carrier eq 'R&L CARRIERS'`: Only R&L shipments
- `PickTicketState eq 'ReadyToPick' or PickTicketState eq 'Waved'`: Ready for processing
- `ProNumber eq null or length(ProNumber) eq 0`: No existing tracking number

### R&L Carrier Integration (RLCarrierService)

#### Authentication
- **Method**: API Key in request header
- **Header**: `apiKey: {your-api-key}`

#### Key Methods
- `CreateBillOfLadingAsync()`: Creates BOL and returns ProNumber

#### Response Validation
- Checks for HTTP success status codes
- Validates `Code == 0` in response
- Ensures `ProNumber` is not null or empty

## Configuration

### appsettings.json Structure
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information",
      "RandLAPI": "Debug"
    }
  },
  "ServiceSettings": {
    "BaseUrl": "https://nadc218demo.p4warehouse.com/",
    "ApiKey": "your-p4w-api-key",
    "MaxRecordsPerCheck": 100,
    "CheckIntervalSeconds": 15
  },
  "RLCarrierSettings": {
    "BaseUrl": "https://apisandbox.rlc.com",
    "ApiKey": "your-rl-carrier-api-key",
    "Endpoint": "/BillOfLading"
  }
}
```

### Configuration Classes

#### ServiceSettings
- `BaseUrl`: P4W base URL
- `ApiKey`: P4W authentication key
- `MaxRecordsPerCheck`: Batch size limit (default: 100)
- `CheckIntervalSeconds`: Processing frequency (default: 15)

#### RLCarrierSettings
- `BaseUrl`: R&L Carrier API base URL
- `ApiKey`: R&L Carrier authentication key
- `Endpoint`: BOL creation endpoint path

### Environment-Specific Configuration
- **Development**: `appsettings.Development.json`
- **Production**: Environment variables or Azure Key Vault
- **User Secrets**: For local development sensitive data

## Code Structure

### Core Components

#### Worker.cs
Main background service that orchestrates the integration:
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        await ProcessPickTicketsAsync();
        await Task.Delay(TimeSpan.FromSeconds(_settings.CheckIntervalSeconds), stoppingToken);
    }
}
```

#### PickTicketService.cs
Handles P4W API communication:
- OData query construction with proper encoding
- HTTP client configuration with base URL and API key
- JSON deserialization of OData responses
- Error handling for API failures

#### RLCarrierService.cs
Manages R&L Carrier API integration:
- BOL request payload construction
- HTTP client configuration for R&L API
- Response validation and ProNumber extraction
- Static data mapping for shipper/consignee information

### Models

#### PickTicket.cs
```csharp
public class PickTicket
{
    public Guid Id { get; set; }
    public string PickTicketNumber { get; set; } = string.Empty;
    public string? ProNumber { get; set; }
    public string Carrier { get; set; } = string.Empty;
    public string PickTicketState { get; set; } = string.Empty;
}
```

#### RLCarrierBOLRequest.cs
Hierarchical structure for BOL creation:
- `BillOfLading` (root)
  - `Shipper` (company and address details)
  - `Consignee` (delivery destination)
  - `Items[]` (shipment contents)

#### RLCarrierBOLResponse.cs
```csharp
public class RLCarrierBOLResponse
{
    public string ProNumber { get; set; } = string.Empty;
    public int Code { get; set; }
}
```

## Error Handling

### Exception Management
The service implements comprehensive error handling at multiple levels:

#### Worker Level
```csharp
try
{
    await ProcessPickTicketsAsync();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unhandled error in processing cycle");
}
```

#### Service Level
- **PickTicketService**: Returns empty collections on API failures
- **RLCarrierService**: Returns null on BOL creation failures
- **Individual Operations**: Try-catch around each pick ticket processing

#### Recovery Strategies
- **Transient Failures**: Continue processing remaining items
- **API Unavailable**: Log error and retry on next cycle
- **Invalid Data**: Skip problematic records and continue
- **Configuration Issues**: Service fails to start (fail-fast principle)

### Error Scenarios

| Scenario | Handling | Impact |
|----------|----------|---------|
| P4W API Unavailable | Log error, return empty list | Skip cycle, retry next interval |
| R&L API Returns 400 | Log error, return null | Skip BOL creation, continue with next ticket |
| ProNumber Missing | Log warning, return null | Skip P4W update, continue processing |
| P4W Update Fails | Log error, return false | Log failure, continue with next ticket |
| Invalid Configuration | Throw exception | Service fails to start |

## Logging

### Log Levels and Usage

#### Information Level
- Service startup/shutdown
- Successful BOL creation and P4W updates
- Processing statistics (count of tickets processed)

#### Warning Level
- R&L API returns success but no ProNumber
- BOL creation failures that don't throw exceptions

#### Error Level
- API communication failures
- P4W update failures
- Unhandled exceptions in processing

#### Debug Level
- Detailed request/response information
- OData query construction
- Processing cycle start/end

### Log Output Examples
```
[Information] R&L Carrier BOL Integration Service started
[Information] Processing 3 eligible PickTickets
[Information] Successfully processed PickTicket PT-12345 with ProNumber 987654321
[Warning] Failed to create BOL for PickTicket PT-12346
[Error] Error processing PickTicket PT-12347: API timeout
[Debug] Starting PickTicket processing cycle
```

### Windows Event Log Integration
When running as a Windows service:
- Application and Services Logs → RandL-BOL-Integration
- Structured logging with event IDs
- Integration with Windows monitoring tools

## Deployment

### Prerequisites
- Windows Server 2019+ or Windows 10+
- .NET 9 Runtime
- Administrative privileges for service installation
- Network access to P4W and R&L Carrier APIs

### Build Process
```bash
# Build application
dotnet build --configuration Release

# Publish for deployment
dotnet publish --configuration Release --runtime win-x64 --self-contained false --output ./publish
```

### Service Installation
```cmd
# Install Windows service
sc create "RandL-BOL-Integration" binPath="C:\Services\RandLAPI\RandLAPI.exe" start=auto

# Set description
sc description "RandL-BOL-Integration" "R&L Carrier Bill of Lading Integration Service for P4 Warehouse"

# Start service
sc start "RandL-BOL-Integration"
```

### Configuration Security
For production deployments:

#### Environment Variables
```cmd
setx ServiceSettings__ApiKey "your-actual-api-key" /M
setx RLCarrierSettings__ApiKey "your-actual-api-key" /M
```

#### Azure Key Vault (Recommended)
- Install Azure.Extensions.AspNetCore.Configuration.Secrets
- Configure Key Vault connection in Program.cs
- Store all sensitive configuration in vault

## Monitoring

### Health Indicators
- **Service Status**: Windows Service Control Manager
- **Processing Rate**: Log entries showing ticket processing
- **API Connectivity**: Error rates in logs
- **Business Metrics**: ProNumber assignment success rate

### Key Metrics to Monitor
- Pick tickets processed per cycle
- BOL creation success rate
- P4W update success rate
- Average processing time per ticket
- API response times

### Monitoring Queries
```powershell
# View recent service logs
Get-WinEvent -LogName Application | Where-Object {$_.ProviderName -eq "RandL-BOL-Integration"} | Select-Object TimeCreated, LevelDisplayName, Message

# Check service status
Get-Service "RandL-BOL-Integration"

# Monitor processing rate
Get-WinEvent -FilterHashtable @{LogName='Application'; ProviderName='RandL-BOL-Integration'; Level=4} | Group-Object {$_.TimeCreated.Date} | Select-Object Name, Count
```

## Troubleshooting

### Common Issues

#### Service Won't Start
**Symptoms**: Service fails to start, Event Log shows startup errors
**Causes**: 
- Missing .NET 9 runtime
- Invalid configuration
- Insufficient permissions
**Resolution**: Verify runtime installation, check appsettings.json syntax, run as administrator

#### No Pick Tickets Processed
**Symptoms**: Service runs but no tickets are processed
**Causes**:
- Incorrect OData filter
- P4W API connectivity issues
- Authentication failures
**Resolution**: Check P4W URL and API key, verify network connectivity, review filter criteria

#### BOL Creation Failures
**Symptoms**: Pick tickets found but BOLs not created
**Causes**:
- R&L API connectivity issues
- Invalid API key
- Malformed request payload
**Resolution**: Verify R&L API key, check network access, review request format

#### P4W Update Failures
**Symptoms**: BOLs created but ProNumbers not saved
**Causes**:
- P4W API authentication issues
- Invalid pick ticket IDs
- API endpoint changes
**Resolution**: Verify P4W API key, check endpoint URL, validate ticket ID format

### Diagnostic Steps

1. **Check Service Status**
   ```cmd
   sc query "RandL-BOL-Integration"
   ```

2. **Review Event Logs**
   ```powershell
   Get-WinEvent -LogName Application -MaxEvents 50 | Where-Object {$_.ProviderName -eq "RandL-BOL-Integration"}
   ```

3. **Test API Connectivity**
   - Manual API calls using Postman or curl
   - Verify network access and firewall rules
   - Validate API keys and endpoints

4. **Configuration Validation**
   - Check appsettings.json syntax
   - Verify all required settings are present
   - Test with known good configuration values

### Support Information
- **Log Location**: Windows Event Log → Application and Services Logs
- **Configuration Path**: Service installation directory
- **Common Log Patterns**: Search for "Error", "Failed", "Exception"
- **Performance Baseline**: ~1-2 seconds per pick ticket under normal conditions

---

*Last Updated: July 18, 2025*
*Version: 1.0*