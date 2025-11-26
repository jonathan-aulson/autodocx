using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace BackendTests.Functions;

[ExcludeFromCodeCoverage]
public class FakeHttpResponseData : HttpResponseData
{
    private Stream _body;

    public FakeHttpResponseData(FunctionContext functionContext) : base(functionContext)
    {
        _body = new MemoryStream();
        Cookies = null!;
    }

    public override HttpStatusCode StatusCode { get; set; }
    public override HttpHeadersCollection Headers { get; set; } = new HttpHeadersCollection();
    public override Stream Body 
    { 
        get => _body; 
        set => _body = value; 
    }
    public override HttpCookies Cookies { get; }

    public async Task<string> ReadAsStringAsync()
    {
        if (_body is null)
            return string.Empty;
        _body.Position = 0;
        using var reader = new StreamReader(_body, Encoding.UTF8, true, 1024, true);
        return await reader.ReadToEndAsync();
    }
}
