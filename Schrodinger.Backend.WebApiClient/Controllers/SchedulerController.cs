using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Schrodinger.Backend.Abstractions.Interfaces.ApiKeys;
using Schrodinger.Backend.Abstractions.Interfaces.Images;
using Schrodinger.Backend.Abstractions.Types.AccountUsage;
using Schrodinger.Backend.Abstractions.Types.ApiKeys;
using WebApi.Models;

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
            var grain = _client.GetGrain<IAPIKeyGrain>("SchedulerGrain");
            var addApiKeysResponseDto = await grain.AddApiKeys(apiKeyEntries);
            
            if(addApiKeysResponseDto.IsSuccessful)
            {
                return new AddApiKeyResponseOk(addApiKeysResponseDto.ValidApiKeys, addApiKeysResponseDto.DuplicateApiKeys);
            }

            return new AddApiKeyResponseFailed(
                addApiKeysResponseDto.Error,
                addApiKeysResponseDto.DuplicateApiKeys
            );
        }
        catch (Exception ex)
        {
            return new AddApiKeyResponseFailed(ex.Message, []);
        }
    }
    
    [HttpPost("remove")]
    public async Task<RemoveApiKeyAPIResponse> RemoveApiKeys(List<ApiKeyDto> apiKeyDtos)
    {
        try
        {
            var apiKeys = apiKeyDtos.Select(dto => new ApiKey(dto.ApiKeyString, dto.ServiceProvider, dto.Url)).ToList();

            var grain = _client.GetGrain<IAPIKeyGrain>("SchedulerGrain");
            var removeApiKeys = await grain.RemoveApiKeys(apiKeys);
            
            var removedApiKeyDtos = removeApiKeys.Select(apiKey => new ApiKeyDto(apiKey)).ToList();
            return new RemoveApiKeyResponseOk(removedApiKeyDtos);
        }
        catch (Exception ex)
        {
            return new RemoveApiKeyResponseFailed(ex.Message);
        }
    }
    
    [HttpGet]
    public async Task<ActionResult<ApiKeyEntryDto[]>> GetAllApiKeys()
    {
        var grain = _client.GetGrain<IAPIKeyGrain>("SchedulerGrain");
        var apiAccountInfos = await grain.GetAllApiKeys();
        
        return Ok(apiAccountInfos.ToArray());
    }
    
    [HttpGet("apiKeysUsageInfo")]
    public async Task<ApiKeysUsageInfoResponse> GetApiKeysUsageInfo()
    {
        try
        {
            var grain = _client.GetGrain<IAPIKeyGrain>("SchedulerGrain");
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
            var grain = _client.GetGrain<IImageGenerationRequestManager>("SchedulerGrain");
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
            var grain = _client.GetGrain<IImageGenerationRequestManager>("SchedulerGrain");
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
            var grain = _client.GetGrain<IImageGenerationRequestManager>("SchedulerGrain");
            var states = await grain.GetImageGenerationStates();
            
            return new ImageGenerationStatesResponseOk<Dictionary<string, List<RequestAccountUsageInfoDto>>>(states);
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
            var grain = _client.GetGrain<IImageGenerationRequestManager>("SchedulerGrain");
            var blockedRequests = await grain.GetBlockedImageGenerationRequestsAsync();
            
            return new BlockedRequestResponseOk<List<BlockedRequestInfoDto>>(blockedRequests);
        }
        catch (Exception ex)
        {
            return new BlockedRequestResponseFailed(ex.Message);
        }
    }
}