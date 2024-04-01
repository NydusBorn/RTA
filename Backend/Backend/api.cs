using Microsoft.AspNetCore.Mvc;

namespace Backend;

public class Api : Controller
{

    [HttpPost]
    [Route("/api")]
    public IActionResult Get()
    {
        return Ok("abc");
    }

}