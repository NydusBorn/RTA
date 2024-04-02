using Microsoft.AspNetCore.Mvc;

namespace Backend;

public class Api : Controller
{

    [HttpPost]
    [Route("/api")]
    public IActionResult Get([FromBody]string regex)
    {
        return Ok(Translator.Translate(regex));
    }

}