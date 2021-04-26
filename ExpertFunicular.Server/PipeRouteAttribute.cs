using System;

namespace ExpertFunicular.Server
{
    public class PipeRouteAttribute : Attribute
    {
        private readonly string _route;

        public PipeRouteAttribute(string route)
        {
            _route = route;
        }

        public string Route => _route.ToLowerInvariant();
    }
}