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

    private readonly ILogger<SchedulerController> _logger;


    public SchedulerController(IClusterClient client, ILogger<SchedulerController> logger)
    {
        _client = client;
        _logger = logger;
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
                ApiKey = info.ApiKey,//.Substring(0, info.ApiKey.Length/2),
                Email = info.Email,
                Description = info.Description,
                MaxQuota = info.MaxQuota,
                Tier = info.Tier
            };
            ret.Add(newInfo);
        }
        
        return Ok(ret.ToArray());
    }
    
    [HttpGet("apiKeysUsageInfo")]
    public async Task<ApiKeysUsageInfoResponse> GetApiKeysUsageInfo()
    {
        try
        {
            var grain = _client.GetGrain<ISchedulerGrain>("SchedulerGrain");
            var usageInfo = await grain.GetApiKeysUsageInfo();
            return new ApiKeysUsageInfoResponseOk<Dictionary<string, ApiKeyUsageInfo>>(usageInfo);
        }
        catch (Exception ex)
        {
            return new ApiKeysUsageInfoResponseFailed(ex.Message);
        }
    }
    
    [HttpGet("states")]
    public async Task<ImageGenerationStatesResponse> GetImageGenerationStates()
    {
        try
        {
            var grain = _client.GetGrain<ISchedulerGrain>("SchedulerGrain");
            var states = await grain.GetImageGenerationStates();
            var startedImageGenerationRequests = new List<RequestAccountUsageInfo>(states.StartedImageGenerationRequests.Values);
            startedImageGenerationRequests.ForEach(item => item.ApiKey = "");
            var pendingImageGenerationRequests = new List<RequestAccountUsageInfo>(states.PendingImageGenerationRequests.Values);
            pendingImageGenerationRequests.ForEach(item => item.ApiKey = "");
            var completedImageGenerationRequests = new List<RequestAccountUsageInfo>(states.CompletedImageGenerationRequests.Values);
            completedImageGenerationRequests.ForEach(item => item.ApiKey = "");
            var failedImageGenerationRequests = new List<RequestAccountUsageInfo>(states.FailedImageGenerationRequests.Values);
            failedImageGenerationRequests.ForEach(item => item.ApiKey = "");
            var imageGenerationRequests = new Dictionary<string, List<RequestAccountUsageInfo>>()
            {
                {"startedRequests", startedImageGenerationRequests},
                {"pendingRequests", pendingImageGenerationRequests},
                {"completedRequests", completedImageGenerationRequests},
                {"failedRequests", failedImageGenerationRequests}
            };
            return new ImageGenerationStatesResponseOk<Dictionary<string, List<RequestAccountUsageInfo>>>(imageGenerationRequests);
        }
        catch (Exception ex)
        {
            return new ImageGenerationStatesResponseFailed(ex.Message);
        }
    }
}