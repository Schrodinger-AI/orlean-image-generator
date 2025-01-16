using System.Globalization;
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
    
    [HttpGet("apiKeysUsageInfo")]
    public async Task<ApiKeysUsageInfoResponse> GetApiKeysUsageInfo()
    {
        try
        {
            var grain = _client.GetGrain<ISchedulerGrain>("SchedulerGrain");
            var usageInfo = await grain.GetApiKeysUsageInfo();

            var apiKeyUsageInfoDtos = usageInfo.Select(i => new ApiKeyUsageInfoDto
            {
                ApiKey = i.Key,
                ReactivationTimestamp = UnixTimeStampInSecondsToDateTime(i.Value.GetReactivationTimestamp()).ToString(CultureInfo.InvariantCulture),
                Status = i.Value.Status.ToString(),
                ErrorCode = i.Value.ErrorCode?.ToString()
            });

            return new ApiKeysUsageInfoResponseOk<IEnumerable<ApiKeyUsageInfoDto>>(apiKeyUsageInfoDtos);
        }
        catch (Exception ex)
        {
            return new ApiKeysUsageInfoResponseFailed(ex.Message);
        }
    }
    
    private static DateTime UnixTimeStampInSecondsToDateTime( long unixTimeStamp )
    {
        var dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(unixTimeStamp);
        var dateTime = new DateTime(dateTimeOffset.Ticks, DateTimeKind.Utc);
        return dateTime;
    }
    
    [HttpGet("isOverloaded")]
    public async Task<IsOverloadedResponse> IsOverloaded()
    {
        try
        {
            var grain = _client.GetGrain<ISchedulerGrain>("SchedulerGrain");
            var isOverloaded = await grain.IsOverloaded();
            return new IsOverloadedResponseOk(isOverloaded);
        }
        catch (Exception ex)
        {
            return new IsOverloadedResponseFailed(ex.Message);
        }
    }
    
    [HttpGet("states")]
    public async Task<ImageGenerationStatesResponse> GetImageGenerationStates()
    {
        try
        {
            var grain = _client.GetGrain<ISchedulerGrain>("SchedulerGrain");
            var states = await grain.GetImageGenerationStates();
            
            var startedImageGenerationRequests = GetRequestAccountUsageInfoDtoList(states.StartedImageGenerationRequests);
            var pendingImageGenerationRequests = GetRequestAccountUsageInfoDtoList(states.PendingImageGenerationRequests);
            var completedImageGenerationRequests = GetRequestAccountUsageInfoDtoList(states.CompletedImageGenerationRequests);
            var failedImageGenerationRequests = GetRequestAccountUsageInfoDtoList(states.FailedImageGenerationRequests);
            
            var imageGenerationRequests = new Dictionary<string, IEnumerable<RequestAccountUsageInfoDto>>()
            {
                {"startedRequests", startedImageGenerationRequests},
                {"pendingRequests", pendingImageGenerationRequests},
                {"completedRequests", completedImageGenerationRequests},
                {"failedRequests", failedImageGenerationRequests}
            };
            return new ImageGenerationStatesResponseOk<Dictionary<string, IEnumerable<RequestAccountUsageInfoDto>>>(imageGenerationRequests);
        }
        catch (Exception ex)
        {
            return new ImageGenerationStatesResponseFailed(ex.Message);
        }
    }

    [HttpGet("forceRequestExecution")]
    public async Task<ForceRequestExecutionResponse> ForceRequestExecution(string childId)
    {
        try
        {
            var grain = _client.GetGrain<ISchedulerGrain>("SchedulerGrain");
            var success = await grain.ForceRequestExecution(childId);
            return new ForceRequestExecutionResponseOk(success);
        }
        catch (Exception ex)
        {
            return new ForceRequestExecutionResponseFailed(ex.Message);
        }
    }

    private static IEnumerable<RequestAccountUsageInfoDto> GetRequestAccountUsageInfoDtoList(Dictionary<string, RequestAccountUsageInfo> requests)
    {
        var failedImageGenerationRequests = new List<RequestAccountUsageInfo>(requests.Values);
        failedImageGenerationRequests.ForEach(item => item.ApiKey = item.ApiKey[..(item.ApiKey.Length/2)]);
        var ret = failedImageGenerationRequests.Select(i => new RequestAccountUsageInfoDto
        {
            RequestId = i.RequestId,
            RequestTimestamp = UnixTimeStampInSecondsToDateTime(i.RequestTimestamp).ToString(CultureInfo.InvariantCulture),
            StartedTimestamp = UnixTimeStampInSecondsToDateTime(i.StartedTimestamp).ToString(CultureInfo.InvariantCulture),
            FailedTimestamp = UnixTimeStampInSecondsToDateTime(i.FailedTimestamp).ToString(CultureInfo.InvariantCulture),
            CompletedTimestamp = UnixTimeStampInSecondsToDateTime(i.CompletedTimestamp).ToString(CultureInfo.InvariantCulture),
            Attempts = i.Attempts,
            ApiKey = i.ApiKey,
            ChildId = i.ChildId
        });
        return ret;
    }

    [HttpPost("clear-failed")]
    public async Task ClearFailedList()
    {
        var grain = _client.GetGrain<ISchedulerGrain>("SchedulerGrain"); 
        await grain.ClearFailedRequest();
    }
    
    [HttpPost("clear-pending")]
    public async Task RemovePending()
    {
        var grain = _client.GetGrain<ISchedulerGrain>("SchedulerGrain"); 
        await grain.ClearPendingRequest();
    }
}