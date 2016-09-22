using System;

namespace CloudApiHost.Helper
{
	public class CASingleton<T> where T : class
	{
		private static readonly Lazy<T> InstanceHolder = new Lazy<T>(CreateInstance);
		public static T Instance => InstanceHolder.Value;

		private static T CreateInstance()
		{
			return Activator.CreateInstance(typeof (T), true) as T;
		}
	}
}

