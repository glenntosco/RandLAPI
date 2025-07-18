# R&L Carrier BOL Integration Service - Deployment Guide

## Prerequisites

- Windows Server 2019+ or Windows 10+
- .NET 9 Runtime installed
- Administrative privileges for service installation

## Build and Publish

1. **Build the application:**
   ```cmd
   dotnet build --configuration Release
   ```

2. **Publish for Windows deployment:**
   ```cmd
   dotnet publish --configuration Release --runtime win-x64 --self-contained false --output ./publish
   ```

## Windows Service Installation

### Option 1: Using sc.exe (Command Line)

1. **Copy published files** to target directory (e.g., `C:\Services\RandLAPI\`)

2. **Install the service:**
   ```cmd
   sc create "RandL-BOL-Integration" binPath="C:\Services\RandLAPI\RandLAPI.exe" start=auto
   sc description "RandL-BOL-Integration" "R&L Carrier Bill of Lading Integration Service for P4 Warehouse"
   ```

3. **Start the service:**
   ```cmd
   sc start "RandL-BOL-Integration"
   ```

### Option 2: Using PowerShell

1. **Install as service:**
   ```powershell
   New-Service -Name "RandL-BOL-Integration" -BinaryPathName "C:\Services\RandLAPI\RandLAPI.exe" -DisplayName "R&L BOL Integration" -StartupType Automatic -Description "R&L Carrier Bill of Lading Integration Service for P4 Warehouse"
   ```

2. **Start the service:**
   ```powershell
   Start-Service "RandL-BOL-Integration"
   ```

## Configuration

### Update appsettings.json

Before starting the service, update the configuration in `appsettings.json`:

```json
{
  "ServiceSettings": {
    "BaseUrl": "https://your-p4warehouse-url.com/",
    "ApiKey": "your-p4w-api-key",
    "MaxRecordsPerCheck": 100,
    "CheckIntervalSeconds": 30
  },
  "RLCarrierSettings": {
    "BaseUrl": "https://api.rlc.com",
    "ApiKey": "your-rl-carrier-api-key",
    "Endpoint": "/BillOfLading"
  }
}
```

### Security Considerations

For production deployment, consider:

1. **User Secrets** (Development):
   ```cmd
   dotnet user-secrets set "ServiceSettings:ApiKey" "your-actual-api-key"
   dotnet user-secrets set "RLCarrierSettings:ApiKey" "your-actual-api-key"
   ```

2. **Environment Variables**:
   ```cmd
   setx ServiceSettings__ApiKey "your-actual-api-key" /M
   setx RLCarrierSettings__ApiKey "your-actual-api-key" /M
   ```

3. **Azure Key Vault** (Recommended for production):
   - Install Azure Key Vault package
   - Configure Key Vault connection
   - Store sensitive keys in vault

## Service Management

### Check Service Status
```cmd
sc query "RandL-BOL-Integration"
```

### Stop Service
```cmd
sc stop "RandL-BOL-Integration"
```

### Start Service
```cmd
sc start "RandL-BOL-Integration"
```

### Remove Service
```cmd
sc stop "RandL-BOL-Integration"
sc delete "RandL-BOL-Integration"
```

## Monitoring and Logging

### Windows Event Log
The service logs to Windows Event Log under:
- **Application and Services Logs** → **RandL-BOL-Integration**

### Log Levels
- **Information**: Normal operation, successful processing
- **Warning**: BOL creation failures, missing ProNumbers
- **Error**: API failures, update failures
- **Debug**: Detailed processing information

### View Logs
```powershell
Get-WinEvent -LogName Application | Where-Object {$_.ProviderName -eq "RandL-BOL-Integration"} | Select-Object TimeCreated, LevelDisplayName, Message
```

## Troubleshooting

### Common Issues

1. **Service won't start:**
   - Check .NET 9 runtime is installed
   - Verify file permissions
   - Check Event Log for errors

2. **API Connection failures:**
   - Verify URLs in configuration
   - Check API keys are correct
   - Confirm network connectivity

3. **No PickTickets processed:**
   - Verify OData filter criteria
   - Check P4W API connectivity
   - Review log output for errors

### Health Checks

Monitor the service by checking:
- Windows Service status
- Event Log entries
- P4W PickTicket updates
- R&L Carrier API responses

## Performance Tuning

### Configuration Parameters

- **CheckIntervalSeconds**: Adjust processing frequency (default: 30)
- **MaxRecordsPerCheck**: Limit batch size to prevent timeouts (default: 100)
- **100ms delay**: Between API calls to prevent throttling

### Recommendations

- **Production**: 60-300 second intervals
- **Testing**: 10-30 second intervals
- **High Volume**: Reduce MaxRecordsPerCheck to 50

## Uninstallation

1. **Stop the service:**
   ```cmd
   sc stop "RandL-BOL-Integration"
   ```

2. **Remove the service:**
   ```cmd
   sc delete "RandL-BOL-Integration"
   ```

3. **Delete service files:**
   ```cmd
   rmdir /s "C:\Services\RandLAPI"
   ```

## Support

For issues or questions:
- Check Windows Event Log first
- Review configuration settings
- Verify API connectivity
- Contact system administrator