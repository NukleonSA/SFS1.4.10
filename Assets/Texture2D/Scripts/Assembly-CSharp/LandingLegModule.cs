public class LandingLegModule : Module
{
	public MoveModule moveModule;

	public string onTimeWarpWarningOpen;

	public string onTimeWarpWarningClose;

	public float closed;

	public float open;

	public override void OnPartUsed()
	{
		if (Ref.timeWarping)
		{
			MenuManager.ShowMsg((moveModule.targetTime.floatValue != closed) ? onTimeWarpWarningClose : onTimeWarpWarningOpen);
		}
		else
		{
			moveModule.SetTargetTime((moveModule.targetTime.floatValue != closed) ? closed : open);
		}
	}
}
