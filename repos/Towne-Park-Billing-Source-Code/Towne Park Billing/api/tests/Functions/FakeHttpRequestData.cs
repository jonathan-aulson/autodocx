using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace BackendTests.Functions;

public class FakeHttpRequestData : HttpRequestData
{
    private readonly Stream _body;
    private readonly HttpHeadersCollection _headers = new HttpHeadersCollection();
    private readonly FunctionContext _context;
    public string MethodValue { get; set; }

    public FakeHttpRequestData(FunctionContext context, string method, string body = null) : base(context)
    {
        _context = context;
        MethodValue = method;
        _body = body != null ? new MemoryStream(System.Text.Encoding.UTF8.GetBytes(body)) : new MemoryStream();
    }

    // Legacy constructor for other tests: (context, url, body)
    public FakeHttpRequestData(FunctionContext context, System.Uri url, Stream body) : base(context)
    {
        _context = context;
        MethodValue = "GET";
        _body = body ?? new MemoryStream();
        _url = url;
    }

    private System.Uri _url = new System.Uri("http://localhost");
    public override System.Uri Url => _url;

    public override Stream Body => _body;
    public override HttpHeadersCollection Headers => _headers;
    public override string Method => MethodValue;
    public override IReadOnlyCollection<Microsoft.Azure.Functions.Worker.Http.IHttpCookie> Cookies => Array.Empty<Microsoft.Azure.Functions.Worker.Http.IHttpCookie>();
    public override IEnumerable<System.Security.Claims.ClaimsIdentity> Identities => Array.Empty<System.Security.Claims.ClaimsIdentity>();
    public override HttpResponseData CreateResponse()
    {
        return new FakeHttpResponseData(_context);
    }
    public HttpResponseData CreateResponse(HttpStatusCode statusCode)
    {
        var resp = new FakeHttpResponseData(_context);
        resp.StatusCode = statusCode;
        return resp;
    }
}
