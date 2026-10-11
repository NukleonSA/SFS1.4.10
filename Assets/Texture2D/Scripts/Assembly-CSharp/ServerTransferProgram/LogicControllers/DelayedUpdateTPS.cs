namespace ServerTransferProgram.LogicControllers
{
	public class DelayedUpdateTPS : Delayer
	{
		public DelayedUpdateTPS(int loopsPerSecond)
		{
			holdMS = 1000 / loopsPerSecond;
		}
	}
}
