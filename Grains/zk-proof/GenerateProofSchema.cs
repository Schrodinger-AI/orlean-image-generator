using System.Text.Json.Serialization;

namespace ProofService.interfaces;

[GenerateSerializer]
    public class ProofGenerationTestRequest
    {
        [JsonPropertyName("jwt")]
        public List<string> Jwt { get; set; }
        
        [JsonPropertyName("signature")]
        public List<string> Signature { get; set; }
        
        [JsonPropertyName("pubkey")]
        public List<string> Pubkey { get; set; }
        
        [JsonPropertyName("salt")]
        public List<string> Salt { get; set; }
    }
    
[GenerateSerializer]
    public class ProofGenerationRequest
    {
        [JsonPropertyName("jwt")]
        public string Jwt { get; set; }
        
        [JsonPropertyName("salt")]
        public string Salt { get; set; }
    }
    
[GenerateSerializer]
    public class ProofGenerationResponse
    {
        [Id(0)]
        [JsonPropertyName("proof")]
        public string Proof { get; set; }
        
        [Id(1)]
        [JsonPropertyName("identifierHash")]
        public string IdentifierHash { get; set; }
        
        [Id(2)]
        [JsonPropertyName("publicKey")]
        public string PublicKey { get; set; }
    }
    
[GenerateSerializer]
    public class InitializeRequest
    {
        [JsonPropertyName("ip")]
        public string Ip { get; set; }

        [JsonPropertyName("publicKey")]
        public string PublicKey { get; set; }
        
        // [JsonPropertyName("endpoint")]
        // public string Endpoint { get; set; }
        //
        // [JsonPropertyName("contractAddress")]
        // public string ContractAddress { get; set; }
        //
        // [JsonPropertyName("walletAddress")]
        // public string WalletAddress { get; set; }
        //
        // [JsonPropertyName("pk")]
        // public string Pk { get; set; }
        //
        // [JsonPropertyName("vk")]
        // public string Vk { get; set; }
    }
    
    public class CreateTestRequest
    {
        [JsonPropertyName("guardianApproved")]
        public GuardianApproved GuardianApproved { get; set; }
        
        [JsonPropertyName("managerInfo")]
        public ManagerInfoReq ManagerInfo { get; set; }
    }
    
[GenerateSerializer]
    public class GuardianApproved
    {
        [JsonPropertyName("endpoint")]
        public string Endpoint { get; set; }
        
        [JsonPropertyName("contractAddress")]
        public string ContractAddress { get; set; }
    }
    
[GenerateSerializer]
    public class ZkGuardianInfoReq
    {
        [JsonPropertyName("endpoint")]
        public string Endpoint { get; set; }
        
        [JsonPropertyName("contractAddress")]
        public string ContractAddress { get; set; }
    }
    
[GenerateSerializer]
    public class ManagerInfoReq
    {
        [JsonPropertyName("endpoint")]
        public string Endpoint { get; set; }
        
        [JsonPropertyName("contractAddress")]
        public string ContractAddress { get; set; }
    }
    
[GenerateSerializer]
    public class CallTestRequest
    {
        [JsonPropertyName("endpoint")]
        public string Endpoint { get; set; }
        
        [JsonPropertyName("caHash")]
        public string CaHash { get; set; }
        
        [JsonPropertyName("toAddress")]
        public string ToAddress { get; set; }
        
        [JsonPropertyName("tokenContractAddress")]
        public string TokenContractAddress { get; set; }
        
        [JsonPropertyName("caContractAddress")]
        public string CaContractAddress { get; set; }
        
        [JsonPropertyName("pk")]
        public string Pk { get; set; }
        
        [JsonPropertyName("amount")]
        public int Amount { get; set; }
    }
    
[GenerateSerializer]
    public class ProofLoginInRequest
    {
        [JsonPropertyName("proof")]
        public string Proof { get; set; }
        
        [JsonPropertyName("identifierHash")]
        public string IdentifierHash { get; set; }
        
        [JsonPropertyName("publicKey")]
        public string PublicKey { get; set; }
        
        [JsonPropertyName("managerAddress")]
        public string ManagerAddress { get; set; }
        
        [JsonPropertyName("salt")]
        public string Salt { get; set; }
    }
    
[GenerateSerializer]
    public class ProofLoginInResponse
    {
        [Id(0)]
        [JsonPropertyName("caCash")]
        public string CaCash { get; set; }
        
        [Id(1)]
        [JsonPropertyName("caAddress")]
        public string CaAddress { get; set; }
    }
    
