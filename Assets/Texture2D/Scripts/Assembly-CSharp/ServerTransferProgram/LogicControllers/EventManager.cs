using System;
using System.Collections.Generic;
using ServerTransferProgram.LogicControllers.EventTypes;

namespace ServerTransferProgram.LogicControllers
{
	public class EventManager
	{
		private List<EventListenerCollectable> eventListeners = new List<EventListenerCollectable>();

		public void RegisterListener(EventListenerCollectable listener)
		{
			eventListeners.Add(listener);
			listener.holder = this;
		}

		public void RemoveListener(EventListenerCollectable listener)
		{
			eventListeners.Remove(listener);
		}

		public T Invoke<T>(T myEvent)
		{
			if (!myEvent.GetType().IsSubclassOf(typeof(Event)))
			{
				throw new Exception("This class isn't a subclass of Event");
			}
			bool cancelled = false;
			EventListenerCollectable[] array = eventListeners.ToArray();
			EventListenerCollectable[] array2 = array;
			foreach (EventListenerCollectable eventListenerCollectable in array2)
			{
				if (eventListenerCollectable.targetType == myEvent.GetType() && !eventListenerCollectable.lowInvoke(myEvent))
				{
					cancelled = true;
					break;
				}
			}
			if (myEvent.GetType().IsSubclassOf(typeof(CancellableEvent)))
			{
				((CancellableEvent)(object)myEvent).SetCancelled(cancelled);
			}
			return myEvent;
		}
	}
}
