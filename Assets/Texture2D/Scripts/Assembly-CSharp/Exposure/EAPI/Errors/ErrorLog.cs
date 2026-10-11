using System.Collections.Generic;

namespace Exposure.EAPI.Errors
{
	public class ErrorLog
	{
		public List<ExposureBaseError> Errors = new List<ExposureBaseError>();

		private string currentStage = "Inititiation";

		public void SetStage(string stage)
		{
			currentStage = stage;
			Errors.Clear();
		}

		public string GetStage()
		{
			return currentStage;
		}
	}
}
