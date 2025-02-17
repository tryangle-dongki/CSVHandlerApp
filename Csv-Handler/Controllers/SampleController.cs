using Microsoft.AspNetCore.Mvc;

namespace csv_handler.Controllers;

[ApiController]
[Route("api/sample")]
public class SampleController : Controller
{
    [HttpGet("hello")]
    public string GetHello()
    {
        return "Hello, REST API!";
    }
}