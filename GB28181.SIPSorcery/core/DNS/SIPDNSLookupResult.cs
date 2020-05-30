//-----------------------------------------------------------------------------
// Filename: SIPDNSLocation.cs
//
// Description: Used to hold the results of the various DNS lookups required to resolve a SIP hostname.
//
// History:
// 10 Mar 2009	Aaron Clauson	Created.
// 30 May 2020	Edward Chen     Updated.
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.


using System;
using System.Collections.Generic;
using System.Linq;
using SIPSorcery.SIP;

namespace GB28181
{

    public class SIPDNSLookupEndPoint
    {
        public SIPEndPoint LookupEndPoint;
        public int TTL;
        public DateTime ResolvedAt;
        public DateTime FailedAt;
        public string FailureReason;

        public SIPDNSLookupEndPoint(SIPEndPoint sipEndPoint, int ttl)
        {
            LookupEndPoint = sipEndPoint;
            TTL = ttl;
            ResolvedAt = DateTime.Now;
        }
    }

    public class SIPDNSLookupResult
    {
        public SIPURI URI;
        public string LookupError;
        public DateTime? NAPTRTimedoutAt;
        public DateTime? SRVTimedoutAt;
        public DateTime? ATimedoutAt;
        public DateTime Inserted = DateTime.Now;
        public Dictionary<SIPServicesEnum, SIPDNSServiceResult> SIPNAPTRResults;
        public List<SIPDNSServiceResult> SIPSRVResults;
        public List<SIPDNSLookupEndPoint> EndPointResults;
        public bool Pending;                // If an aysnc lookup request is made this will be set to true if no immediate result is available.

        public SIPDNSLookupResult(SIPURI uri)
        {
            URI = uri;
        }

        public SIPDNSLookupResult(SIPURI uri, string lookupError)
        {
            URI = uri;
            LookupError = lookupError;
        }

        /// <summary>
        /// Used when the result is already known such as when the lookup is for an IP address but a DNS lookup
        /// object still needs to be returned.
        /// </summary>
        /// <param name="uri">The URI being looked up.</param>
        /// <param name="resultEndPoint">The known result SIP end point.</param>
        public SIPDNSLookupResult(SIPURI uri, SIPEndPoint resultEndPoint)
        {
            URI = uri;
            EndPointResults = new List<SIPDNSLookupEndPoint>() { new SIPDNSLookupEndPoint(resultEndPoint, Int32.MaxValue) };
        }

        public void AddLookupResult(SIPDNSLookupEndPoint lookupEndPoint)
        {
            //logger.Debug(" adding SIP end point result for " + URI.ToString() + " of " + lookupEndPoint.LookupEndPoint + ".");

            if(EndPointResults == null)
            {
                EndPointResults = new List<SIPDNSLookupEndPoint>() { lookupEndPoint };
            }
            else
            {
                EndPointResults.Add(lookupEndPoint);
            }
        }

        public void AddNAPTRResult(SIPDNSServiceResult sipNAPTRResult)
        {
                if (SIPNAPTRResults == null)
                {
                    SIPNAPTRResults = new Dictionary<SIPServicesEnum, SIPDNSServiceResult>() { { sipNAPTRResult.SIPService, sipNAPTRResult } };
                }
                else
                {
                    SIPNAPTRResults.Add(sipNAPTRResult.SIPService, sipNAPTRResult);
                }
        }

        public void AddSRVResult(SIPDNSServiceResult sipSRVResult)
        {
            if (SIPSRVResults == null)
            {
                SIPSRVResults = new List<SIPDNSServiceResult>() { sipSRVResult };
            }
            else
            {
                SIPSRVResults.Add(sipSRVResult);
            }
        }

        public SIPDNSServiceResult GetNextUnusedSRV()
        {
            if (SIPSRVResults != null && SIPSRVResults.Count > 0)
            {
                return (from srv in SIPSRVResults where srv.EndPointsResolvedAt == null orderby srv.Priority select srv).FirstOrDefault();
            }

            return null;
        }

        public SIPEndPoint GetSIPEndPoint()
        {
            if (EndPointResults != null && EndPointResults.Count > 0)
            {
                return EndPointResults[0].LookupEndPoint;
            }
            else
            {
                return null;
            }
        }
    }
}
