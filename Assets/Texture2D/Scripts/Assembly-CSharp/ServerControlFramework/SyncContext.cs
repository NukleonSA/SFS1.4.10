using System;
using System.Threading;

namespace ServerControlFramework
{
	public class SyncContext
	{
		public static SynchronizationContext context;

		private static SynchronizationContext getContext()
		{
			if (context == null)
			{
				context = new SynchronizationContext();
			}
			return context;
		}

		public static void RunOnUI(Action toDo)
		{
			getContext().Post((object a) =>
			{
				toDo();
			}, null);
		}

		public static void RunOnUI<T>(Action<T> toDo, T arg)
		{
			getContext().Post((object a) =>
			{
				toDo(arg);
			}, null);
		}

		public static void RunOnUI<T, T1>(Action<T, T1> toDo, T arg, T1 arg2)
		{
			getContext().Post((object a) =>
			{
				toDo(arg, arg2);
			}, null);
		}

		public static void RunOnUI<T, T1, T2>(Action<T, T1, T2> toDo, T arg, T1 arg2, T2 arg3)
		{
			getContext().Post((object a) =>
			{
				toDo(arg, arg2, arg3);
			}, null);
		}

		public static void Sepperate(Action toDo)
		{
			ThreadPool.QueueUserWorkItem((object s) =>
			{
				toDo();
			}, null);
		}
	}
}
