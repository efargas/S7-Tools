    private async Task WaitForFlushTickAsync(PeriodicTimer timer, StreamWriter writer)
    {
        try
        {
            while (await timer.WaitForNextTickAsync(_shutdownCts.Token))
            {
                await writer.FlushAsync();
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            // Log to console as a last resort. Swallowing this would hide flush errors.
            Console.Error.WriteLine($"AsyncFileLogger periodic flush failed: {ex}");
        }
    }
