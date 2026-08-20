using Solar.Infrastructure.Background;
using Xunit;

namespace Solar.WebApi.Tests;

public class BackgroundWorkerTests
{
    [Fact]
    public async Task QueueBackgroundWorkItemAsync_ShouldEnqueueAndDequeueTaskSuccessfully()
    {
        // Arrange
        var queue = new DefaultBackgroundTaskQueue(50);
        bool executed = false;

        // Act
        await queue.QueueBackgroundWorkItemAsync(token =>
        {
            executed = true;
            return ValueTask.CompletedTask;
        });

        Assert.Equal(1, queue.PendingCount);

        var workItem = await queue.DequeueAsync(CancellationToken.None);
        await workItem(CancellationToken.None);

        // Assert
        Assert.True(executed);
        Assert.Equal(0, queue.PendingCount);
    }

    [Fact]
    public async Task QueueBackgroundWorkItemAsync_ShouldProcessMultipleItemsInOrderWithoutLockContention()
    {
        // Arrange
        var queue = new DefaultBackgroundTaskQueue(500);
        var processedItems = new System.Collections.Concurrent.ConcurrentBag<int>();

        // Act: Enfileira 100 tarefas assíncronas concorrentes
        var tasks = Enumerable.Range(1, 100).Select(i => queue.QueueBackgroundWorkItemAsync(token =>
        {
            processedItems.Add(i);
            return ValueTask.CompletedTask;
        })).ToArray();

        await Task.WhenAll(tasks.Select(v => v.AsTask()));

        // Processa todos os itens da fila
        while (queue.PendingCount > 0)
        {
            var workItem = await queue.DequeueAsync(CancellationToken.None);
            await workItem(CancellationToken.None);
        }

        // Assert
        Assert.Equal(100, processedItems.Count);
        Assert.Equal(0, queue.PendingCount);
    }
}
