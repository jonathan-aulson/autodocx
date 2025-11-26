using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TownePark.Billing.Api.Config;

public static class Configuration
{
    public static string GetSQLConnectionString()
    {
        return GetEnvironmentVariable("SQL_CONNECTION_STRING");
    }

    public static string GetAzureServiceClientTenant()
    {
        var url = GetEnvironmentVariable("AZURE_SERVICE_CLIENT_TENANT");
        return url;
    }

    public static string GetAzureServiceClientId()
    {
        var url = GetEnvironmentVariable("AZURE_SERVICE_CLIENT_ID");
        return url;
    }

    private static string GetEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name) ?? throw new KeyNotFoundException($"{name} not found");
    }
}
