using System;
using System.Threading;
using System.Threading.Tasks;
using CloudFlareExtension.Models;
using Spectre.Console;

namespace CloudFlareExtension.ConsoleOutput;

internal static class SpectreConsoleReporter
{
    private const string DisableEnvironmentVariable = "CLOUDFLARE_EXTENSION_NO_CONSOLE";

    private static readonly Lazy<bool> Interactive = new(() =>
        !System.Console.IsOutputRedirected &&
        string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(DisableEnvironmentVariable)));

    private static readonly Lazy<IAnsiConsole> LazyConsole = new(() =>
    {
        var settings = new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput(System.Console.Error)
        };

        return AnsiConsole.Create(settings);
    });

    internal static bool IsEnabled => Interactive.Value;

    private static IAnsiConsole Ansi => LazyConsole.Value;

    internal static async Task<T> RunOperationAsync<T>(string description, Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        if (!IsEnabled)
        {
            return await operation(cancellationToken);
        }

        var escapedDescription = Markup.Escape(description);
        T? result = default;
        Exception? failure = null;

        await Ansi.Progress()
            .Columns(new TaskDescriptionColumn(), new SpinnerColumn(), new ProgressBarColumn(), new PercentageColumn())
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask(escapedDescription, autoStart: true);
                try
                {
                    result = await operation(cancellationToken);
                    task.Value = task.MaxValue;
                }
                catch (Exception ex)
                {
                    failure = ex;
                    task.StopTask();
                }
            });

        if (failure is not null)
        {
            throw failure;
        }

        return result!;
    }

    internal static void WriteInfo(string message)
    {
        if (!IsEnabled)
        {
            return;
        }

        Ansi.MarkupLine($"[cyan]{Markup.Escape(message)}[/]");
    }

    internal static void WriteSuccess(string message)
    {
        if (!IsEnabled)
        {
            return;
        }

        Ansi.MarkupLine($"[green]✔ {Markup.Escape(message)}[/]");
    }

    internal static void WriteWarning(string message)
    {
        if (!IsEnabled)
        {
            return;
        }

    Ansi.MarkupLine($"[yellow]⚠ {Markup.Escape(message)}[/]");
    }

    internal static void RenderDnsRecordSummary(CloudFlareDnsRecord record)
    {
        if (!IsEnabled)
        {
            return;
        }

        var table = CreateSummaryTable("DNS Record");
        table.AddRow("Name", record.Name);
        table.AddRow("Type", record.Type);
        table.AddRow("Content", record.Content);
        table.AddRow("TTL", record.Ttl.ToString());
        table.AddRow("Proxied", record.Proxied.ToString());
        table.AddRow("Proxiable", record.Proxiable.ToString());
        table.AddRow("Zone Id", record.ZoneId);
        table.AddRow("Record Id", record.RecordId ?? "(pending)");
        if (!string.IsNullOrWhiteSpace(record.Comment))
        {
            table.AddRow("Comment", record.Comment!);
        }

        Ansi.Write(table);
    }

    internal static void RenderZoneSummary(CloudFlareZone zone, bool existedPrior)
    {
        if (!IsEnabled)
        {
            return;
        }

        var table = CreateSummaryTable(existedPrior ? "Existing Zone" : "Created Zone");
        table.AddRow("Name", zone.Name);
        table.AddRow("Zone Id", zone.ZoneId ?? "(pending)");
        table.AddRow("Status", zone.Status);
        table.AddRow("Plan", zone.Plan);
        table.AddRow("Paused", zone.Paused.ToString());
        if (zone.NameServers is { Length: > 0 })
        {
            table.AddRow("Name Servers", string.Join(", ", zone.NameServers));
        }

        Ansi.Write(table);
    }

    private static Table CreateSummaryTable(string title)
    {
        var table = new Table
        {
            Title = new TableTitle($"[bold]{Markup.Escape(title)}[/]")
        };

        table.Border(TableBorder.Rounded);
        table.Expand();
        table.AddColumn(new TableColumn("[grey]Property[/]"));
        table.AddColumn(new TableColumn("[grey]Value[/]"));

        return table;
    }
}
