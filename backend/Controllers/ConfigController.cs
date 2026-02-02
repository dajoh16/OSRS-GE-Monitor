using Microsoft.AspNetCore.Mvc;
using OSRSGeMonitor.Api.Models;
using OSRSGeMonitor.Api.Models.Requests;
using OSRSGeMonitor.Api.Services;

namespace OSRSGeMonitor.Api.Controllers;

[ApiController]
[Route("api/config")]
public class ConfigController : ControllerBase
{
    private readonly InMemoryDataStore _dataStore;

    public ConfigController(InMemoryDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    [HttpGet]
    public ActionResult<GlobalConfig> GetConfig()
    {
        return Ok(_dataStore.Config);
    }

    [HttpPut]
    public ActionResult<GlobalConfig> UpdateConfig(UpdateConfigRequest request)
    {
        if (request.UserAgent is not null && string.IsNullOrWhiteSpace(request.UserAgent))
        {
            return BadRequest("User-Agent is required.");
        }

        var updated = _dataStore.UpdateConfig(request);
        return Ok(updated);
    }
}
