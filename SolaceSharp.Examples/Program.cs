using System;
using System.Threading.Tasks;
using SolaceSharp.V2.ByExamples;

try
{
    await new BasicPubSub().Run();
    await new RequestReply().Run();
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());
}

Console.ReadLine();