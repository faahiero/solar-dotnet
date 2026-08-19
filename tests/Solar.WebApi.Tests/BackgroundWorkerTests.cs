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
}
