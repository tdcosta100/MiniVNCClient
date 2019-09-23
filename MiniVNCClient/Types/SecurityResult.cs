using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniVNCClient.Types
{
	public enum SecurityResult : uint
	{
		OK = 0,
		Failed = 1,
		FailedTooManyAttempts = 2
	}
}
