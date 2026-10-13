using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;

namespace Sentinel.Tests.Services;

public sealed class CaseCreationInterceptorTests
{
    [Fact]
    public async Task ProcessPendingTaskTriggersAsync_CreatesCaseTasksAfterTheCallerCommits()
    {
        var taskService = new Mock<ITaskService>();
        taskService
            .Setup(service => service.CreateTasksForCase(It.IsAny<Guid>(), It.IsAny<TaskTrigger>()))
            .ReturnsAsync(new List<CaseTask>());

        await using var serviceProvider = new ServiceCollection()
            .AddScoped<ITaskService>(_ => taskService.Object)
            .BuildServiceProvider();
        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"case-task-trigger-{Guid.NewGuid()}")
                .Options,
            new HttpContextAccessor());

        var caseId = Guid.NewGuid();
        context.SetCasesAwaitingTaskCreation(new List<CaseTaskTrigger>
        {
            new() { CaseId = caseId, Type = CaseType.Case }
        });

        var interceptor = new CaseCreationInterceptor(
            serviceProvider,
            Mock.Of<ILogger<CaseCreationInterceptor>>());

        await interceptor.ProcessPendingTaskTriggersAsync(context);

        taskService.Verify(
            service => service.CreateTasksForCase(caseId, TaskTrigger.OnCaseCreation),
            Times.Once);
        Assert.Null(context.GetCasesAwaitingTaskCreation());
    }
}
