using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TecWare.DE.Server;
using TecWare.DE.Stuff;

namespace TecWare.DE
{
	///////////////////////////////////////////////////////////////////////////////
	/// <summary></summary>
	public class PowerShellCronItem : CronJobItem
	{
		private const string ActivityCategory = "Progress";

		private readonly DynamicPowerShell powerShell;

		private readonly SimpleConfigItemProperty<string> activityProperty;
		private readonly SimpleConfigItemProperty<string> currentOperationProperty;
		private readonly SimpleConfigItemProperty<string> statusDescriptionProperty;
		private readonly SimpleConfigItemProperty<string> progressProperty;

		public PowerShellCronItem(IServiceProvider sp, string name)
			: base(sp, name)
		{
			this.powerShell = new DynamicPowerShell(this);

			powerShell.Progress += PowerShell_Progress;
			powerShell.ProgressCompleted += PowerShell_ProgressCompleted;

			activityProperty = new SimpleConfigItemProperty<string>(this, "tw_cron_psactivity", "1. Activity", ActivityCategory, "Current task in the script.", null, null);
			statusDescriptionProperty = new SimpleConfigItemProperty<string>(this, "tw_cron_psstatus", "2. Status Description", ActivityCategory, "Current status description.", null, null);
			progressProperty = new SimpleConfigItemProperty<string>(this, "tw_cron_psprogres", "3. Progress", ActivityCategory, "Current progress of the current operation.", null, null);
			currentOperationProperty = new SimpleConfigItemProperty<string>(this, "tw_cron_psoperation", "4. Current Operation", ActivityCategory, "Current operation.", null, null);
		} // ctor

		protected override void Dispose(bool disposing)
		{
			activityProperty.Dispose();
			currentOperationProperty.Dispose();
			statusDescriptionProperty.Dispose();
			progressProperty.Dispose();

			if (disposing)
				powerShell.Dispose();
			base.Dispose(disposing);
		} // proc Dispose

		protected override void OnEndReadConfiguration(IDEConfigLoading config)
		{
			base.OnEndReadConfiguration(config);
		} // proc OnEndReadConfiguration

		private void PowerShell_Progress(object sender, DynamicPowerShellProgressArgs e)
		{
			activityProperty.Value = e.Activity;
			currentOperationProperty.Value = e.CurrentOperation;
			statusDescriptionProperty.Value = e.StatusDescription;
			progressProperty.Value = $"{e.PrecentComplete}%, {new TimeSpan(0, 0, 0, e.SecondsRemaining, 0).ToString()} seconds";
    } // event PowerShell_Progress

		private void PowerShell_ProgressCompleted(object sender, EventArgs e)
		{
			activityProperty.Value = null;
			currentOperationProperty.Value = null;
			statusDescriptionProperty.Value = null;
			progressProperty.Value = null;
		} // event PowerShell_ProgressCompleted

		protected override void OnRunJob(CancellationToken cancellation)
		{
			var scriptPath = Config.GetAttribute("file", String.Empty);
			if (!String.IsNullOrEmpty(scriptPath))
			{
				Log.Info("Run script: {0}", scriptPath);
				powerShell.InvokeScript(scriptPath);
			}
		} // proc OnRunJob
	} // class PowerShellCronItem
}
