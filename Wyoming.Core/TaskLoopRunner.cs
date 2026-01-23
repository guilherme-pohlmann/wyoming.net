using Microsoft.Extensions.Logging;

namespace Wyoming.Net.Core;

[Flags]
public enum TaskLoopRunnerOptions
{
    None = 0,
    LongRunning = 1,
    RestartOnFail = 2
}

public abstract class TaskLoopRunner
{
    private readonly TaskLoopRunnerOptions options;
    private readonly SemaphoreSlim startStopSemaphore = new(1, 1);

    protected readonly ILogger logger;

    private CancellationTokenSource? cancellationTokenSource;
    private Task? loopTask;
    private bool isRunning;

    protected TaskLoopRunner(ILogger logger, TaskLoopRunnerOptions options = TaskLoopRunnerOptions.None)
    {;
        this.logger = logger;
        this.options = options;
    }

    public CancellationTokenSource? CancellationTokenSource => cancellationTokenSource;

    public bool IsRunning => isRunning;

    public async ValueTask StartAsync()
    {
        try
        {
            await startStopSemaphore.WaitAsync();

            if (isRunning)
            {
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();
            await OnStartAsync();

            if (options.HasFlag(TaskLoopRunnerOptions.LongRunning))
            {
                loopTask = Task.Factory.StartNew(SafeLoopAsync, TaskCreationOptions.LongRunning).Unwrap();
            }
            else
            {
                loopTask = Task.Run(SafeLoopAsync);
            }

            isRunning = true;
        }
        finally
        {
            startStopSemaphore.Release();
        }
    }

    public ValueTask StopAsync()
    {
        return StopAsync(true);
    }

    protected async Task WaitAsync()
    {
        if (isRunning)
        {
            await loopTask!;
        }
    }

    protected virtual ValueTask OnStartAsync() 
    {
        return ValueTask.CompletedTask;
    }

    protected virtual ValueTask OnStopAsync() 
    {
        return ValueTask.CompletedTask;
    }

    private async ValueTask StopAsync(bool shouldWait)
    {
        try
        {
            await startStopSemaphore.WaitAsync();

            if (!isRunning)
            {
                return;
            }
            isRunning = false;

            if (cancellationTokenSource is not null)
            {
                await cancellationTokenSource.CancelAsync();
            }

            if (shouldWait && loopTask is not null && !loopTask.IsCompleted)
            {
                try
                {
                    await loopTask.WaitAsync(TimeSpan.FromMinutes(5));
                    loopTask.Dispose();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
               
            }
            await OnStopAsync();
        }
        finally
        {
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
            loopTask = null;

            startStopSemaphore.Release();
        }
    }

    private async Task SafeLoopAsync()
    {
        try
        {
            await LoopAsync();
        }
        catch (OperationCanceledException)
        {
            logger.LogDebug("TaskLoopRunner of type {type} was cancelled", GetType().Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TaskLoopRunner of type {type} got an exception", GetType().Name);

            if(options.HasFlag(TaskLoopRunnerOptions.RestartOnFail))
            {
                logger.LogInformation(ex, "TaskLoopRunner of type {type} is restarting", GetType().Name);
                await StopAsync(false);
                await StartAsync();
            }
        }
    }

    protected abstract Task LoopAsync();
}
