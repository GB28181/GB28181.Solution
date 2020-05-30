// ============================================================================
// FileName: AssStreamState.cs
//
// Description:
//  Holds application configuration information.
//
// Author(s):
//	Aaron Clauson
//
// History:
// 04 MAy 2007	Aaron Clauson	Created.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//

using GB28181.Sys;
using System;
using GB28181.Logger4Net;

namespace GB28181.Net
{
    /// <summary>
    /// This class maintains static application configuration settings that can be used by all classes within
    /// the AppDomain. This class is the one stop shop for retrieving or accessing application configuration settings.
    /// </summary> 
    public class AssemblyStreamState
	{
		public const string LOGGER_NAME = "rtsp";

	    public static ILog logger = null;

		static AssemblyStreamState()
		{
			try
			{
				// Configure logging.
				logger = AppState.GetLogger(LOGGER_NAME);
			}
			catch(Exception excp)
			{
                Console.WriteLine("Exception AssStreamState: " + excp.Message);
			}
		}
	}
}