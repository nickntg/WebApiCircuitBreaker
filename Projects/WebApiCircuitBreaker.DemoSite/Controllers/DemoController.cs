using System.Web.Http;

namespace WebApiCircuitBreaker.DemoSite.Controllers
{
    public class DemoController : ApiController
    {
        [HttpGet]
        public IHttpActionResult Get(string text)
        {
            return Ok(text);
        }
    }
}