//-----------------------------------------------------------------------------
// Filename: GBSIPConstants.cs
//
// Description: SIP constants.
// 
// History:
// 17 Sep 2005	Aaron Clauson	Created.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//


using System;
using System.Collections.Generic;
using SIPSorcery.Sys;



namespace GB28181
{


    public static class GBSIPConstants
    {

        public const int SIP_MAXIMUM_UDP_SEND_LENGTH = 1275;				// Any SIP messages over this size should be prevented from using a UDP transport.
        public const string SIP_USERAGENT_STRING = "SipOrg/1.0";
        public const string SIP_SERVER_STRING = "SipOrg/1.0";


        // Custom SIP headers to allow proxy to communicate network info to internal servers.
        public const string SIP_HEADER_PROXY_RECEIVEDON = "Proxy-ReceivedOn";
        public const string SIP_HEADER_PROXY_RECEIVEDFROM = "Proxy-ReceivedFrom";
        public const string SIP_HEADER_PROXY_SENDFROM = "Proxy-SendFrom";

        // Custom SIP headers to interact with the SIP Sorcery switchboard.
        public const string SIP_HEADER_SWITCHBOARD_ORIGINAL_CALLID = "Switchboard-OriginalCallID";
        //public const string SIP_HEADER_SWITCHBOARD_CALLER_DESCRIPTION = "Switchboard-CallerDescription";
        public const string SIP_HEADER_SWITCHBOARD_LINE_NAME = "Switchboard-LineName";
        //public const string SIP_HEADER_SWITCHBOARD_ORIGINAL_FROM = "Switchboard-OriginalFrom";
        //public const string SIP_HEADER_SWITCHBOARD_FROM_CONTACT_URL = "Switchboard-FromContactURL";
        public const string SIP_HEADER_SWITCHBOARD_OWNER = "Switchboard-Owner";
        //public const string SIP_HEADER_SWITCHBOARD_ORIGINAL_TO = "Switchboard-OriginalTo";
        public const string SIP_HEADER_SWITCHBOARD_TERMINATE = "Switchboard-Terminate";
        //public const string SIP_HEADER_SWITCHBOARD_TOKEN = "Switchboard-Token";
        //public const string SIP_HEADER_SWITCHBOARD_TOKENREQUEST = "Switchboard-TokenRequest";

        // Custom SIP headers for CRM integration.
        public const string SIP_HEADER_CRM_PERSON_NAME = "CRM-PersonName";
        public const string SIP_HEADER_CRM_COMPANY_NAME = "CRM-CompanyName";
        public const string SIP_HEADER_CRM_PICTURE_URL = "CRM-PictureURL";

    }







}