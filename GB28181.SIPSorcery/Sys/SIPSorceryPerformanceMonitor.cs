// ============================================================================
// FileName: SIPSorceryPerformanceMonitor.cs
//
// Description:
// Contains helper and configuration functions to allow application wide use of
// Windows performance monitor counters to monitor application metrics.
//
// Author(s):
// Aaron Clauson
//
// History:
// 27 Jul 2010  Aaron Clauson   Created.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//


using GB28181.Logger4Net;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

#if UNITTEST
using NUnit.Framework;
#endif

namespace GB28181.Sys
{
    public class SIPSorceryPerformanceMonitor
    {
        private const string PERFORMANCE_COUNTER_CATEGORY_NAME = "SIPSorcery";

        public const string PROXY_PREFIX = "Proxy";
        public const string REGISTRAR_PREFIX = "Registrar";
        public const string REGISTRATION_AGENT_PREFIX = "RegistrationAgent";

        public const string SIP_TRANSPORT_STUN_REQUESTS_PER_SECOND_SUFFIX = "STUNRequestsPerSecond";
        public const string SIP_TRANSPORT_SIP_REQUESTS_PER_SECOND_SUFFIX = "SIPRequestsPerSecond";
        public const string SIP_TRANSPORT_SIP_RESPONSES_PER_SECOND_SUFFIX = "SIPResponsesPerSecond";
        public const string SIP_TRANSPORT_SIP_BAD_MESSAGES_PER_SECOND_SUFFIX = "SIPBadMessagesPerSecond";

        public const string PROXY_STUN_REQUESTS_PER_SECOND = PROXY_PREFIX + SIP_TRANSPORT_STUN_REQUESTS_PER_SECOND_SUFFIX;
        public const string PROXY_SIP_REQUESTS_PER_SECOND = PROXY_PREFIX + SIP_TRANSPORT_SIP_REQUESTS_PER_SECOND_SUFFIX;
        public const string PROXY_SIP_RESPONSES_PER_SECOND = PROXY_PREFIX + SIP_TRANSPORT_SIP_RESPONSES_PER_SECOND_SUFFIX;
        public const string PROXY_SIP_BAD_MESSAGES_PER_SECOND = PROXY_PREFIX + SIP_TRANSPORT_SIP_BAD_MESSAGES_PER_SECOND_SUFFIX;

        public const string REGISTRAR_STUN_REQUESTS_PER_SECOND = REGISTRAR_PREFIX + SIP_TRANSPORT_STUN_REQUESTS_PER_SECOND_SUFFIX;
        public const string REGISTRAR_SIP_REQUESTS_PER_SECOND = REGISTRAR_PREFIX + SIP_TRANSPORT_SIP_REQUESTS_PER_SECOND_SUFFIX;
        public const string REGISTRAR_SIP_RESPONSES_PER_SECOND = REGISTRAR_PREFIX + SIP_TRANSPORT_SIP_RESPONSES_PER_SECOND_SUFFIX;
        public const string REGISTRAR_SIP_BAD_MESSAGES_PER_SECOND = REGISTRAR_PREFIX + SIP_TRANSPORT_SIP_BAD_MESSAGES_PER_SECOND_SUFFIX;
        public const string REGISTRAR_REGISTRATION_REQUESTS_PER_SECOND = REGISTRAR_PREFIX + "RegistersReceivedPerSecond";

        public const string REGISTRATION_AGENT_STUN_REQUESTS_PER_SECOND = REGISTRATION_AGENT_PREFIX + SIP_TRANSPORT_STUN_REQUESTS_PER_SECOND_SUFFIX;
        public const string REGISTRATION_AGENT_SIP_REQUESTS_PER_SECOND = REGISTRATION_AGENT_PREFIX + SIP_TRANSPORT_SIP_REQUESTS_PER_SECOND_SUFFIX;
        public const string REGISTRATION_AGENT_SIP_RESPONSES_PER_SECOND = REGISTRATION_AGENT_PREFIX + SIP_TRANSPORT_SIP_RESPONSES_PER_SECOND_SUFFIX;
        public const string REGISTRATION_AGENT_SIP_BAD_MESSAGES_PER_SECOND = REGISTRATION_AGENT_PREFIX + SIP_TRANSPORT_SIP_BAD_MESSAGES_PER_SECOND_SUFFIX;
        public const string REGISTRATION_AGENT_REGISTRATIONS_PER_SECOND = REGISTRATION_AGENT_PREFIX + "RegistrationsPerSecond";

        private static ILog logger = AppState.logger;

        private static bool m_sipsorceryCategoryReady = false;
        
        //private static Dictionary<string, PerformanceCounterType> m_counterNames = new Dictionary<string, PerformanceCounterType>()
        //{
          
        //    // Proxy
        //    { PROXY_STUN_REQUESTS_PER_SECOND, PerformanceCounterType.RateOfCountsPerSecond32 },
        //    { PROXY_SIP_REQUESTS_PER_SECOND, PerformanceCounterType.RateOfCountsPerSecond32 },
        //    { PROXY_SIP_RESPONSES_PER_SECOND, PerformanceCounterType.RateOfCountsPerSecond32 },
        //    { PROXY_SIP_BAD_MESSAGES_PER_SECOND, PerformanceCounterType.RateOfCountsPerSecond32 },

