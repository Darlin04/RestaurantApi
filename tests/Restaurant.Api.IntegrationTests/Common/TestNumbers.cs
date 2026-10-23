namespace Restaurant.Api.IntegrationTests.Common;

public static class TestNumbers
{
    private static int _counter;

    public static int NextTableNumber() => Interlocked.Increment(ref _counter);
}
