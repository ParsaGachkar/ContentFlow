// Docker Compose stack contract tests (review: the project is part of its stack).
//
// Pins the stack shape WITHOUT running Docker: no containers, no ports, fully
// deterministic. Anything requiring a live stack belongs in E2E (Testcontainers);
// ad-hoc curl against manually started stacks is not a verification method.
// These are permanent regression guards: DO NOT DELETE.

namespace ContentFlow.IntegrationTests;

public sealed class ComposeStackTests
{
    private static readonly string RepoRoot = FindRepoRoot();
    private static readonly string Compose = Read("docker-compose.yml");
    private static readonly string Dockerignore = Read(".dockerignore");

    [Fact]
    public void Compose_IncludesWebService_BuiltFromWebDockerfile()
    {
        Assert.Contains("web:", Compose, StringComparison.Ordinal);
        Assert.Contains("src/ContentFlow.Blazor/Web/Dockerfile", Compose, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(RepoRoot, "src", "ContentFlow.Blazor", "Web", "Dockerfile")));
    }

    [Fact]
    public void Compose_IncludesMigratorService_RunningApply()
    {
        Assert.Contains("migrator:", Compose, StringComparison.Ordinal);
        Assert.Contains("tools/ContentFlow.Migrator/Dockerfile", Compose, StringComparison.Ordinal);
        Assert.Contains("\"apply\"", Compose, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(RepoRoot, "tools", "ContentFlow.Migrator", "Dockerfile")));
    }

    [Fact]
    public void Compose_StartOrder_IsPostgresThenMigratorThenWeb()
    {
        // Web must wait for a healthy DB AND a successfully completed migration.
        Assert.Contains("service_healthy", Compose, StringComparison.Ordinal);
        Assert.Contains("service_completed_successfully", Compose, StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_WebHealthcheck_ProbesHealthEndpoint()
    {
        Assert.Contains("/healthz", Compose, StringComparison.Ordinal);
    }

    [Fact]
    public void Dockerignore_ExcludesHostBuildArtifacts()
    {
        // Regression guard: a Windows node_modules copied into the Linux image
        // build breaks on node.exe shims; bin/obj bloat the context. Both must
        // always be excluded (proven by a real broken image build).
        Assert.Contains("**/node_modules/", Dockerignore, StringComparison.Ordinal);
        Assert.Contains("**/bin/", Dockerignore, StringComparison.Ordinal);
        Assert.Contains("**/obj/", Dockerignore, StringComparison.Ordinal);
    }

    private static string Read(string relativePath)
    {
        var full = Path.Combine(RepoRoot, relativePath);
        Assert.True(File.Exists(full), $"Expected repo file missing: {relativePath}");
        return File.ReadAllText(full);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ContentFlow.slnx")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir.FullName;
    }
}
