using System;


namespace RelayHybridConnRx.CustomException
{
    public class RelayClientException : Exception
    {
        public RelayClientException()
        {

        }

        public RelayClientException(string message) : base(message)
        {

        }

        public RelayClientException(string message, Exception inner) : base(message, inner)
        {

        }
    }
}
