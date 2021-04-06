using System;
using System.Collections.Generic;
using WalletWasabi.Fluent.MathNet;

namespace WalletWasabi.Fluent.ViewModels.LineChartDemo
{
	public class XYLineChartViewModel
	{
		private static readonly DateTime TimeOrigin = new DateTime(1899, 12, 31, 0, 0, 0, DateTimeKind.Utc);

		public static double ToDouble(DateTime value)
		{
			var span = value - TimeOrigin;
			return span.TotalDays + 1;
		}

		public XYLineChartViewModel()
		{
			var values = new List<DateTime>()
			{
				new DateTime(2021, 4, 1, 0, 0, 0),
				new DateTime(2021, 4, 1, 1, 0, 0),
				new DateTime(2021, 4, 1, 2, 0, 0),
				new DateTime(2021, 4, 1, 3, 0, 0),
				new DateTime(2021, 4, 1, 4, 0, 0),
			};

			XAxisValues = new();
			XAxisLabels = new();

			foreach (var value in values)
			{
				var d = ToDouble(value);
				XAxisValues.Add(d);
				XAxisLabels.Add(value.ToString());
			}
		}

		public List<double> XAxisValues { get; }

		public List<string> XAxisLabels { get; }

		public List<double> YAxisValues => new()
		{
			4,
			2,
			5,
			8,
			1
		};
	}
}
