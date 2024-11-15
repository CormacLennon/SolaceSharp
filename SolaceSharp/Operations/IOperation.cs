using SolaceSystems.Solclient.Messaging;
using System;

namespace SolaceSharp.Operations
{
    internal interface IOperation
    {
        Guid Id { get; }
        void Execute();
        void HandleException(Exception ex);
        void HandleResponse(SessionEventArgs args);
    }
}
