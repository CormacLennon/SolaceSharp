
using SolaceSystems.Solclient.Messaging;
using SolaceSharp.Examples;

namespace SolaceSharp.V2.ByExamples;

public class RequestReply
{
    public async Task Run()
    {
        var properties = new SessionProperties()
        {
            Host = "localhost",
            VPNName = "test_vpn",
            ClientName = "SolaceByExamples",
            UserName = "admin",
            Password = "password",

            AckEventMode = SessionProperties.AckMode.PER_MSG,
            BlockWhileConnecting = false,
            ConnectBlocking = false,
            SendBlocking = false,
            SubscribeBlocking = false,
            ConnectRetries = 5,
            ConnectTimeoutInMsecs = 5000,
            ReapplySubscriptions = true,
            TopicDispatch = true,
        };

        var serializers = new ExampleSerializerRegistry();

        var client = new SolaceClient(properties, serializers);

        await client.ConnectAsync();


        // Subscribe to pings
        await using var sub = await client.SubscribeAsync<Ping>("test.ping");

        var cts = new CancellationTokenSource();
        var handler = Handle;

        //Listen for ping requests off the main thread
        var replyTask = Task.Run(async () =>
        {
            await foreach (var msg in sub.AsAsyncEnumerable(cts.Token).Catch(handler))
            {
                var pong = new Pong()
                {
                    Message = $"Hi {msg.Payload.Message}"
                };
                await msg.SendReply(pong);
            }
        });

        await Task.Delay(500);

        // publish 100 ping requests to solace
        var meta = new PublishMeta("test.ping", true);
        foreach (var i in Enumerable.Range(1, 100))
        {
            var request = new Ping() { Message = $"Request {i}" };
            var res = await client.SendRequest<Ping, Pong>(request, meta, timeout: TimeSpan.FromSeconds(10));
            Console.WriteLine(res.Message);
        }

        cts.Cancel();
        await replyTask;
    }

    private IAsyncEnumerable<Message<Ping>> Handle(Exception ex)
    {
        if (ex is OperationCanceledException tce)
        {
            return Enumerable.Empty<Message<Ping>>().ToAsyncEnumerable();
        }
        throw ex;
    }

    private CancellationTokenSource GetTimeoutToken(TimeSpan time)
    {
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(10));
        return cts;
    }
}