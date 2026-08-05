using System.Reflection;

namespace CloudflareExtension.Tests.Handlers;

/// <summary>
/// Reflection-based helpers for invoking protected handler methods in unit tests.
/// </summary>
internal static class HandlerTestHelper
{
    /// <summary>
    /// Invokes the protected <c>CreateOrUpdate</c> method on a handler, returning the Properties of the response.
    /// </summary>
    public static async Task<TProperties> InvokeCreateOrUpdateAsync<THandler, TProperties>(
        THandler handler,
        TProperties properties,
        CancellationToken cancellationToken = default)
        where THandler : class
        where TProperties : class
    {
        var request = BuildRequest(handler, properties);

        var method = typeof(THandler).GetMethod(
            "CreateOrUpdate",
            BindingFlags.NonPublic | BindingFlags.Instance);

        var task = (Task)method!.Invoke(handler, [request, cancellationToken])!;
        await task;

        var responseProperties = task.GetType()
            .GetProperty("Result")!
            .GetValue(task);

        return (TProperties)responseProperties!.GetType()
            .GetProperty("Properties")!
            .GetValue(responseProperties)!;
    }

    /// <summary>
    /// Invokes the protected <c>Preview</c> method on a handler, returning the Properties of the response.
    /// </summary>
    public static async Task<TProperties> InvokePreviewAsync<THandler, TProperties>(
        THandler handler,
        TProperties properties,
        CancellationToken cancellationToken = default)
        where THandler : class
        where TProperties : class
    {
        var request = BuildRequest(handler, properties);

        var method = typeof(THandler).GetMethod(
            "Preview",
            BindingFlags.NonPublic | BindingFlags.Instance);

        var task = (Task)method!.Invoke(handler, [request, cancellationToken])!;
        await task;

        var responseProperties = task.GetType()
            .GetProperty("Result")!
            .GetValue(task);

        return (TProperties)responseProperties!.GetType()
            .GetProperty("Properties")!
            .GetValue(responseProperties)!;
    }

    private static object BuildRequest<THandler, TProperties>(THandler handler, TProperties properties)
        where THandler : class
        where TProperties : class
    {
        // Walk up to ResourceHandler`3 to get the concrete ResourceRequest type
        var handlerType = typeof(THandler);
        var baseBase = handlerType.BaseType!.BaseType!; // ResourceHandler<TProps, TIds, TConfig>
        var typeArgs = baseBase.GetGenericArguments();  // [TProperties, TIdentifiers, TConfig]

        var rhBase = handlerType.BaseType!.BaseType!.GetGenericTypeDefinition()
                     ?? typeof(THandler).BaseType!.BaseType!;

        // Find ResourceHandler`3 generic type def from the assembly
        var extAsm = typeof(THandler).Assembly.GetReferencedAssemblies()
            .Select(System.Reflection.Assembly.Load)
            .SelectMany(a => { try { return a.GetTypes(); } catch { return []; } })
            .FirstOrDefault(t => t.Name == "ResourceHandler`3")
            ?.Assembly
            .GetType("Bicep.Local.Extension.Host.Handlers.ResourceHandler`3");

        if (extAsm is null)
        {
            // Fallback: load directly from the loaded assemblies
            extAsm = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return []; } })
                .First(t => t.FullName == "Bicep.Local.Extension.Host.Handlers.ResourceHandler`3");
        }

        var reqNestedDef = extAsm.GetNestedType("ResourceRequest")!;
        var concreteReqType = reqNestedDef.MakeGenericType(typeArgs);

        var req = Activator.CreateInstance(concreteReqType)!;
        concreteReqType.GetProperty("Properties")!.SetValue(req, properties);
        return req;
    }
}
