//-----------------------------------------------------------------------------
// Filename: IceConnectionStatesEnum.cs
//
// Description: An enumeration of the different ICE connection states a WebRTC peer can have.
//
// History:
// 03 Mar 2016	Aaron Clauson	Created.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//


namespace GB28181.Net
{
    public enum IceConnectionStatesEnum
    {
        None = 0,
        Gathering = 1,
        GatheringComplete = 2,
        Connecting = 3,
        Connected = 4,
        Closed = 5
    }
}
