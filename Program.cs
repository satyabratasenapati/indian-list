using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using MunicipalityTax.Application.Commands;
using MunicipalityTax.Domain.Enums; // For TaxType enum

public class Program
{
    private static readonly HttpClient _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000/api/") };

    public static async Task Main(string[] args)
    {
        Console.WriteLine("Municipality Tax API Consumer Application");
        Console.WriteLine("-----------------------------------------");

        // --- Demonstrate GET Tax ---
        Console.WriteLine("\n--- GET Tax Example ---");
        await GetTax("Copenhagen", "2024.01.01"); // Yearly tax
        await GetTax("Copenhagen", "2024.01.02"); // Daily tax (if active range is full year)
        await GetTax("Copenhagen", "2024.02.01"); // Monthly tax on 1st
        await GetTax("Roskilde", "2024.01.01"); // Should be 0 (not Monday)
        await GetTax("Roskilde", "2024.01.08"); // Weekly tax (Monday)
        await GetTax("NonExistentCity", "2024.03.15"); // Not found
        await GetTax("Odense", "2024.05.01"); // Specific daily tax

        // --- Demonstrate POST Add Tax Rule ---
        Console.WriteLine("\n--- POST Add Tax Rule Example ---");

        // Adding a new daily tax for Bangalore on a specific day
        var addCommandBangaloreDaily = new AddTaxRuleCommand
        {
            MunicipalityName = "Bangalore",
            Type = TaxType.Daily,
            TaxValue = 0.08m,
            StartDate = new DateTime(2024, 6, 15),
            EndDate = new DateTime(2024, 6, 15),
            DayOfMonth = null,
            DayOfWeek = null,
            DayOfYear = null // Ensure specific day fields are null for daily
        };
        await AddNewTaxRule(addCommandBangaloreDaily);
        await GetTax("Bangalore", "2024.06.15"); // Expected: 0.08

        // Adding a new monthly tax for Chennai for entire 2024 on 1st of month
        var addCommandChennaiMonthly = new AddTaxRuleCommand
        {
            MunicipalityName = "Chennai",
            Type = TaxType.Monthly,
            TaxValue = 0.04m,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31),
            DayOfMonth = 1,
            DayOfWeek = null,
            DayOfYear = null
        };
        await AddNewTaxRule(addCommandChennaiMonthly);
        await GetTax("Chennai", "2024.07.01"); // Expected: 0.04

