## readme

```bash
    /// <bnf>
    /// Via               =  ( "Via" / "v" ) HCOLON via-parm *(COMMA via-parm)
    /// via-parm          =  sent-protocol LWS sent-by *( SEMI via-params )
    /// via-params        =  via-ttl / via-maddr / via-received / via-branch / via-extension
    /// via-ttl           =  "ttl" EQUAL ttl
    /// via-maddr         =  "maddr" EQUAL host
    /// via-received      =  "received" EQUAL (IPv4address / IPv6address)
    /// via-branch        =  "branch" EQUAL token
    /// via-extension     =  generic-param
    /// sent-protocol     =  protocol-name SLASH protocol-version SLASH transport
    /// protocol-name     =  "SIP" / token
    /// protocol-version  =  token
    /// transport         =  "UDP" / "TCP" / "TLS" / "SCTP" / other-transport
    /// sent-by           =  host [ COLON port ]
    /// ttl               =  1*3DIGIT ; 0 to 255
    /// generic-param     =  token [ EQUAL gen-value ]
    /// gen-value         =  token / host / quoted-string
    /// </bnf>
```