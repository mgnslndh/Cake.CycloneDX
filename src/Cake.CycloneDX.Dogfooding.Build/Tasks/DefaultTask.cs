using Cake.Frosting;

namespace Cake.CycloneDX.Dogfooding.Build.Tasks;

[TaskName("Default")]
[IsDependentOn(typeof(GenerateSbomTask))]
[IsDependentOn(typeof(ValidateSbomTask))]
public class DefaultTask : FrostingTask
{
}