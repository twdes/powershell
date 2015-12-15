using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using TecWare.DE.Server;
using TecWare.DE.Stuff;

namespace TecWare.DE
{

	///////////////////////////////////////////////////////////////////////////////
	/// <summary></summary>
	public sealed class DynamicPowerShell
	{
		#region -- class DynamicPsUserInterface -------------------------------------------

		///////////////////////////////////////////////////////////////////////////////
		/// <summary></summary>
		private sealed class DynamicPsUserInterface : PSHostUserInterface
		{
			private readonly DynamicPsHost host;

			private StringBuilder currentLine = new StringBuilder();

			#region -- Ctor/Dtor ------------------------------------------------------------

			public DynamicPsUserInterface(DynamicPsHost host)
			{
				this.host = host;
			} // ctor

			#endregion

			#region -- Prompt, Readline -----------------------------------------------------

			public override Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions)
			{
				throw new NotImplementedException();
			}

			public override int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice)
			{
				throw new NotImplementedException();
			}

			public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName)
			{
				throw new NotImplementedException();
			}

			public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options)
			{
				throw new NotImplementedException();
			}

			public override string ReadLine()
			{
				throw new NotImplementedException();
			}

			public override SecureString ReadLineAsSecureString()
			{
				throw new NotImplementedException();
			}

			#endregion

			#region -- Write ----------------------------------------------------------------

			public override void Write(string value)
			{
				currentLine.Append(value);
			} // proc Write

			public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
			{
				currentLine.Append(value);
			} // proc Write

			public override void WriteDebugLine(string message)
			{
				if (host.VerboseDebug)
					host.Log.Info(message);
			} // proc WriteDebugLine

			public override void WriteErrorLine(string value)
			{
				host.Log.Except(value);
			} // proc WriteErrorLine

			public override void WriteLine(string value)
			{
				currentLine.Append(value);
				host.Log.Info(currentLine.ToString());
				currentLine.Length = 0;
			} // proc WriteLine

			public override void WriteProgress(long sourceId, ProgressRecord record)
			{
			} // proc WriteProgress

			public override void WriteVerboseLine(string message)
			{
				host.Log.Info(message);
			} // proc WriteVerboseLine

			public override void WriteWarningLine(string message)
			{
				host.Log.Warn(message);
			} // proc WriteWarningLine

			#endregion

			public override PSHostRawUserInterface RawUI => null;
		} // class DynamicPsUserInterface

		#endregion

		#region -- class DynamicPsHost ----------------------------------------------------

		///////////////////////////////////////////////////////////////////////////////
		/// <summary></summary>
		private sealed class DynamicPsHost : PSHost, IServiceProvider
		{
			private readonly Guid instanceId;
			private readonly DynamicPowerShell powershell;
			private readonly DynamicPsUserInterface ui;

			private readonly LoggerProxy log;

			public DynamicPsHost(DynamicPowerShell powershell)
			{
				if (powershell == null)
					throw new ArgumentNullException("powershell");

				this.instanceId = Guid.NewGuid();
				this.powershell = powershell;
				this.ui = new DynamicPsUserInterface(this);

				this.log = LoggerProxy.Create(this.GetService<ILogger>(false), "Powershell");
			} // ctor

			public override void EnterNestedPrompt()
			{
				throw new NotSupportedException();
			} // proc EnterNestedPrompt

			public override void ExitNestedPrompt()
			{
				throw new NotSupportedException();
			} // proc ExitNestedPrompt

			public override void NotifyBeginApplication()
			{
				log.Info("Begin of application");
			} // proc NotifyBeginApplication

			public override void NotifyEndApplication()
			{
				log.Info("End of application");
			} // proc NotifyEndApplication

			public override void SetShouldExit(int exitCode)
			{
				log.Except("Exit requested: {0}", exitCode);
			} // proc SetShouldExit

			public object GetService(Type serviceType)
				=> powershell.sp?.GetService(serviceType);

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

			public override Version Version => new Version(1, 0, 0, 0);

			public bool VerboseDebug => false;

			public LoggerProxy Log => log;
		} // class DynamicPsHost

		#endregion

		private readonly IServiceProvider sp;
		private readonly DynamicPsHost host;
		private readonly Runspace runspace;

		public DynamicPowerShell(IServiceProvider sp)
		{
			this.sp = sp;

			this.host = new DynamicPsHost(this);
			this.runspace = RunspaceFactory.CreateRunspace(host, InitialSessionState.CreateDefault());
			runspace.Open();
		} // ctor
		
		public void Dispose()
		{
			runspace.Dispose();
		} // proc Dispose
		
		public void InvokeScript(string scriptPath)
		{
			// read complete script
			string scriptContent = File.ReadAllText(scriptPath);
			
			// create a pipeline
			var pipe = runspace.CreatePipeline();
			pipe.Commands.AddScript(scriptContent);
			pipe.Invoke();
		} // proc InvokeScript
	} // class DynamicPowerShell
}
