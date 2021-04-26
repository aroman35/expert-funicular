namespace ExpertFunicular.Common.Exceptions
{
    public class FunicularPipeRouterException : FunicularException
    {
        public string PipeName { get; }
        public string Route { get; }

        public FunicularPipeRouterException(string pipeName, string route, string message = "unable to route the request") : base(message)
        {
            PipeName = pipeName;
            Route = route;
        }
    }
}