using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Backend;

public class Api : Controller
{

    [HttpPost]
    [Route("/api")]
    public IActionResult Get([FromBody]string regex)
    {
        try
        {
            if (regex == "")
            {
                return Ok(("s", "flowchart LR \n\ts((s))").ToTuple()); //TODO make graph
            }
            return Ok(Translator.Translate(regex).ToTuple());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return Ok((e.Message, "boom").ToTuple());
        }
    }

}