using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ExpertFunicular.Common.Exceptions;
using ExpertFunicular.Common.Messaging;

namespace ExpertFunicular.Server
{
    [PipeRoute("base")]
    public abstract class FunicularController
    {
        protected FunicularMessage RequestMessage { get; private set; }
        public FunicularMessage ResponseMessage { get; private set; }
        protected string Route;
        
        private IDictionary<string, MethodInfo> _actionMethodInputParameters;

        protected FunicularController()
        {
            ValidateControllerInitialState();
        }

        internal async Task HandlePipeRequest(FunicularMessage funicularMessage, string route)
        {
            RequestMessage = funicularMessage;
            Route = route;
            if (funicularMessage.MessageType == FunicularMessageType.Response)
            {
                ResponseMessage = new FunicularMessage
                {
                    MessageType = FunicularMessageType.Response,
                    ErrorMessage = "Expected request, but received response"
                };
                return;
                // throw ControllerException("Expected request, but received response");
            }

            if (!_actionMethodInputParameters.TryGetValue(Route, out var callingMethod))
            {
                ResponseMessage = new FunicularMessage
                {
                    MessageType = FunicularMessageType.Response,
                    ErrorMessage = $"Method \"{Route}\" is not declared"
                };
                return;
                // throw ControllerException($"Method \"{Route}\" is not declared");
            }
            
            var inputType = callingMethod
                .GetParameters()
                .FirstOrDefault(x => x.ParameterType != typeof(CancellationToken))?.ParameterType;
            
            if (RequestMessage.HasValue && inputType != null)
                await InvokeAsync(callingMethod, RequestMessage.GetPayload(inputType)).ConfigureAwait(false);
            else
                await InvokeAsync(callingMethod).ConfigureAwait(false);
        }
        
        private async Task InvokeAsync(MethodInfo callingMethod, params object[] parameters)
        {
            var isAwaitable = callingMethod.GetCustomAttributes<AsyncStateMachineAttribute>().Any();

            object result = null;
            bool isVoid;
            var returnType = callingMethod.ReturnType;
            
            if (isAwaitable)
            {
                isVoid = !callingMethod.ReturnType.IsGenericType;
                if (isVoid)
                {
                    await InvokeAsyncMethod(callingMethod, parameters).ConfigureAwait(false);
                }
                else
                {
                    returnType = callingMethod.ReturnType.GenericTypeArguments[0];
                    result = await InvokeAsyncMethodAndReturn(callingMethod, parameters).ConfigureAwait(false);
                }
            }
            else
            {
                isVoid = callingMethod.ReturnType == typeof(void);
                if (isVoid)
                    callingMethod.Invoke(this, parameters);
                else
                    result = callingMethod.Invoke(this, parameters);
            }

            if (!isVoid)
            {
                var entireResult = Convert.ChangeType(result, returnType);
                ResponseMessage = new FunicularMessage
                {
                    MessageType = FunicularMessageType.Response,
                    Route = Route
                };
                ResponseMessage.SetPayload(entireResult);
            }
        }
        
        private async Task<object> InvokeAsyncMethodAndReturn(MethodInfo @this, params object[] parameters)
        {
            var task = (Task)@this.Invoke(this, parameters);
            if (task == null)
                throw  new ArgumentNullException();
            
            await task.ConfigureAwait(false);
            var resultProperty = task.GetType().GetProperty("Result");
            return resultProperty?.GetValue(task);
        }

        private async Task InvokeAsyncMethod(MethodInfo @this, params object[] parameters)
        {
            var result = @this.Invoke(this, parameters);
            
            if (result is Task task)
                await task;
        }

        private void ValidateControllerInitialState()
        {
            var actionMethods = GetType()
                .GetMethods(BindingFlags.Public|BindingFlags.Instance)
                .Where(x => !string.IsNullOrEmpty(x.GetCustomAttribute<PipeRouteAttribute>()?.Route))
                .ToArray();

            if (actionMethods.Select(x =>
                x.GetParameters()
                    .Where(param => param.ParameterType != typeof(CancellationToken))
                    .Select(param => param.Name)
                    .ToArray())
                .Any(x => x.Length > 1))
            {
                throw ControllerException("Input arguments count must be 1");
            }

            _actionMethodInputParameters = actionMethods
                .ToDictionary(
                    x => x.GetCustomAttribute<PipeRouteAttribute>()!.Route,
                    x => x);
        }

        private FunicularPipeControllerException ControllerException(string message) => new(RequestMessage, message);
    }
}