        //    // Registrar
        //    { REGISTRAR_STUN_REQUESTS_PER_SECOND, PerformanceCounterType.RateOfCountsPerSecond32 },
        //    { REGISTRAR_SIP_REQUESTS_PER_SECOND, PerformanceCounterType.RateOfCountsPerSecond32 },
        //    { REGISTRAR_SIP_RESPONSES_PER_SECOND, PerformanceCounterType.RateOfCountsPerSecond32 },
        //    { REGISTRAR_SIP_BAD_MESSAGES_PER_SECOND, PerformanceCounterType.RateOfCountsPerSecond32 },
        //    { REGISTRAR_REGISTRATION_REQUESTS_PER_SECOND, PerformanceCounterType.RateOfCountsPerSecond32 },
            
        //    // Registration agent counters.
        //    { REGISTRATION_AGENT_STUN_REQUESTS_PER_SECOND, PerformanceCounterType.RateOfCountsPerSecond32 },
        //    { REGISTRATION_AGENT_SIP_REQUESTS_PER_SECOND, PerformanceCounterType.RateOfCountsPerSecond32 },
        //    { REGISTRATION_AGENT_SIP_RESPONSES_PER_SECOND, PerformanceCounterType.RateOfCountsPerSecond32 },
        //    { REGISTRATION_AGENT_SIP_BAD_MESSAGES_PER_SECOND, PerformanceCounterType.RateOfCountsPerSecond32 },
        //    { REGISTRATION_AGENT_REGISTRATIONS_PER_SECOND, PerformanceCounterType.RateOfCountsPerSecond32 }
        //};

        //private static Dictionary<string, PerformanceCounter> m_counters = new Dictionary<string, PerformanceCounter>();

        static SIPSorceryPerformanceMonitor()
        {
            ThreadPool.QueueUserWorkItem(delegate { CheckCounters(); });
        }
        
        public static bool Initialise()
        {
            CheckCounters();
            return m_sipsorceryCategoryReady;
        }

        public static void IncrementCounter(string counterName)
        {
            IncrementCounter(counterName, 1);
        }

        public static void IncrementCounter(string counterName, int incrementBy)
        {
            //try
            //{
            //    if (m_sipsorceryCategoryReady)
            //    {
            //        if (m_counters.ContainsKey(counterName))
            //        {
            //            m_counters[counterName].IncrementBy(incrementBy);
            //        }
            //        else
            //        {
            //            PerformanceCounter counter = new PerformanceCounter(PERFORMANCE_COUNTER_CATEGORY_NAME, counterName, false);
            //            m_counters.Add(counterName, counter);
            //            counter.IncrementBy(incrementBy);
            //        }
            //    }
            //}
            //catch (Exception excp)
            //{
            //    logger.Error("Exception SIPSorceryPerformanceMonitor IncrementCounter (" + counterName + "). " + excp.Message);
            //}
        }

        private static void CheckCounters()
        {
            //try
            //{
            //    if (!PerformanceCounterCategory.Exists(PERFORMANCE_COUNTER_CATEGORY_NAME))
            //    {
            //        CreateCategory();
            //    }
            //    else
            //    {
            //        foreach (var counter in m_counterNames)
            //        {
            //            if (!PerformanceCounterCategory.CounterExists(counter.Key, PERFORMANCE_COUNTER_CATEGORY_NAME))
            //            {
            //                CreateCategory();
            //                break;
            //            }
            //        }
            //    }

            //    m_sipsorceryCategoryReady = true;
            //}
            //catch (Exception excp)
            //{
            //    logger.Error("Exception SIPSorceryPerformanceMonitor CheckCounters. " + excp.Message);
            //}
        }

        private static void CreateCategory()
        {
            //try
            //{
            //    logger.Debug("SIPSorceryPerformanceMonitor creating " + PERFORMANCE_COUNTER_CATEGORY_NAME + " category.");

            //    if (PerformanceCounterCategory.Exists(PERFORMANCE_COUNTER_CATEGORY_NAME))
            //    {
            //        PerformanceCounterCategory.Delete(PERFORMANCE_COUNTER_CATEGORY_NAME);
            //    }

            //     CounterCreationDataCollection ccdc = new CounterCreationDataCollection();

            //     foreach (var counter in m_counterNames)
            //     {
            //         CounterCreationData counterData = new CounterCreationData();
            //         counterData.CounterType = counter.Value;
            //         counterData.CounterName = counter.Key;
            //         ccdc.Add(counterData);

            //         logger.Debug("SIPSorceryPerformanceMonitor added counter " + counter.Key + ".");
            //     }

            //    PerformanceCounterCategory.Create(PERFORMANCE_COUNTER_CATEGORY_NAME, "SIP Sorcery performance counters", PerformanceCounterCategoryType.SingleInstance, ccdc);
            //}
            //catch (Exception excp)
            //{
            //    logger.Error("Exception SIPSorceryPerformanceMonitor CreateCategory. " + excp.Message);
            //    throw;
            //}
        }

        #region Unit testing.

        #if UNITTEST

		[TestFixture]
		public class SIPDialPlanUnitTest
		{			
			[TestFixtureSetUp]
			public void Init()
			{ }

            [TestFixtureTearDown]
            public void Dispose()
            { }

			[Test]
			public void SampleTest()
			{
				Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);
				
				Assert.IsTrue(true, "True was false.");

				Console.WriteLine("---------------------------------"); 
			}

            [Test]
            public void IncrementCounterUnitTest()
            {
                Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);

                SIPSorceryPerformanceMonitor.IncrementCounter("UnitTestCounter");

                Console.WriteLine("---------------------------------");
            }
        }

        #endif

        #endregion

    }
}
