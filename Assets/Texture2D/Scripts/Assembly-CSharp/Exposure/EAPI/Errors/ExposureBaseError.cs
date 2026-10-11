using System;

namespace Exposure.EAPI.Errors
{
	public class ExposureBaseError : Exception
	{
		public ExposureModule TargetModule;

		public ExposureBaseError(ExposureModule targetModule, string ErrorMessage = "No message received")
			: base(ErrorMessage)
		{
			TargetModule = targetModule;
		}
	}
}
