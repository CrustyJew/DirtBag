using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DirtbagWebservice.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class SentinelBanController : Controller {

        private BLL.ISentinelChannelBanBLL bll;
        public SentinelBanController(BLL.ISentinelChannelBanBLL sentinelChannelBanBLL ) {
            bll = sentinelChannelBanBLL;
        }
    
        [HttpGet("{sub}/{thingid}")]
        public Task<IEnumerable<Models.SentinelChannelBan>> CheckBanList([FromRoute]string sub, [FromRoute] string thingid ) {
            if(!User.IsInRole(sub.ToLower())) throw new UnauthorizedAccessException("Not a moderator of that sub");
            return bll.CheckSentinelChannelBan(sub, thingid);
        }
    }
}
