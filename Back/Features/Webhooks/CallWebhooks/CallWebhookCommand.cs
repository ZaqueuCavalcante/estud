using System.Text;
using System.Diagnostics;

namespace Estud.Back.Features.Webhooks.CallWebhooks;

[CommandDescription("Chama um webhook")]
public record CallWebhookCommand(string WebhookCallUid) : ICommand;

public class CallWebhookCommandHandler(EstudDbContext ctx, IHttpClientFactory factory) : ICommandHandler<CallWebhookCommand>
{
    public async Task Handle(int commandId, CallWebhookCommand command)
    {
        var call = await ctx.WebhookCalls
            .Include(x => x.Attempts)
            .FirstAsync(x => x.Uid == command.WebhookCallUid);

        var subscription = await ctx.WebhookSubscriptions.AsNoTracking()
            .Where(x => x.Id == call.WebhookSubscriptionId)
            .Select(x => new { x.Url, x.CustomHeaders })
            .FirstAsync();

        var client = factory.CreateClient();
        client.BaseAddress = new Uri(subscription.Url);
        foreach (var header in subscription.CustomHeaders)
        {
            client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var payload = new StringContent(call.Payload, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("", payload);
            var responseContent = await response.Content.ReadAsStringAsync();

            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                call.Success((int)response.StatusCode, responseContent, (int)stopwatch.ElapsedMilliseconds);
            }
            else
            {
                call.Failed((int)response.StatusCode, responseContent, (int)stopwatch.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            call.Failed(999, ex.Message, (int)stopwatch.ElapsedMilliseconds);
        }
    }
}
