using System;
using System.Threading;

internal sealed class ConnectionManager
{
    private readonly int _maxConnections;
    private int _currentConnections;

    public ConnectionManager(int maxConnections)
    {
        if (maxConnections <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(maxConnections));

        _maxConnections = maxConnections;
    }

    public int CurrentConnections =>
        Volatile.Read(ref _currentConnections);

    public int MaxConnections => _maxConnections;

    public bool TryAddConnection()
    {
        while (true)
        {
            int current = CurrentConnections;

            if (current >= _maxConnections)
                return false;

            if (Interlocked.CompareExchange(
                ref _currentConnections,
                current + 1,
                current) == current)
            {
                return true;
            }
        }
    }

    public void RemoveConnection()
    {
        while (true)
        {
            int current = CurrentConnections;

            if (current == 0)
                return;

            if (Interlocked.CompareExchange(
                ref _currentConnections,
                current - 1,
                current) == current)
            {
                return;
            }
        }
    }
}
