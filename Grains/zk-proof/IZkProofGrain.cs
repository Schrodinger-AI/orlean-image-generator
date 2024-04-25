namespace Grains;

public interface IZkProofGrain : IGrainWithStringKey
{
    Task<string> Generate(string jwt, string salt);
    // Task<string> Login();
}