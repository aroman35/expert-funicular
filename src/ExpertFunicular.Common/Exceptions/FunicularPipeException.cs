using System;

namespace ExpertFunicular.Common.Exceptions
{
    public class FunicularPipeException : Exception
    {
        public string Reason { get; }
        public string Route { get; }
        
        public FunicularPipeException(string reason, string route) : base(reason)
        {
            Reason = reason;
            Route = route;
        }
    }
}