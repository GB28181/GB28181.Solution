//-----------------------------------------------------------------------------
// Filename: ISIPClientUserAgent.cs
//
// Description: The interface definition for SIP Client User Agents (UAC).
// 
// History:
// 30 Aug 2009	Aaron Clauson	    Created.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//
// Copyright (c) 2009 Aaron Clauson (aaron@sipsorcery.com), SIP Sorcery Ltd, London, UK (www.sipsorcery.com)
// All rights reserved.
//



namespace GB28181.App
{

    public interface ISIPClientUserAgent {

        string Owner { get; }
        string AdminMemberId { get; }
        UACInviteTransaction ServerTransaction { get; }
        SIPDialogue SIPDialogue { get; }
        SIPCallDescriptor CallDescriptor { get; }
        bool IsUACAnswered { get; }

        // Real-time call control properties.
        //string AccountCode { get; set; }
        //decimal ReservedCredit { get; set; }
        //int ReservedSeconds { get; set; }
        //decimal Rate { get; set; }

        event SIPCallResponseDelegate CallTrying;
        event SIPCallResponseDelegate CallRinging;
        event SIPCallResponseDelegate CallAnswered;
        event SIPCallFailedDelegate CallFailed;

      //  void Call(SIPCallDescriptor sipCallDescriptor);
        void Cancel();
        void Update(CRMHeaders crmHeaders);
    }
}
