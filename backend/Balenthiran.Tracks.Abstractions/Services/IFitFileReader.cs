using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.Abstractions.Services;

public interface IFitFileReader
{
    /// <summary>
    /// Reads the run in a file from Garmin Connect's "Export Original": a <c>.fit</c>, or the <c>.zip</c> around it.
    /// Throws <c>ValidationException</c> for anything else, a damaged file, or an activity that is not a run.
    /// </summary>
    IFitActivity Read(Stream file);
}
