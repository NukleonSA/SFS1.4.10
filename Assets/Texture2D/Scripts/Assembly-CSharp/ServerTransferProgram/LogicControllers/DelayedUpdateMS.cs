namespace ServerTransferProgram.LogicControllers
{
	public class DelayedUpdateMS : Delayer
	{
		public DelayedUpdateMS(int holdPerLoop)
		{
			holdMS = holdPerLoop;
		}
	}
}
