using System;

namespace SolaceSharp
{

    public class ConnectionFailedException : Exception
    {
        public ConnectionFailedException(string info) : base(info)
        {

        }
    }

    public class PublishException : Exception
    {
        public PublishException(string info) : base(info)
        {
        }
    }

    public class MessageTooBigException : Exception
    {
    }

    public class UnexpectedResponseException : Exception
    {
        public UnexpectedResponseException(string info) : base(info)
        {
        }
    }

    public class RejectedMessageException : Exception
    {
    }
    
    public class CreateSubscriptionException : Exception
    {
        public CreateSubscriptionException(string message) : base(message)
        {

        }
    }

    public class SubscribeFailedException : Exception
    {
    }

    public class UnsubscribeFailedException : Exception
    {
    }

    public class QueueProvisionFailure : Exception
    {
    }
}
