using POSInventorySystem.UI;

namespace POSInventorySystem.Tests;

// A quick save finishes before the "Saving..." box would appear, so these
// need no window.
public class BusyTests
{
    [Fact]
    public void The_result_of_the_work_is_returned()
    {
        Assert.Equal(42, Busy.Run(null, () => 42));
    }

    [Fact]
    public void A_failed_save_throws_the_original_exception_so_the_caller_can_warn()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => Busy.Run<int>(null, () => throw new InvalidOperationException("db down")));

        Assert.Equal("db down", ex.Message);
    }

    [Fact]
    public void The_work_runs_off_the_calling_thread()
    {
        int caller = Environment.CurrentManagedThreadId;

        Assert.NotEqual(caller, Busy.Run(null, () => Environment.CurrentManagedThreadId));
    }
}
