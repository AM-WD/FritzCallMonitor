namespace AMWD.Net.Api.Fritz.CallMonitor.Wrappers
{
	/// <summary>
	/// Factory for creating instances of <see cref="TcpClientWrapper"/>.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	internal class TcpClientWrapperFactory
	{
		/// <summary>
		/// Create a new instance of <see cref="TcpClientWrapper"/>.
		/// </summary>
		public virtual TcpClientWrapper Create()
		{
			var client = new TcpClientWrapper();
			return client;
		}
	}
}
