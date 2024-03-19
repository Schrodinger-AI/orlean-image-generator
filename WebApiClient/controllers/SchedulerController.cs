using System.Globalization;
using Grains.types;
using Grains.usage_tracker;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using WebApi.ApiKey.Models;
using ApiKeyEntryModel = WebApi.ApiKey.Models.ApiKeyEntry;
using ApiKeyUsageInfoDto = Shared.ApiKeyUsageInfoDto;
using SharedImageGenerationServiceProvider = Shared.ImageGenerationServiceProvider;
using ImageGenerationStatesResponse = WebApi.ApiKey.Models.ImageGenerationStatesResponse;
using ImageGenerationStatesResponseFailed = WebApi.ApiKey.Models.ImageGenerationStatesResponseFailed;
using IsOverloadedResponse = WebApi.ApiKey.Models.IsOverloadedResponse;
using IsOverloadedResponseFailed = WebApi.ApiKey.Models.IsOverloadedResponseFailed;
using IsOverloadedResponseOk = WebApi.ApiKey.Models.IsOverloadedResponseOk;
using RequestAccountUsageInfoDto = Shared.RequestAccountUsageInfoDto;

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
    public async Task<AddApiKeyAPIResponse> AddApiKeys(List<ApiKeyEntryModel> apiKeyEntries)
    {
        try
        {
            var apiAccountInfos = apiKeyEntries.Select(entry => new APIAccountInfo
            {
                ApiKey = new Shared.ApiKey
                {
                    ApiKeyString = entry.ApiKey.ApiKeyString,
                    ServiceProvider = GetServiceProvider(entry.ApiKey.ServiceProvider)
                },
                Email = entry.Email,
                Tier = entry.Tier,
                MaxQuota = entry.MaxQuota
            }).ToList();

            var grain = _client.GetGrain<ISchedulerGrain>("SchedulerGrain");
            var addedApiKeys = await grain.AddApiKeys(apiAccountInfos);
            var ret = addedApiKeys.Select(apiKey => new ApiKeyModel { ApiKeyString = apiKey.ApiKeyString.Substring(0, apiKey.ApiKeyString.Length / 2), ServiceProvider = apiKey.ServiceProvider.ToString() }).ToList();
            return new AddApiKeyResponseOk(ret);
        }
        catch (Exception ex)
        {
            return new AddApiKeyResponseFailed(ex.Message);
        }
    }
    
    [HttpPost("remove")]
    public async Task<RemoveApiKeyAPIResponse> RemoveApiKeys(List<ApiKeyModel> apiKeyModels)
    {
        try
        {
            var apiKeys = apiKeyModels.Select(model => new Shared.ApiKey
            {
                ApiKeyString = model.ApiKeyString,
                ServiceProvider = GetServiceProvider(model.ServiceProvider)
            }).ToList() ?? throw new ArgumentNullException("apiKeyDtos.Select(dto =>\n            {\n                return new ApiKey\n                {\n                    ApiKeyString = dto.ApiKeyString,\n                    ServiceProvider = GetServiceProvider(dto.ServiceProvider)\n                };\n            }).ToList()");

            var grain = _client.GetGrain<ISchedulerGrain>("SchedulerGrain");
            var addedApiKeys = await grain.RemoveApiKeys(apiKeys);
            
            apiKeyModels.Clear();
            apiKeyModels.AddRange(addedApiKeys.Select(apiKey => new ApiKeyModel
            {
                ApiKeyString = apiKey.ApiKeyString,
                ServiceProvider = apiKey.ServiceProvider.ToString()
            }));
            return new RemoveApiKeyResponseOk(apiKeyModels);
        }
        catch (Exception ex)
        {
            return new RemoveApiKeyResponseFailed(ex.Message);
        }
    }
    
    [HttpGet]
    public async Task<ActionResult<ApiKeyEntryModel[]>> GetAllApiKeys()
    {
        var grain = _client.GetGrain<ISchedulerGrain>("SchedulerGrain");
        var apiAccountInfos = await grain.GetAllApiKeys();
        
        //hack for when we do not have user protection
        var ret = new List<ApiKeyEntryModel>();
        foreach (var info in apiAccountInfos)
        {
            var newInfo = new ApiKeyEntryModel
            {
                ApiKey = new ApiKeyModel
                {
                    ApiKeyString = info.ApiKey.ApiKeyString.Substring(0, info.ApiKey.ApiKeyString.Length/2),
                    ServiceProvider = info.ApiKey.ServiceProvider.ToString()
                },
                Email = info.Email,
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
    
    private static SharedImageGenerationServiceProvider GetServiceProvider(string serviceProvider)
    {
        return serviceProvider switch
        {
            "DalleOpenAI" => SharedImageGenerationServiceProvider.DalleOpenAI,
            "AzureOpenAI" => SharedImageGenerationServiceProvider.AzureOpenAI,
            _ => throw new ArgumentOutOfRangeException(nameof(serviceProvider), serviceProvider, null)
        };
    }
}