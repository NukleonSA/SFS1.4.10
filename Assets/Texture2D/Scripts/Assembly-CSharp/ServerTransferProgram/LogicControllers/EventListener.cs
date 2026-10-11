using System;

namespace ServerTransferProgram.LogicControllers
{
	public class EventListener<T> : EventListenerCollectable
	{
		public Func<T, bool> onInvoke;

		public EventListener(Func<T, bool> listener)
			: base(typeof(T))
		{
			onInvoke = listener;
			invoked = (object o) =>
			{
				T arg = (T)o;
				return onInvoke(arg);
			};
		}

		public EventListener(Action<T> listener)
			: base(typeof(T))
		{
			EventListener<T> eventListener = this;
			Func<T, bool> func = (T ev) =>
			{
				listener(ev);
				return true;
			};
			onInvoke = func;
			invoked = (object o) =>
			{
				T arg = (T)o;
				return eventListener.onInvoke(arg);
			};
		}

		public EventListener(Action<T> listener, object nullEventSelector)
			: base(typeof(T))
		{
			EventListener<T> eventListener = this;
			Func<T, bool> func = (T ev) =>
			{
				listener(ev);
				return true;
			};
			onInvoke = func;
			invoked = (object o) =>
			{
				T arg = (T)o;
				return eventListener.onInvoke(arg);
			};
		}

		public bool Invoke(T e)
		{
			return onInvoke(e);
		}
	}
}
