using Orleans;
using Shared;
namespace Grains;

public interface IMultiImageGeneratorGrain : ISchrodingerGrain, IGrainWithStringKey
{
}
