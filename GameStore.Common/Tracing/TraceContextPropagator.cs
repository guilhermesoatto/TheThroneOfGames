using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace GameStore.Common.Tracing;

/// <summary>
/// Propaga o contexto de trace distribuído (W3C traceparent/tracestate) através do corpo JSON
/// das mensagens publicadas no RabbitMQ. Não usa cabeçalhos AMQP porque o binding
/// [RabbitMQTrigger] do Azure Functions Worker isolated (usado em GameStore.Notifications.Functions)
/// só expõe o corpo da mensagem como string, sem acesso às propriedades/headers do AMQP — ver
/// https://learn.microsoft.com/azure/azure-functions/functions-bindings-rabbitmq-trigger. Embutir o
/// traceparent no próprio payload funciona para qualquer tipo de consumidor (BaseEventConsumer
/// nativo em AMQP ou o binding de string do Functions).
/// </summary>
public static class TraceContextPropagator
{
    private const string TraceParentField = "__traceparent";
    private const string TraceStateField = "__tracestate";

    /// <summary>
    /// Insere o traceparent/tracestate da Activity atual (se houver) no JSON da mensagem antes de
    /// publicar. Campos desconhecidos são ignorados pelo Newtonsoft na desserialização tipada, então
    /// isso não quebra consumidores que ainda não conhecem estes campos.
    /// </summary>
    public static string Inject(string json)
    {
        var activity = Activity.Current;
        if (activity == null)
        {
            return json;
        }

        var token = JToken.Parse(json);
        if (token is not JObject obj)
        {
            return json;
        }

        obj[TraceParentField] = activity.Id;
        if (!string.IsNullOrEmpty(activity.TraceStateString))
        {
            obj[TraceStateField] = activity.TraceStateString;
        }

        return obj.ToString(Newtonsoft.Json.Formatting.None);
    }

    /// <summary>
    /// Extrai o ActivityContext do traceparent/tracestate embutido no JSON da mensagem, se presente.
    /// </summary>
    public static bool TryExtract(string json, out ActivityContext context)
    {
        context = default;

        JToken token;
        try
        {
            token = JToken.Parse(json);
        }
        catch (Newtonsoft.Json.JsonReaderException)
        {
            return false;
        }

        if (token is not JObject obj)
        {
            return false;
        }

        var traceParent = obj[TraceParentField]?.Value<string>();
        if (string.IsNullOrEmpty(traceParent))
        {
            return false;
        }

        var traceState = obj[TraceStateField]?.Value<string>();

        return ActivityContext.TryParse(traceParent, traceState, isRemote: true, out context);
    }
}
