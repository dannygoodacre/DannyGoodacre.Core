using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace DannyGoodacre.Cqrs.Testing;

/// <summary>
/// Provides fluent xUnit assertion extensions for <see cref="FakeLogCollector"/>
/// to verify source-generated CQRS log messages.
/// </summary>
public static class FakeLogCollectorExtensions
{
    extension(FakeLogCollector collector)
    {
        /// <summary>
        /// Asserts that a log entry with the specified event ID and level exists in the collector.
        /// </summary>
        public FakeLogRecord ShouldHaveLogged(int eventId, LogLevel level)
        {
            IReadOnlyList<FakeLogRecord> snapshot = collector.GetSnapshot();

            FakeLogRecord? record = snapshot.FirstOrDefault(r => r.Id.Id == eventId && r.Level == level);

            Assert.True(
                record is not null,
                $"Expected a log entry with EventId {eventId} and Level '{level}', but none was found. " +
                $"Actual logs ({snapshot.Count}): {string.Join("; ", snapshot.Select(s => $"[{s.Level}] {s.Message}"))}");

            return record;
        }

        /// <summary>
        /// Asserts that a log entry with the specified event ID and level exists in the collector.
        /// </summary>
        public FakeLogRecord ShouldHaveLogged(CqrsLogEvent logEvent, LogLevel level)
            => collector.ShouldHaveLogged((int)logEvent, level);
    }
}
