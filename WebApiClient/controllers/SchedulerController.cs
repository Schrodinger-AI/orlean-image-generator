using Grains.types;
using Grains.usage_tracker;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using Shared;

namespace WebApi.Controllers;

[ApiController]
[Route("scheduler")]
public class SchedulerController : ControllerBase
{
    private readonly IClusterClient _client;

    public SchedulerController(IClusterClient client)
    {
        _client = client;
    }

    [HttpPost("add")]
    public async Task<AddApiKeyAPIResponse> AddApiKeys(List<ApiKeyEntry> apiKeyEntries)
    {
        try
        {
            var grain = _client.GetGrain<ISchedulerGrain>("SchedulerGrain");
            var addedApiKeys = await grain.AddApiKeys(apiKeyEntries);
            return new AddApiKeyResponseOk(addedApiKeys);
        }
        catch (Exception ex)
        {
            return new AddApiKeyResponseFailed(ex.Message);
        }
    }
    
    [HttpPost("remove")]
    public async Task<RemoveApiKeyAPIResponse> RemoveApiKeys(List<string> apiKeys)
    {
        try
        {
            var grain = _client.GetGrain<ISchedulerGrain>("SchedulerGrain");
            var addedApiKeys = await grain.RemoveApiKeys(apiKeys);
            return new RemoveApiKeyResponseOk(addedApiKeys);
        }
        catch (Exception ex)
        {
            return new RemoveApiKeyResponseFailed(ex.Message);
        }
    }
    
    [HttpGet]
    public async Task<ActionResult<APIAccountInfo[]>> GetAllApiKeys()
    {
        var grain = _client.GetGrain<ISchedulerGrain>("SchedulerGrain");
        var apiAccountInfos = await grain.GetAllApiKeys();
        
        //hack for when we do not have user protection
        var ret = new List<APIAccountInfo>();
        foreach (var info in apiAccountInfos)
        {
            var newInfo = new APIAccountInfo
            {
                ApiKey = info.ApiKey.Substring(0, info.ApiKey.Length/2),
                Email = info.Email,
                Description = info.Description,
                MaxQuota = info.MaxQuota,
                Tier = info.Tier
            };
            ret.Add(newInfo);
        }
        
        return Ok(ret.ToArray());
    }
}