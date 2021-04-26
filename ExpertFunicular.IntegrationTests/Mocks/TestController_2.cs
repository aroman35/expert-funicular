using System.Linq;
using ExpertFunicular.Server;

namespace ExpertFunicular.IntegrationTests.Mocks
{
    [PipeRoute("test-2")]
    public class TestController_2 : FunicularController
    {
        public static TestRequest RequestAccessor;

        [PipeRoute("receive")]
        public void ReceiveAction(TestRequest request)
        {
            RequestAccessor = request;
        }

        [PipeRoute("receive-large-string")]
        public TestResponse HandleLargeString(TestRequest request)
        {
            var response = new TestResponse
            {
                StringField = request.StringField
            };
            return response;
        }

        [PipeRoute("get")]
        public TestResponse GetLite()
        {
            var strValue = new string(Enumerable.Repeat('A', 1 * 0x00100000).ToArray()); // 1Mb
            var response = new TestResponse
            {
                StringField = strValue
            };
            return response;
        }
        
        [PipeRoute("get-huge")]
        public TestResponse GetHuge()
        {
            var strValue = new string(Enumerable.Repeat('A', 1 * 0x20000000).ToArray()); // 0.5Gb
            var response = new TestResponse
            {
                StringField = strValue
            };
            return response;
        }
    }
}