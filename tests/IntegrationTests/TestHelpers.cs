// Shared test helpers for integration tests.

using System;
using System.Threading;

namespace IntegrationTests
{
    // Thread-safe holder for a single exception value.
    // Used to capture errors from background tasks without races.
    internal sealed class ExceptionHolder
    {
        private readonly object _lock = new object();
        private Exception? _value;

        public void Set(Exception ex)
        {
            lock (_lock) { _value = ex; }
        }

        public Exception? Get()
        {
            lock (_lock) { return _value; }
        }
    }
}
