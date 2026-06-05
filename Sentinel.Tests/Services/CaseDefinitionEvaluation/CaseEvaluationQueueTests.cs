using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using Sentinel.Services.CaseDefinitionEvaluation;
using System;
using System.Threading.Tasks;

namespace Sentinel.Tests.Services.CaseDefinitionEvaluation
{
    public class CaseEvaluationQueueTests
    {
        [Fact]
        public async Task QueueEvaluationAsync_QueuesJob_IncreasesCount()
        {
            // Arrange
            var queue = new CaseEvaluationQueue(NullLogger<CaseEvaluationQueue>.Instance);
            var caseId = Guid.NewGuid();
            var reason = "Test reason";

            // Act
            await queue.QueueEvaluationAsync(caseId, reason);

            // Assert
            Assert.Equal(1, queue.Count);
        }

        [Fact]
        public async Task DequeueAsync_ReturnsQueuedJob()
        {
            // Arrange
            var queue = new CaseEvaluationQueue(NullLogger<CaseEvaluationQueue>.Instance);
            var caseId = Guid.NewGuid();
            var reason = "Test reason";
            await queue.QueueEvaluationAsync(caseId, reason);

            // Act
            var job = await queue.DequeueAsync(default);

            // Assert
            Assert.NotNull(job);
            Assert.Equal(caseId, job.CaseId);
            Assert.Equal(reason, job.ChangeReason);
            Assert.Equal(0, queue.Count);
        }

        [Fact]
        public async Task QueueEvaluationAsync_MultipleJobs_MaintainsOrder()
        {
            // Arrange
            var queue = new CaseEvaluationQueue(NullLogger<CaseEvaluationQueue>.Instance);
            var caseId1 = Guid.NewGuid();
            var caseId2 = Guid.NewGuid();
            var caseId3 = Guid.NewGuid();

            // Act
            await queue.QueueEvaluationAsync(caseId1, "Reason 1");
            await queue.QueueEvaluationAsync(caseId2, "Reason 2");
            await queue.QueueEvaluationAsync(caseId3, "Reason 3");

            // Assert
            Assert.Equal(3, queue.Count);

            var job1 = await queue.DequeueAsync(default);
            Assert.Equal(caseId1, job1!.CaseId);
            Assert.Equal(2, queue.Count);

            var job2 = await queue.DequeueAsync(default);
            Assert.Equal(caseId2, job2!.CaseId);
            Assert.Equal(1, queue.Count);

            var job3 = await queue.DequeueAsync(default);
            Assert.Equal(caseId3, job3!.CaseId);
            Assert.Equal(0, queue.Count);
        }

        [Fact]
        public async Task Count_AfterQueueAndDequeue_ReflectsCorrectValue()
        {
            // Arrange
            var queue = new CaseEvaluationQueue(NullLogger<CaseEvaluationQueue>.Instance);

            // Act & Assert
            Assert.Equal(0, queue.Count);

            await queue.QueueEvaluationAsync(Guid.NewGuid(), "Test 1");
            Assert.Equal(1, queue.Count);

            await queue.QueueEvaluationAsync(Guid.NewGuid(), "Test 2");
            Assert.Equal(2, queue.Count);

            await queue.DequeueAsync(default);
            Assert.Equal(1, queue.Count);

            await queue.DequeueAsync(default);
            Assert.Equal(0, queue.Count);
        }

        [Fact]
        public async Task QueueEvaluationAsync_SetsQueuedAtTimestamp()
        {
            // Arrange
            var queue = new CaseEvaluationQueue(NullLogger<CaseEvaluationQueue>.Instance);
            var beforeQueue = DateTime.UtcNow;

            // Act
            await queue.QueueEvaluationAsync(Guid.NewGuid(), "Test");
            var job = await queue.DequeueAsync(default);
            var afterQueue = DateTime.UtcNow;

            // Assert
            Assert.NotNull(job);
            Assert.InRange(job.QueuedAt, beforeQueue, afterQueue);
        }

        [Fact]
        public async Task ConcurrentQueueOperations_MaintainsThreadSafety()
        {
            // Arrange
            var queue = new CaseEvaluationQueue(NullLogger<CaseEvaluationQueue>.Instance);
            var taskCount = 100;
            var tasks = new Task[taskCount];

            // Act - Queue 100 items concurrently
            for (int i = 0; i < taskCount; i++)
            {
                var caseId = Guid.NewGuid();
                tasks[i] = Task.Run(async () => await queue.QueueEvaluationAsync(caseId, $"Test {i}"));
            }

            await Task.WhenAll(tasks);

            // Assert - All items should be queued
            Assert.Equal(taskCount, queue.Count);

            // Dequeue all
            for (int i = 0; i < taskCount; i++)
            {
                var job = await queue.DequeueAsync(default);
                Assert.NotNull(job);
            }

            Assert.Equal(0, queue.Count);
        }
    }
}
