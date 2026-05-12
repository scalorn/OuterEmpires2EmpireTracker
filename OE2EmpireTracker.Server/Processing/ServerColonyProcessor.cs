using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OE2EmpireTracker.Server.Processing;

/// <summary>
/// Processes colony timers server-side for opted-in characters (Req 17).
/// Works with raw JSON since the Common library (net481) cannot be directly
/// referenced from the Server (net8.0). Advances CountDownTime objects
/// (BuildCompletionTime, ProcessCompletionTime) on colony structures by
/// computing elapsed intervals and updating IntervalsPassed.
/// </summary>
public class ServerColonyProcessor
{
    private readonly ILogger<ServerColonyProcessor> _logger;

    public ServerColonyProcessor(ILogger<ServerColonyProcessor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Processes colony data for a character. Advances timers on all structures
    /// and returns true if any data was modified.
    /// </summary>
    public bool ProcessColonies(string coloniesJson, out string updatedJson)
    {
        updatedJson = coloniesJson;

        try
        {
            var colonies = JArray.Parse(coloniesJson);
            bool anyModified = false;

            foreach (var colony in colonies)
            {
                if (colony is not JObject colonyObj)
                {
                    continue;
                }

                var structures = colonyObj["Structures"] as JArray;
                if (structures == null)
                {
                    continue;
                }

                foreach (var structure in structures)
                {
                    if (structure is not JObject structureObj)
                    {
                        continue;
                    }

                    // Process BuildCompletionTime
                    if (AdvanceTimer(structureObj, "BuildCompletionTime"))
                    {
                        anyModified = true;
                    }

                    // Process ProcessCompletionTime
                    if (AdvanceTimer(structureObj, "ProcessCompletionTime"))
                    {
                        anyModified = true;
                    }
                }
            }

            if (anyModified)
            {
                updatedJson = colonies.ToString(Formatting.Indented);
            }

            return anyModified;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse colony JSON for processing");
            return false;
        }
    }

    /// <summary>
    /// Checks if any structure in the colony JSON has active timers that need processing.
    /// </summary>
    public bool HasActiveTimers(string coloniesJson)
    {
        try
        {
            var colonies = JArray.Parse(coloniesJson);
            foreach (var colony in colonies)
            {
                if (colony is not JObject colonyObj)
                {
                    continue;
                }

                var structures = colonyObj["Structures"] as JArray;
                if (structures == null)
                {
                    continue;
                }

                foreach (var structure in structures)
                {
                    if (structure is not JObject structureObj)
                    {
                        continue;
                    }

                    if (HasTimer(structureObj, "BuildCompletionTime") ||
                        HasTimer(structureObj, "ProcessCompletionTime"))
                    {
                        return true;
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Malformed JSON — no timers to process
        }

        return false;
    }

    private static bool HasTimer(JObject structureObj, string timerProperty)
    {
        var timer = structureObj[timerProperty] as JObject;
        if (timer == null)
        {
            return false;
        }

        var startTime = timer["StartTime"];
        return startTime != null && startTime.Type != JTokenType.Null;
    }

    /// <summary>
    /// Advances a CountDownTime timer on a structure. Computes elapsed time since
    /// StartTime and updates IntervalsPassed for repeating timers, or marks
    /// non-repeating timers as expired.
    /// Returns true if the timer was modified.
    /// </summary>
    private bool AdvanceTimer(JObject structureObj, string timerProperty)
    {
        var timer = structureObj[timerProperty] as JObject;
        if (timer == null)
        {
            return false;
        }

        var startTimeToken = timer["StartTime"];
        if (startTimeToken == null || startTimeToken.Type == JTokenType.Null)
        {
            return false;
        }

        DateTime startTime;
        try
        {
            startTime = startTimeToken.Value<DateTime>();
        }
        catch
        {
            return false;
        }

        var isRepeating = timer["IsRepeating"]?.Value<bool>() ?? false;
        var repeatIntervalSeconds = timer["RepeatIntervalSeconds"]?.Value<double>() ?? 0;
        var currentIntervalsPassed = timer["IntervalsPassed"]?.Value<long>() ?? 0;

        var now = DateTime.UtcNow;
        var elapsed = now - startTime;

        if (isRepeating && repeatIntervalSeconds > 0)
        {
            // Calculate total intervals that should have passed since start
            long totalIntervals = (long)(elapsed.TotalSeconds / repeatIntervalSeconds);
            var consumedIntervals = timer["ConsumedIntervals"]?.Value<long>() ?? 0;
            long newIntervalsPassed = totalIntervals - consumedIntervals;

            if (newIntervalsPassed > currentIntervalsPassed)
            {
                timer["IntervalsPassed"] = newIntervalsPassed;
                timer["LastProcessedUtc"] = now.ToString("o");

                _logger.LogDebug(
                    "Advanced repeating timer on {Property}: {Old} -> {New} intervals",
                    timerProperty,
                    currentIntervalsPassed,
                    newIntervalsPassed);

                return true;
            }
        }
        else if (!isRepeating)
        {
            // Non-repeating timer: compute remaining time
            var durationSeconds = timer["DurationSeconds"]?.Value<double>() ?? 0;
            if (durationSeconds > 0)
            {
                var remaining = durationSeconds - elapsed.TotalSeconds;
                var currentRemaining = timer["TimeRemaining"]?.Value<double>() ?? remaining;

                if (remaining < currentRemaining)
                {
                    timer["TimeRemaining"] = Math.Max(0, remaining);
                    timer["LastProcessedUtc"] = now.ToString("o");

                    _logger.LogDebug(
                        "Advanced non-repeating timer on {Property}: remaining={Remaining:F1}s",
                        timerProperty,
                        remaining);

                    return true;
                }
            }
        }

        return false;
    }
}
