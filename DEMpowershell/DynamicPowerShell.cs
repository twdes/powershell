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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Security;
using System.Text;
using TecWare.DE.Server;
using TecWare.DE.Stuff;

namespace TecWare.DE
{
	#region -- class DynamicPowerShellProgressArgs ------------------------------------

	/// <summary></summary>
	public sealed class DynamicPowerShellProgressArgs : EventArgs
	{
		private readonly ProgressRecord progressRecord;

		/// <summary></summary>
		/// <param name="progressRecord"></param>
		public DynamicPowerShellProgressArgs(ProgressRecord progressRecord)
		{
			this.progressRecord = progressRecord;
		} // ctor

		/// <summary></summary>
		public string Activity => progressRecord.Activity;
		/// <summary></summary>
		public string CurrentOperation => progressRecord.CurrentOperation;
		/// <summary></summary>
		public string StatusDescription => progressRecord.StatusDescription;
		/// <summary></summary>
		public int PrecentComplete => progressRecord.PercentComplete;
		/// <summary></summary>
		public int SecondsRemaining => progressRecord.SecondsRemaining;
	} // DynamicPowerShellProgressArgs

	#endregion

	#region -- class DynamicPowerShell ------------------------------------------------

	/// <summary></summary>
	public sealed class DynamicPowerShell
	{
		/// <summary></summary>
		public event EventHandler<DynamicPowerShellProgressArgs> Progress;
		/// <summary></summary>
		public event EventHandler ProgressCompleted;

		#region -- class DynamicPsUserInterface ---------------------------------------

		/// <summary></summary>
		private sealed class DynamicPsUserInterface : PSHostUserInterface
		{
			#region -- class ProgressInfo ---------------------------------------------

			private sealed class ProgressInfo
			{
				private readonly Stopwatch stopWatch = Stopwatch.StartNew();
				private readonly StringBuilder statusText = new StringBuilder();

				private string currentActivity = String.Empty;
				private string currentStatusText = String.Empty;

				public ProgressInfo(string activity)
				{
					currentStatusText = activity;
					statusText.AppendLine(activity);
				} // ctor

				public void UpdateActivity(string newActivity)
				{
					if (String.IsNullOrEmpty(newActivity))
						return;

					if (String.Compare(currentActivity, newActivity, StringComparison.OrdinalIgnoreCase) != 0)
					{
						currentActivity = newActivity;
						statusText.AppendLine($">> {currentActivity} <<");
					}
				} // proc UpdateActivity

				public void UpdateStatusText(string newStatusText)
				{
					if (String.IsNullOrEmpty(newStatusText))
						return;

					if (String.Compare(currentStatusText, newStatusText, StringComparison.OrdinalIgnoreCase) != 0)
					{
						currentStatusText = newStatusText;
						statusText.AppendLine(currentStatusText);
					}
				} // proc UpdateStatusText

				public void AppendTime()
					=> statusText.AppendLine($"=== Duration = {stopWatch.ElapsedMilliseconds:N0}ms, {stopWatch.Elapsed} ===");

				public string ProgressText => statusText.ToString();
			} // class ProgressInfo

			#endregion

			private readonly DynamicPsHost host;

			private StringBuilder currentLine = new StringBuilder();
			private Dictionary<long, ProgressInfo> progress = new Dictionary<long, ProgressInfo>();

			#region -- Ctor/Dtor ------------------------------------------------------

			public DynamicPsUserInterface(DynamicPsHost host)
			{
				this.host = host;
			} // ctor

			#endregion

			#region -- Prompt, Readline -----------------------------------------------

			public override Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions)
				=> throw new NotImplementedException();

			public override int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice)
				=> throw new NotImplementedException();

