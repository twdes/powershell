#region -- copyright --
//
// Licensed under the EUPL, Version 1.1 or - as soon they will be approved by the
// European Commission - subsequent versions of the EUPL(the "Licence"); You may
// not use this work except in compliance with the Licence.
//
// You may obtain a copy of the Licence at:
// http://ec.europa.eu/idabc/eupl
//
// Unless required by applicable law or agreed to in writing, software distributed
// under the Licence is distributed on an "AS IS" basis, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, either express or implied. See the Licence for the
// specific language governing permissions and limitations under the Licence.
//
#endregion
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
	/// <summary></summary>
	public class PowerShellCronItem : CronJobItem
	{
		private const string ActivityCategory = "Progress";

		private readonly DynamicPowerShell powerShell;

		private readonly SimpleConfigItemProperty<string> activityProperty;
		private readonly SimpleConfigItemProperty<string> currentOperationProperty;
		private readonly SimpleConfigItemProperty<string> statusDescriptionProperty;
		private readonly SimpleConfigItemProperty<string> progressProperty;

		/// <summary></summary>
		/// <param name="sp"></param>
		/// <param name="name"></param>
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

		/// <summary></summary>
		/// <param name="disposing"></param>
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

		/// <summary></summary>
		/// <param name="cancellation"></param>
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
