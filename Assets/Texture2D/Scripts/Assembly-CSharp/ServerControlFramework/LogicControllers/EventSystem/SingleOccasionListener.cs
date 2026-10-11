using System;
using ServerTransferProgram.LogicControllers;

namespace ServerControlFramework.LogicControllers.EventSystem
{
	public class SingleOccasionListener<T> : EventListener<T>
	{
		public SingleOccasionListener(Func<T, bool> onRun)
			: base(onRun)
		{
			SingleOccasionListener<T> singleOccasionListener = this;
			onInvoke = (T a) =>
			{
				singleOccasionListener.holder.RemoveListener(singleOccasionListener);
				return onRun(a);
			};
		}

		public SingleOccasionListener(Action<T> onRun)
			: base(onRun)
		{
			onInvoke = (T a) =>
			{
				holder.RemoveListener(this);
				return true;
			};
		}

		public SingleOccasionListener(Action<T> onRun, object nullable)
			: base(onRun, (object)null)
		{
			onInvoke = (T a) =>
			{
				holder.RemoveListener(this);
				return true;
			};
		}
	}
}
