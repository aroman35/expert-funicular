using ExpertFunicular.Common.Messaging;

namespace ExpertFunicular.Common.Exceptions
{
    public class FunicularPipeControllerException : FunicularException
    {
        public FunicularMessage RequestMessage { get; }

        public FunicularPipeControllerException(FunicularMessage requestMessage, string message) : base(message)
        {
            RequestMessage = requestMessage;
        }
    }
}