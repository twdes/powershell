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
		private readonly DynamicPowerShell powerShell;

		public PowerShellCronItem(IServiceProvider sp, string name)
			: base(sp, name)
		{
			this.powerShell = new DynamicPowerShell(this);
		} // ctor

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				powerShell.Dispose();
			base.Dispose(disposing);
		} // proc Dispose

		protected override void OnEndReadConfiguration(IDEConfigLoading config)
		{
			base.OnEndReadConfiguration(config);
		} // proc OnEndReadConfiguration

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
