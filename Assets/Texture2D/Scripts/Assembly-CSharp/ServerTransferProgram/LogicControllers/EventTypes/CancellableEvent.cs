namespace ServerTransferProgram.LogicControllers.EventTypes
{
	public class CancellableEvent : Event
	{
		private bool Cancel;

		public void SetCancelled(bool state)
		{
			Cancel = state;
		}
	}
}
