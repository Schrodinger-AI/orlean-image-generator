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
    public async Task<AddApiKeyAPIResponse> AddApiKeys(List<ApiKeyEntryDto> apiKeyEntries)
    {
        try
        {
            var grain = _client.GetGrain<ISchedulerGrain>("SchedulerGrain");
            var addedApiKeys = await grain.AddApiKeys(apiKeyEntries);
            var ret = addedApiKeys.Select(apiKey => new ApiKeyDto { ApiKeyString = apiKey.ApiKeyString.Substring(0, apiKey.ApiKeyString.Length / 2), ServiceProvider = apiKey.ServiceProvider.ToString(), Url = apiKey.Url}).ToList();
            return new AddApiKeyResponseOk(ret);
        }
        catch (Exception ex)
        {
            return new AddApiKeyResponseFailed(ex.Message);
        }
    }
    
    [HttpPost("remove")]
    public async Task<RemoveApiKeyAPIResponse> RemoveApiKeys(List<ApiKeyDto> apiKeyDtos)
    {
        try
        {
            var apiKeys = apiKeyDtos.Select(dto => new ApiKey(dto.ApiKeyString, dto.ServiceProvider, dto.Url)).ToList();

            var grain = _client.GetGrain<ISchedulerGrain>("SchedulerGrain");
            var addedApiKeys = await grain.RemoveApiKeys(apiKeys);
            
            apiKeyDtos.Clear();
            apiKeyDtos.AddRange(addedApiKeys.Select(apiKey => new ApiKeyDto
            {
                ApiKeyString = apiKey.ApiKeyString,
                ServiceProvider = apiKey.ServiceProvider.ToString()
            }));
            return new RemoveApiKeyResponseOk(apiKeyDtos);
        }
        catch (Exception ex)
        {
            return new RemoveApiKeyResponseFailed(ex.Message);
        }
    }
    
    [HttpGet]
    public async Task<ActionResult<ApiKeyEntryDto[]>> GetAllApiKeys()
    {
        var grain = _client.GetGrain<ISchedulerGrain>("SchedulerGrain");
        var apiAccountInfos = await grain.GetAllApiKeys();
        
        return Ok(apiAccountInfos.ToArray());
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
                ApiKey = i.Value.ApiKey,
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
    
    [HttpGet("blocked")]
    public async Task<BlockedRequestResponse> GetBlockedRequests()
    {
        try
        {
            var grain = _client.GetGrain<ISchedulerGrain>("SchedulerGrain");
            var blockedRequests = await grain.GetBlockedImageGenerationRequestsAsync();
            
            var requestList = new List<BlockedRequestInfo>(blockedRequests.Values);
            requestList.ForEach(item => item.RequestAccountUsageInfo.ApiKey.ApiKeyString = item.RequestAccountUsageInfo.ApiKey.ApiKeyString[..(item.RequestAccountUsageInfo.ApiKey.ApiKeyString.Length/2)]);
            var result = requestList.Select(i => new BlockedRequestInfoDto
            {
                BlockedReason = i.BlockedReason?.ToString(),
                RequestInfo = new RequestAccountUsageInfoDto
                {
                    RequestId = i.RequestAccountUsageInfo.RequestId,
                    RequestTimestamp = UnixTimeStampInSecondsToDateTime(i.RequestAccountUsageInfo.RequestTimestamp).ToString(CultureInfo.InvariantCulture),
                    StartedTimestamp = UnixTimeStampInSecondsToDateTime(i.RequestAccountUsageInfo.StartedTimestamp).ToString(CultureInfo.InvariantCulture),
                    FailedTimestamp = UnixTimeStampInSecondsToDateTime(i.RequestAccountUsageInfo.FailedTimestamp).ToString(CultureInfo.InvariantCulture),
                    CompletedTimestamp = UnixTimeStampInSecondsToDateTime(i.RequestAccountUsageInfo.CompletedTimestamp).ToString(CultureInfo.InvariantCulture),
                    Attempts = i.RequestAccountUsageInfo.Attempts,
                    ApiKey = i.RequestAccountUsageInfo.ApiKey,
                    ChildId = i.RequestAccountUsageInfo.ChildId,
                }
            });
            
            return new BlockedRequestResponseOk<IEnumerable<BlockedRequestInfoDto>>(result);
        }
        catch (Exception ex)
        {
            return new BlockedRequestResponseFailed(ex.Message);
        }
    }

    private static IEnumerable<RequestAccountUsageInfoDto> GetRequestAccountUsageInfoDtoList(Dictionary<string, RequestAccountUsageInfo> requests)
    {
        var imageGenerationRequests = new List<RequestAccountUsageInfo>(requests.Values);
        imageGenerationRequests.ForEach(item =>
        {
            if (item.ApiKey != null)
                item.ApiKey.ApiKeyString = item.ApiKey.ApiKeyString[..(item.ApiKey.ApiKeyString.Length / 2)];
        });
        var ret = imageGenerationRequests.Select(i => new RequestAccountUsageInfoDto
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
}