using System;

namespace ServerTransferProgram.LogicControllers
{
	public class EventListenerCollectable
	{
		public readonly Type targetType;

		protected Func<object, bool> invoked;

		public EventManager holder;

		public EventListenerCollectable(Type t)
		{
			targetType = t;
		}

		public bool lowInvoke(object o)
		{
			return invoked(o);
		}
	}
}
