
using SolaceSystems.Solclient.Messaging;
using SolaceSharp.Examples;
using SolaceSharp.Operations;
using System.Threading.Tasks;
using System.Linq;

namespace SolaceSharp.V2.ByExamples;

public class BasicPubSub
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

        // publish 100 pings to solace fire and forget style, the method returns
        // when the payload has been written to the transport without waiting for an ack
        var meta = new PublishMeta("test.ping", fireAndForget: true);
        foreach (var i in Enumerable.Range(1, 100))
        {
            await client.PublishAsync(new Ping() { Message = $"Hi {i}" }, meta);
        }

        using var cts = GetTimeoutToken(TimeSpan.FromSeconds(3));
        var handle = Handle;

        //Consume pings
        await foreach (var msg in sub.AsAsyncEnumerable(cts.Token).Catch(handle))
        {
            Console.WriteLine(msg.Payload.Message);
        }


        await using var sub2 = await client.SubscribeAsync<Ping>("test.ping");
        using var cts2 = GetTimeoutToken(TimeSpan.FromSeconds(3));

        // publish 100 pings to solace  and wait for each one to ack before proceeding, the method returns
        // when solace has acked our message
        meta = new PublishMeta("test.ping", fireAndForget: false);
        foreach (var i in Enumerable.Range(101, 100))
        {
            await client.PublishAsync(new Ping() { Message = $"Hi {i}" }, meta);
        }

        //Consume pings
        await foreach (var msg in sub2.AsAsyncEnumerable(cts2.Token).Catch(handle))
        {
            Console.WriteLine(msg.Payload.Message);
        }



        await using var sub3 = await client.SubscribeAsync<Ping>("test.ping");
        using var cts3 = GetTimeoutToken(TimeSpan.FromSeconds(3));

        // publish 100 pings to solace and wait for them to be acked in parallel, good for guarenteed messaging
        var futures = new List<PublishFuture>();
        foreach (var i in Enumerable.Range(201, 100))
        {
            var future = await client.PublishConcurrentAsync(new Ping() { Message = $"Hi {i}" }, meta);
            futures.Add(future);
        }

        foreach (var future in futures)
        {
            var res = await future.GetResponseAsync();
            if (res != SendResponse.Ack)
                Console.WriteLine($"FAIL {res}");
        }

        //Consume pings
        await foreach (var msg in sub3.AsAsyncEnumerable(cts3.Token).Catch(handle))
        {
            Console.WriteLine(msg.Payload.Message);
        }

    }

    private IAsyncEnumerable<Message<Ping>> Handle(Exception ex)
    {
        if(ex is OperationCanceledException tce)
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