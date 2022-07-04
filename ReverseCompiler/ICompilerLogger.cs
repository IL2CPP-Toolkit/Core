using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Il2CppToolkit.ReverseCompiler
{
	public interface ICompilerLogger
	{
		void LogInfo(string message);
		void LogMessage(string message);
		void LogError(string message);
		void LogWarning(string message);
	}
}
