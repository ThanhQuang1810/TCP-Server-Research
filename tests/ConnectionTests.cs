using Shared;

internal static class ConnectionTests
{
	public static async Task<bool> Run()
	{
		foreach (int count in new[] { 1, 10, 50 })
		{
			var tasks = new List<Task<bool>>();
			for (int i = 0; i < count; i++)
			{
				long requestId = count * 1000L + i;
				tasks.Add(TestHelper.EchoAsync(requestId));
			}
			bool[] results = await Task.WhenAll(tasks);
			int success = results.Count(x => x);
			Console.WriteLine($"  {count} Client: {success}/{count} response dung");
			if (success != count) return false;
		}
		return true;
	}
}