			public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName)
				=> throw new NotImplementedException();

			public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options)
				=> throw new NotImplementedException();

			public override string ReadLine()
				=> throw new NotImplementedException();

			public override SecureString ReadLineAsSecureString()
				=> throw new NotImplementedException();

			#endregion

			#region -- Write ----------------------------------------------------------

			public override void Write(string value)
				=> currentLine.Append(value);

			public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
				=> currentLine.Append(value);

			public override void WriteDebugLine(string message)
				=> host.Log.Info(message);

			public override void WriteErrorLine(string value)
				=> host.Log.Except(value);

			public override void WriteLine()
				=> WriteLine(String.Empty);

			public override void WriteLine(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
				=> WriteLine(value);

			public override void WriteLine(string value)
			{
				currentLine.Append(value);
				if (currentLine.Length > 0)
				{
					host.Log.Info(currentLine.ToString());
					currentLine.Length = 0;
				}
			} // proc WriteLine

			public override void WriteProgress(long sourceId, ProgressRecord record)
			{
				ProgressInfo pi;
				if (record.RecordType == ProgressRecordType.Completed)
				{
					if (progress.TryGetValue(sourceId, out pi))
					{
						pi.AppendTime();
						WriteVerboseLine(pi.ProgressText);
						host.PowerShell.ProgressCompleted?.Invoke(host.PowerShell, EventArgs.Empty);
					}
				}
				else
				{
					if (progress.TryGetValue(sourceId, out pi))
						pi.UpdateActivity(record.Activity);
					else
						progress[sourceId] = pi = new ProgressInfo(record.Activity);

					pi.UpdateStatusText(record.CurrentOperation);

					host.PowerShell.Progress?.Invoke(host.PowerShell, new DynamicPowerShellProgressArgs(record));
				}
			} // proc WriteProgress

			public override void WriteVerboseLine(string message)
				=> host.Log.Info(message);

			public override void WriteWarningLine(string message)
				=> host.Log.Warn(message);

			#endregion

			public override PSHostRawUserInterface RawUI => null;
		} // class DynamicPsUserInterface

		#endregion

		#region -- class DynamicPsHost ------------------------------------------------

		/// <summary></summary>
		private sealed class DynamicPsHost : PSHost, IServiceProvider
		{
			private readonly Guid instanceId;
			private readonly DynamicPowerShell powerShell;
			private readonly DynamicPsUserInterface ui;

			private readonly LoggerProxy log;

			public DynamicPsHost(DynamicPowerShell powershell)
			{
				this.instanceId = Guid.NewGuid();
				this.powerShell = powershell ?? throw new ArgumentNullException("powershell");
				this.ui = new DynamicPsUserInterface(this);

				this.log = LoggerProxy.Create(this.GetService<ILogger>(false), "Powershell");
			} // ctor

			public override void EnterNestedPrompt()
				=> throw new NotSupportedException();

			public override void ExitNestedPrompt()
				=> throw new NotSupportedException();

			public override void NotifyBeginApplication()
				=> log.Info("Begin of application");

			public override void NotifyEndApplication()
				=> log.Info("End of application");

			public override void SetShouldExit(int exitCode)
				=> log.Except("Exit requested: {0}", exitCode);

			public object GetService(Type serviceType)
				=> powerShell.sp?.GetService(serviceType);

			public override Guid InstanceId => instanceId;

			public override string Name
			{
				get
				{
					var host = this.GetService<IDEConfigItem>(false);
					return host == null ? "DynamicHost" : host.Name;
				}
			} // prop Name

			public override CultureInfo CurrentCulture => CultureInfo.CurrentCulture;
			public override CultureInfo CurrentUICulture => CultureInfo.CurrentUICulture;

			public override PSHostUserInterface UI => ui;

			public override Version Version => powerShell.runspace.Version;

			public LoggerProxy Log => log;

			public DynamicPowerShell PowerShell => powerShell;
		} // class DynamicPsHost

		#endregion

		private readonly IServiceProvider sp;
		private readonly DynamicPsHost host;
		private readonly Runspace runspace;

		/// <summary></summary>
		/// <param name="sp"></param>
		public DynamicPowerShell(IServiceProvider sp)
		{
			this.sp = sp;

			this.host = new DynamicPsHost(this);

			// create the runspace
			this.runspace = RunspaceFactory.CreateRunspace(host, InitialSessionState.CreateDefault2());
			runspace.Open();

			// activate verbose
			runspace.SessionStateProxy.SetVariable("VerbosePreference", "Continue");
			//runspace.SessionStateProxy.SetVariable("DebugPreference", "Continue");
		} // ctor

		/// <summary></summary>
		public void Dispose()
			=> runspace.Dispose();

		/// <summary></summary>
		/// <param name="scriptPath"></param>
		public void InvokeScript(string scriptPath)
		{
			// read complete script
			var scriptContent = File.ReadAllText(scriptPath);

			// set path
			runspace.SessionStateProxy.Path.SetLocation(Path.GetDirectoryName(scriptPath));

			// create a pipeline
			using (var pipe = runspace.CreatePipeline())
			{
				pipe.Commands.AddScript(scriptContent);

				pipe.Invoke();
			}
		} // proc InvokeScript
	} // class DynamicPowerShell

	#endregion
}
