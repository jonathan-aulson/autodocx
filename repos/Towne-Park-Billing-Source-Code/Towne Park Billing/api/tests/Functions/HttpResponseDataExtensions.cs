using System.Text;
using Microsoft.Azure.Functions.Worker.Http;

namespace BackendTests.Functions
{
    public static class HttpResponseDataExtensions
    {
        public static void WriteString(this HttpResponseData response, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            response.Body.Write(bytes, 0, bytes.Length);
            response.Body.Seek(0, SeekOrigin.Begin);
        }
    }
}
