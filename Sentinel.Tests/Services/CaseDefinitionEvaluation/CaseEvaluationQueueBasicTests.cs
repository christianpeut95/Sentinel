using Xunit;
using Sentinel.Services.CaseDefinitionEvaluation;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Sentinel.Tests.Services.CaseDefinitionEvaluation
{
    /// <summary>
    /// Basic tests for CaseEvaluationQueue to verify thread-safety and FIFO behavior
    /// </summary>
    public class CaseEvaluationQueueBasicTests
    {
        [Fact]
        public async Task Queue_BasicOperations_Work()
        {
            // Arrange
            var queue = new CaseEvaluationQueue(NullLogger<CaseEvaluationQueue>.Instance);
            var caseId = Guid.NewGuid();

            // Act
            await queue.QueueEvaluationAsync(caseId, "Test reason");
            var job = await queue.DequeueAsync(default);

            // Assert
            Assert.NotNull(job);
            Assert.Equal(caseId, job.CaseId);
            Assert.Equal("Test reason", job.ChangeReason);
            Assert.Equal(0, queue.Count);
        }

        [Fact]
        public async Task Queue_MaintainsFIFO_Order()
        {
            // Arrange
            var queue = new CaseEvaluationQueue(NullLogger<CaseEvaluationQueue>.Instance);
            var caseId1 = Guid.NewGuid();
            var caseId2 = Guid.NewGuid();
            var caseId3 = Guid.NewGuid();

            // Act
            await queue.QueueEvaluationAsync(caseId1, "First");
            await queue.QueueEvaluationAsync(caseId2, "Second");
            await queue.QueueEvaluationAsync(caseId3, "Third");

            var job1 = await queue.DequeueAsync(default);
            var job2 = await queue.DequeueAsync(default);
            var job3 = await queue.DequeueAsync(default);

            // Assert
            Assert.Equal(caseId1, job1.CaseId);
            Assert.Equal(caseId2, job2.CaseId);
            Assert.Equal(caseId3, job3.CaseId);
        }

        [Fact]
        public async Task Queue_Count_IsAccurate()
        {
            // Arrange
            var queue = new CaseEvaluationQueue(NullLogger<CaseEvaluationQueue>.Instance);

            // Assert initial state
            Assert.Equal(0, queue.Count);

            // Act - Add items
            await queue.QueueEvaluationAsync(Guid.NewGuid(), "1");
            Assert.Equal(1, queue.Count);

            await queue.QueueEvaluationAsync(Guid.NewGuid(), "2");
            Assert.Equal(2, queue.Count);

            // Act - Remove items
            await queue.DequeueAsync(default);
            Assert.Equal(1, queue.Count);

            await queue.DequeueAsync(default);
            Assert.Equal(0, queue.Count);
        }

        [Fact]
        public async Task Queue_SetsTimestamp_OnEnqueue()
        {
            // Arrange
            var queue = new CaseEvaluationQueue(NullLogger<CaseEvaluationQueue>.Instance);
            var beforeTime = DateTime.UtcNow;

            // Act
            await queue.QueueEvaluationAsync(Guid.NewGuid(), "Test");
            var job = await queue.DequeueAsync(default);
            var afterTime = DateTime.UtcNow;

            // Assert
            Assert.NotNull(job);
            Assert.InRange(job.QueuedAt, beforeTime.AddSeconds(-1), afterTime.AddSeconds(1));
        }

        [Fact]
        public async Task Queue_ThreadSafety_ConcurrentWrites()
        {
            // Arrange
            var queue = new CaseEvaluationQueue(NullLogger<CaseEvaluationQueue>.Instance);
            var itemCount = 100;
            var tasks = new Task[itemCount];

            // Act - Queue many items concurrently
            for (int i = 0; i < itemCount; i++)
            {
                var index = i; // Capture for closure
                tasks[i] = Task.Run(async () =>
                {
                    await queue.QueueEvaluationAsync(Guid.NewGuid(), $"Test {index}");
                });
            }

            await Task.WhenAll(tasks);

            // Assert - All items should be queued
            Assert.Equal(itemCount, queue.Count);

            // Verify we can dequeue all items
            for (int i = 0; i < itemCount; i++)
            {
                var job = await queue.DequeueAsync(default);
                Assert.NotNull(job);
            }

            Assert.Equal(0, queue.Count);
        }
    }
}
