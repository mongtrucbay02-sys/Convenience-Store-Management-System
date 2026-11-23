using Xunit;

// The MySQL provider uses a server-wide migration lock across fixture databases.
// Avoid racing fixture DDL/cleanup across collections. Domain concurrency tests
// still create simultaneous requests with Task.WhenAll inside their test bodies.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
