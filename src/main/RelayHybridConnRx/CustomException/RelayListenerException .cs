using System;


namespace RelayHybridConnRx.CustomException
{
    public class RelayListenerException : Exception
    {
        public RelayListenerException()
        {

        }

        public RelayListenerException(string message) : base(message)
        {

        }

        public RelayListenerException(string message, Exception inner) : base(message, inner)
        {

        }
    }
}
