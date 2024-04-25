using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Groth16.Net;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orleans.Runtime;
using ProofService.interfaces;
using RestSharp;

namespace Grains;

public class ZkProofGrain : Grain, IZkProofGrain
{
    private readonly IPersistentState<ZkProofState> _zkProofState;
    private readonly ILogger<ZkProofGrain> _logger;
    private readonly ZkProverSetting _proverSetting;
    private Prover _prover;
    
    public ZkProofGrain(
        [PersistentState("zkProofState", "MySqlSchrodingerImageStore")]
        IPersistentState<ZkProofState> zkProofState,
        IOptions<ZkProverSetting> proverSetting,
        ILogger<ZkProofGrain> logger)
    {
        _logger = logger;
        _zkProofState = zkProofState;
        _proverSetting = proverSetting.Value;
    }
    
    public async Task<string> Generate(string jwt, string salt)
    {
        // string jwtStr = "eyJhbGciOiJSUzI1NiIsImtpZCI6IjZjZTExYWVjZjllYjE0MDI0YTQ0YmJmZDFiY2Y4YjMyYTEyMjg3ZmEiLCJ0eXAiOiJKV1QifQ.eyJpc3MiOiJodHRwczovL2FjY291bnRzLmdvb2dsZS5jb20iLCJhenAiOiI2ODc0MzA1NDA3OTItMjJyc29xOGxvbWxzcDEwMHF2MnBkdjBwYmExN2RxNHMuYXBwcy5nb29nbGV1c2VyY29udGVudC5jb20iLCJhdWQiOiI2ODc0MzA1NDA3OTItMjJyc29xOGxvbWxzcDEwMHF2MnBkdjBwYmExN2RxNHMuYXBwcy5nb29nbGV1c2VyY29udGVudC5jb20iLCJzdWIiOiIxMTIwNzIzNzQxMjQ4MTY5Nzc0MzQiLCJlbWFpbCI6InlpbWVuZy5sdUBhZWxmLmlvIiwiZW1haWxfdmVyaWZpZWQiOnRydWUsImF0X2hhc2giOiJFUzAxX2oyQmVRb1RWYjl6YUIxSVhRIiwibmFtZSI6ImpsIGJyIiwicGljdHVyZSI6Imh0dHBzOi8vbGgzLmdvb2dsZXVzZXJjb250ZW50LmNvbS9hL0FDZzhvY0pWcmtucUJQUmtFZWJQVkEzd1FnNUxseWYwREtMRV9lTkZiRzdKVEZQS0oxSWM5dz1zOTYtYyIsImdpdmVuX25hbWUiOiJqbCIsImZhbWlseV9uYW1lIjoiYnIiLCJpYXQiOjE3MTM3NjQzNjIsImV4cCI6MTcxMzc2Nzk2Mn0.BaQtj8S0IwQUxMVTGyza9HoGJv4LgKYKLPByeKN4EuTHPsM_6cGKJLVliUcyOK7Cmo1Bse5m_KP7GTeX0E3RNqHbFn0FXHh01TRFKWQFwUTjkXYbNy1nK0YgT6uy0D0c35NnsrO6UnYgqQlH5lFZvYCkHfsvwnB3PYLKmIJRu_HFSZbzNmtacapkmkSGDf9FMe0dEHscwIQ61Vq6D9z4A9k1PxlfJeOqBwom-HJx-p9eHgsiY-4AwsrRvqwaq_wCzO1JObZsx2-_3mVQv7u3-8voU5U9eP1hOE4ntM6XBpoQ6LIgWzI658VYo8OQy3pZjvnXYRYiIv_pcsQt_ae8Zw";
        // string saltStr = "a677999396dc49a28ad6c9c242719bb3";
        // string wasmPath = "/Users/jasonlu/Downloads/ZkFile/guardianhash.wasm";
        // string r1csPath = "/Users/jasonlu/Downloads/ZkFile/guardianhash.r1cs";
        // string zkeyPath = "/Users/jasonlu/Downloads/ZkFile/guardianhash_0001.zkey";

        if (!_zkProofState.State.isPoverLoad)
        {
            _prover = Prover.Create(_proverSetting.WasmPath, _proverSetting.R1csPath, _proverSetting.ZkeyPath);
            _zkProofState.State.isPoverLoad = true;
        }

        var res = await Generate(jwt, salt, _prover);
        return res;
    }

