using Xunit;

namespace Furina.Tests;

// TASK-14 AC-1 verification: this test is deliberately wrong so pr.yml
// fails and branch protection can be proven to actually block the merge
// button (not just show a warning). Removed once verified.
public class IntentionallyFailingTest
{
    [Fact]
    public void ThisShouldFail()
    {
        Assert.True(false, "Intentional failure to verify branch protection blocks merge (TASK-14 AC-1)");
    }
}
