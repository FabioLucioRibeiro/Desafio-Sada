using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Infra.Context;

namespace TaskManagement.Tests.Integration;

public sealed class TasksApiTests
{
    [Fact]
    public async Task Crud_ReturnsExpectedStatusesAndPreservesCode()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/tasks", new { title = "Teste" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(created.GetProperty("code").GetInt32() > 0);
        Assert.Equal("Pending", created.GetProperty("status").GetString());
        Assert.NotNull(response.Headers.Location);
        var location = response.Headers.Location!;
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(location)).StatusCode);

        var updatedResponse = await client.PutAsJsonAsync(location, new
        {
            title = "Atualizada", description = "Descrição", dueDate = "2026-10-10", status = "Completed"
        });
        Assert.Equal(HttpStatusCode.OK, updatedResponse.StatusCode);
        var updated = await updatedResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(created.GetProperty("code").GetInt32(), updated.GetProperty("code").GetInt32());
        Assert.Equal(created.GetProperty("createdAt").GetString(), updated.GetProperty("createdAt").GetString());
        Assert.Equal("Atualizada", updated.GetProperty("title").GetString());
        Assert.Equal("Completed", updated.GetProperty("status").GetString());
        Assert.Equal("Descrição", updated.GetProperty("description").GetString());
        Assert.Equal("2026-10-10", updated.GetProperty("dueDate").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync(location)).StatusCode);
        var missing = await client.GetAsync(location);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("application/problem+json", missing.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync(location)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PutAsJsonAsync(location, new { title = "Ausente" })).StatusCode);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"title\":\"   \"}")]
    [InlineData("{\"title\":\"Teste\",\"status\":\"Invalid\"}")]
    [InlineData("{\"title\":\"Teste\",\"status\":1}")]
    [InlineData("{\"title\":\"Teste\",\"dueDate\":\"2026-02-30\"}")]
    public async Task InvalidBody_Returns400ForCreateAndUpdate(string json)
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/tasks", new { title = "Original" });
        foreach (var method in new[] { HttpMethod.Post, HttpMethod.Put })
        {
            using var request = new HttpRequestMessage(method,
                method == HttpMethod.Post ? new Uri("/api/tasks", UriKind.Relative) : created.Headers.Location);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        var original = await client.GetFromJsonAsync<JsonElement>(created.Headers.Location);
        Assert.Equal("Original", original.GetProperty("title").GetString());
    }

    [Theory]
    [InlineData("/api/tasks/not-a-guid")]
    [InlineData("/api/tasks?status=Unknown")]
    [InlineData("/api/tasks?status=99")]
    [InlineData("/api/tasks?dueDate=not-a-date")]
    public async Task InvalidRouteOrFilter_Returns400(string url)
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task CodeFromClient_DoesNotOverrideGeneratedValue()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        var codes = new List<int>();
        for (var i = 0; i < 2; i++)
        {
            var response = await client.PostAsJsonAsync("/api/tasks", new { title = "Teste", code = 999999 });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var task = await response.Content.ReadFromJsonAsync<JsonElement>();
            var code = task.GetProperty("code").GetInt32();
            Assert.InRange(code, 1, 999998);
            codes.Add(code);
        }
        Assert.Equal(2, codes.Distinct().Count());
    }

    [Fact]
    public async Task SearchAndFilters_AreAppliedThroughHttp()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/tasks", new
        {
            title = "Reunião", status = "Pending", dueDate = "2026-10-10"
        });
        await client.PostAsJsonAsync("/api/tasks", new { title = "Outra", status = "Completed" });
        var result = await client.GetFromJsonAsync<JsonElement>(
            "/api/tasks?search=REUNI&status=Pending&dueDate=2026-10-10");
        Assert.Equal(1, result.GetArrayLength());
        Assert.Equal("Reunião", result[0].GetProperty("title").GetString());
        var empty = await client.GetFromJsonAsync<JsonElement>("/api/tasks?search=ausente");
        Assert.Equal(0, empty.GetArrayLength());
    }

    [Fact]
    public async Task ConcurrentCreates_ReturnDistinctPositiveCodes()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/tasks")).StatusCode);
        var codes = await Task.WhenAll(Enumerable.Range(1, 20).Select(async i =>
        {
            using var response = await client.PostAsJsonAsync("/api/tasks", new { title = $"Tarefa {i}" });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var task = await response.Content.ReadFromJsonAsync<JsonElement>();
            return task.GetProperty("code").GetInt32();
        }));
        Assert.All(codes, code => Assert.True(code > 0));
        Assert.Equal(20, codes.Distinct().Count());
    }

    [Fact]
    public async Task Swagger_DescribesCrudAndGeneratedCodeOnlyInResponse()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await response.Content.ReadFromJsonAsync<JsonElement>();
        var paths = document.GetProperty("paths");
        Assert.True(paths.GetProperty("/api/tasks").TryGetProperty("post", out _));
        Assert.True(paths.GetProperty("/api/tasks/{id}").TryGetProperty("put", out _));
        Assert.True(paths.GetProperty("/api/tasks/{id}").TryGetProperty("delete", out _));
        var schemas = document.GetProperty("components").GetProperty("schemas");
        Assert.False(schemas.GetProperty("SaveTaskDto").GetProperty("properties").TryGetProperty("code", out _));
        Assert.True(schemas.GetProperty("TaskDto").GetProperty("properties").TryGetProperty("code", out _));
    }

    private sealed class TestApiFactory : WebApplicationFactory<Program>
    {
        private readonly string databaseName = Guid.NewGuid().ToString();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
                services.AddDbContext<DataContext>(options => options.UseInMemoryDatabase(databaseName)));
        }
    }
}
