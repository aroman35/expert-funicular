using System.Threading.Tasks;
using ExpertFunicular.Server;

namespace ExpertFunicular.IntegrationTests
{
    [PipeRoute("test")]
    public class TestController : FunicularController
    {
        [PipeRoute("receive-and-return")]
        public TestResponse ReceiveAndResponseAction(TestRequest request)
        {
            var response = new TestResponse();
            response.StringField = $"Received: {request.StringField} from pipe {RequestMessage.PipeName}";

            return response;
        }

        [PipeRoute("receive-and-return-async")]
        public async Task<TestResponse> ReceiveAndResponseActionAsync(TestRequest request)
        {
            var response = new TestResponse();
            response.StringField = $"Received: {request.StringField} from pipe {RequestMessage.PipeName}";
            await Task.Delay(1000);

            return response;
        }
    }
}