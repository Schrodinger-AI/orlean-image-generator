using Grains;
using Microsoft.AspNetCore.Mvc;
using ProofService.interfaces;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("proof")]
    public class ZkProofController : ControllerBase
    {
        private readonly IClusterClient _client;

        public ZkProofController(IClusterClient client)
        {
            _client = client;
        }

        [HttpPost("generate")]
        public async Task<ProofGenerationSchema.ProofGenerationResponse> Generate(ProofGenerationSchema.ProofGenerationRequest request)
        {
            var zkProofGrain = _client.GetGrain<IZkProofGrain>("zk-proof");
            var res = await zkProofGrain.Generate(request.Jwt, request.Salt);

            return new ProofGenerationSchema.ProofGenerationResponse
            {
                Proof = res
            };
        }
    }
}