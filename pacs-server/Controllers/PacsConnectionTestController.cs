using Microsoft.AspNetCore.Mvc;
using pacs_server.Logic;
using System;

namespace pacs_server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PacsConnectionTestController : ControllerBase
    {
        [HttpGet("[action]")]
        public String TestConnection()
        {
            String result = String.Empty;
            result += PacsConnectionTest.Echo();

            result += PacsConnectionTest.Store("63771021"); // plik o tej nazwie jest w /bin/Debug
            result += PacsConnectionTest.Find();
            result += PacsConnectionTest.Move();
            return result;
        }
    }
}