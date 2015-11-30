using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TecWare.DE.Server;

namespace TecWare.DE
{
	///////////////////////////////////////////////////////////////////////////////
	/// <summary></summary>
	public class PowerShellCronItem : CronJobItem
	{
		public PowerShellCronItem(IServiceProvider sp, string name)
			: base(sp, name)
		{
		} // ctor

		protected override void OnRunJob(CancellationToken cancellation)
		{
		} // proc OnRunJob
	} // class PowerShellCronItem
}