    public async Task<string> Generate(string jwtStr, string saltStr, Prover _prover)
    {
        try
        {
            var jwtHeader = jwtStr.Split(".")[0];
            var jwtPayload = jwtStr.Split(".")[1];
            var jwtSignature = jwtStr.Split(".")[2];

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(jwtStr);

            // get google public key from jwt
            var publicKey = await GetGooglePublicKeyFromJwt(jwt);
            // google public key to hex
            var publicKeyHex = BitConverter
                .ToString(WebEncoders.Base64UrlDecode(publicKey)).Replace("-", "")
                .ToLower();
            // google public key hex to chunked bytes
            var pubkey = Helpers.HexToChunkedBytes(publicKeyHex, 121, 17)
                .Select(s => s.HexToBigInt()).ToList();
            // jwt signature to hex
            var signatureHex = BitConverter
                .ToString(WebEncoders.Base64UrlDecode(jwtSignature)).Replace("-", "")
                .ToLower();
            // jwt signature hex to chunked bytes
            var signature = Helpers.HexToChunkedBytes(signatureHex, 121, 17)
                .Select(s => s.HexToBigInt()).ToList();
            // salt hex to chunked bytes
            var salt = HexStringToByteArray(saltStr).Select(b => b.ToString()).ToList();

            var payloadStartIndex = jwtStr.IndexOf(".") + 1;
            var subClaim = PadString("\"sub\":" + "\"" + jwt.Payload.Sub + "\"" + ",", 41);
            var subClaimLength = jwt.Payload.Sub.Length + 9;
            var jsonString = ParseJwtPayload(jwtPayload);
            // the start index of field sub
            var startIndex = jsonString.IndexOf("\"sub\"");
            // the start index of field sub value
            var valueStartIndex = jsonString.IndexOf('"', startIndex + 5) + 1;
            // the end index of field sub value
            var valueEndIndex = jsonString.IndexOf('"', valueStartIndex);
            var subIndexB64 = payloadStartIndex + startIndex * 4 / 3;
            var subLengthB64 = (valueEndIndex + 2 - (startIndex - 1)) * 4 / 3;
            var subNameLength = 5;
            var subColonIndex = 5;
            var subValueIndex = 6;
            var subValueLength = 23;

            // build parameters of ProveBn254
            IDictionary<string, IList<string>> provingInput = new Dictionary<string, IList<string>>();
            provingInput["jwt"] = PadString(jwtHeader + "." + jwtPayload, 2048);
            provingInput["signature"] = signature;
            provingInput["pubkey"] = pubkey;
            provingInput["salt"] = salt;
            provingInput["payload_start_index"] = new List<string> {payloadStartIndex.ToString()};
            provingInput["sub_claim"] = subClaim;
            provingInput["sub_claim_length"] = new List<string> {subClaimLength.ToString()};
            provingInput["sub_index_b64"] = new List<string> {subIndexB64.ToString()};
            provingInput["sub_length_b64"] = new List<string> {subLengthB64.ToString()};
            provingInput["sub_name_length"] = new List<string> {subNameLength.ToString()};
            provingInput["sub_colon_index"] = new List<string> {subColonIndex.ToString()};
            provingInput["sub_value_index"] = new List<string> {subValueIndex.ToString()};
            provingInput["sub_value_length"] = new List<string> {subValueLength.ToString()};
            
            // exec ProveBn254
            var provingOutputString = _prover.ProveBn254(provingInput);
            var provingOutput = ParseProvingOutput(provingOutputString);
            Console.WriteLine("proof: " + provingOutput.Proof);
            return provingOutput.Proof;
        }
        catch (Exception e)
        {
            Console.WriteLine("proof generate exception, e: ", e.Message);
            throw;
        }
    }

    private string ParseJwtPayload(string payload)
    {
        string padded = payload.Length % 4 == 0 ? payload : payload + "====".Substring(payload.Length % 4);
        string base64 = padded.Replace("_", "/").Replace("-", "+");
        byte[] outputb = Convert.FromBase64String(base64);
        string outStr = Encoding.Default.GetString(outputb);
        return outStr;
    }

    private byte[] HexStringToByteArray(string hex)
    {
        var length = hex.Length;
        var byteArray = new byte[length / 2];

        for (var i = 0; i < length; i += 2)
        {
            byteArray[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }

        return byteArray;
    }
    
    private async Task<string> GetGooglePublicKeyFromJwt(JwtSecurityToken jwt)
    {
        var options = new RestClientOptions("https://www.googleapis.com/oauth2/v3/certs");
        var client = new RestClient(options);
        var request = new RestRequest();
        var response = await client.GetAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return "";
        }

        var res = (JObject) JsonConvert.DeserializeObject(response.Content);
        var keys = res["keys"];
        foreach (var key in keys)
        {
            var kid = key["kid"].ToString();
            if (jwt.Header.Kid == kid)
            {
                return key["n"].ToString();
            }
        }

        return "";
    }
    
    private ProvingOutput ParseProvingOutput(string provingOutput)
    {
        return JsonConvert.DeserializeObject<ProvingOutput>(provingOutput);
    }
    
    private List<string> PadString(string str, int paddedBytesSize)
    {
        var paddedBytes = str.Select(c => ((int) c).ToString()).ToList();

        var paddingLength = paddedBytesSize - paddedBytes.Count;
        if (paddingLength > 0)
        {
            paddedBytes.AddRange(Enumerable.Repeat("0", paddingLength));
        }

        return paddedBytes;
    }

}

public class ProvingOutput
{
    public ProvingOutput(IList<string> publicInputs, string proof)
    {
        PublicInputs = publicInputs;
        Proof = proof;
    }

    [JsonProperty("public_inputs")] public IList<string> PublicInputs { get; set; }

    [JsonProperty("proof")] public string Proof { get; set; }

    public static ProvingOutput FromJsonString(string jsonString)
    {
        return JsonConvert.DeserializeObject<ProvingOutput>(jsonString);
    }
}