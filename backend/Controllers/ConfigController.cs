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
        var updated = _dataStore.UpdateConfig(request);
        return Ok(updated);
    }
}
