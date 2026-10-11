namespace Exposure.EAPI.Errors
{
	public class VersionParseError : ExposureBaseError
	{
		public VersionParseError(ExposureModule targetModule)
			: base(targetModule, "Error whilest parsing a version number, check for typos!")
		{
		}
	}
}
