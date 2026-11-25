namespace EdiMorph.Specifications.Tests;

public sealed class FileSystemSpecificationSourceTests
{
    [Fact]
    public void Read_WhenConsumerProfilesAreDeployedUnderBin_ShouldLoadThemAndIgnoreNestedBuildOutput()
    {
        // Arrange
        var root = Path.Combine(Path.GetTempPath(), "EdiMorph-source-tests", Guid.NewGuid().ToString("N"));
        var profiles = Path.Combine(root, "bin", "profiles");
        Directory.CreateDirectory(Path.Combine(profiles, "bin"));
        Directory.CreateDirectory(Path.Combine(profiles, "obj"));
        File.WriteAllText(Path.Combine(profiles, "profile.rule"), "{}");
        File.WriteAllText(Path.Combine(profiles, "bin", "ignored.rule"), "{}");
        File.WriteAllText(Path.Combine(profiles, "obj", "ignored.rule"), "{}");
        try
        {
            // Act
            var result = new FileSystemSpecificationSource(profiles).Read();

            // Assert
            result.Should().ContainSingle().Which.Path.Should().Be("profile.rule");
        }
        finally
        {
            // Only delete the unique test-owned directory created above.
            Directory.Delete(root, recursive: true);
        }
    }
}
