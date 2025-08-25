using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AMWD.Net.Api.Fritz.CallMonitor.Utils
{
	internal static class Extensions
	{
		[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		public static async void Forget(this Task task, ILogger? logger = null)
		{
			try
			{
				await task.ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				logger?.LogError(ex, "An error occurred in a fire-and-forget task.");
			}
		}
	}
}