        // Adding a new yearly tax for a new city (Lucknow)
        var addCommandLucknowYearly = new AddTaxRuleCommand
        {
            MunicipalityName = "Lucknow",
            Type = TaxType.Yearly,
            TaxValue = 0.12m,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 12, 31)
        };
        await AddNewTaxRule(addCommandLucknowYearly);
        await GetTax("Lucknow", "2025.06.01"); // Expected: 0.12

        // --- Demonstrate PUT Update Tax Rule ---
        Console.WriteLine("\n--- PUT Update Tax Rule Example ---");

        // First, add a rule to ensure we have one to update (if not already there)
        var tempRule = new AddTaxRuleCommand
        {
            MunicipalityName = "Hyderabad",
            Type = TaxType.Daily,
            TaxValue = 0.02m,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31)
        };
        await AddNewTaxRule(tempRule);
        await GetTax("Hyderabad", "2024.05.01"); // Verify initial tax

        // To update, we first need to know the ID of the rule.
        // In a real app, you'd have an API to list rules, or the Add operation would return the ID.
        // For this demo, let's assume we know a rule exists or add one specifically to get an ID.
        // This is a simplification; ideally, you'd query for the ID.
        Console.WriteLine("Please get an existing Tax Rule ID from Swagger UI or Database for testing update.");
        Console.WriteLine("Attempting to update a hypothetical rule with ID 1 (Copenhagen Yearly) - this might fail if ID 1 isn't what you expect.");

        var updateCommandCopenhagen = new UpdateTaxRuleCommand
        {
            Id = 1, // IMPORTANT: Use a real ID from your DB if this is run multiple times
            MunicipalityName = "Copenhagen",
            Type = TaxType.Yearly,
            TaxValue = 0.25m, // Changing tax value
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31)
        };
        await UpdateTaxRule(updateCommandCopenhagen);
        await GetTax("Copenhagen", "2024.01.01"); // Verify updated tax (should be 0.25 if ID 1 was Copenhagen's yearly)


        // --- Demonstrate POST Import Tax Rules from File ---
        Console.WriteLine("\n--- POST Import Tax Rules from File Example ---");
        // Ensure you have a 'tax_rules_import.csv' file in the API's bin/Debug/net8.0 folder
        // or specify a full path like "C:\\temp\\tax_rules_import.csv"
        string importFilePath = "indian_tax_rules.csv"; // Use the Indian city names CSV
        await ImportTaxRules(importFilePath, "Local"); // "Local" is the default source type
        await GetTax("Delhi", "2024.08.15"); // Verify imported daily tax for Delhi
        await GetTax("Chennai", "2024.01.09"); // Verify imported weekly tax for Chennai (Tuesday)

        Console.WriteLine("\n--- All examples completed. Check console output for results. ---");
        Console.ReadKey();
    }

    static async Task GetTax(string municipalityName, string date)
    {
        Console.WriteLine($"\nGetting tax for {municipalityName} on {date}...");
        try
        {
            var response = await _httpClient.GetAsync($"Tax/{municipalityName}/{date}");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<dynamic>();
                Console.WriteLine($"SUCCESS: Municipality: {result?.Municipality}, Date: {result?.Date}, Tax: {result?.Tax}");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"ERROR {response.StatusCode}: {response.ReasonPhrase} - {errorContent}");
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP Request Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred while getting tax: {ex.Message}");
        }
    }

    static async Task AddNewTaxRule(AddTaxRuleCommand command)
    {
        Console.WriteLine($"\nAdding new tax rule for {command.MunicipalityName} ({command.Type})...");
        try
        {
            var response = await _httpClient.PostAsJsonAsync("Municipality/taxrule", command);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"SUCCESS: {result}");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"ERROR {response.StatusCode}: {response.ReasonPhrase} - {errorContent}");
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP Request Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred while adding tax rule: {ex.Message}");
        }
    }

    static async Task UpdateTaxRule(UpdateTaxRuleCommand command)
    {
        Console.WriteLine($"\nUpdating tax rule ID {command.Id} for {command.MunicipalityName}...");
        try
        {
            var response = await _httpClient.PutAsJsonAsync("Municipality/taxrule", command);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"SUCCESS: {result}");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"ERROR {response.StatusCode}: {response.ReasonPhrase} - {errorContent}");
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP Request Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred while updating tax rule: {ex.Message}");
        }
    }

    static async Task ImportTaxRules(string filePath, string fileSourceType)
    {
        Console.WriteLine($"\nImporting tax rules from '{filePath}' using '{fileSourceType}' source...");
        try
        {
            // Note: Query parameters need to be properly encoded for HTTPClient
            var response = await _httpClient.PostAsync($"Municipality/import-tax-rules?filePath={Uri.EscapeDataString(filePath)}&fileSourceType={Uri.EscapeDataString(fileSourceType)}", null);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"SUCCESS: {result}");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"ERROR {response.StatusCode}: {response.ReasonPhrase} - {errorContent}");
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP Request Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred during import: {ex.Message}");
        }
    }
}


using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for clean, readable output
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj} (Thread: {ThreadId}){NewLine}{Exception}")
    .WriteTo.File("Logs/app-.log", rollingInterval: RollingInterval.Day,
        outputTemplate:
        "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();


// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj} " +
        "{Properties}{NewLine}{Exception}")
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj} {Properties}{NewLine}{Exception}")
    .WriteTo.Seq("http://localhost:5341") // Optional: If using Seq
    .CreateLogger();
