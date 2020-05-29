# SIPURI README

+ 说明

```bash
  /// <summary>
    /// Implements the the absoluteURI structure from the SIP RFC (incomplete as at 17 nov 2006, AC).
    /// </summary>
    /// <bnf>
    /// absoluteURI    =  scheme ":" ( hier-part / opaque-part )
    /// hier-part      =  ( net-path / abs-path ) [ "?" query ]
    /// net-path       =  "//" authority [ abs-path ]
    /// abs-path       =  "/" path-segments
    ///
    /// opaque-part    =  uric-no-slash *uric
    /// uric           =  reserved / unreserved / escaped
    /// uric-no-slash  =  unreserved / escaped / ";" / "?" / ":" / "@" / "&" / "=" / "+" / "$" / ","
    /// path-segments  =  segment *( "/" segment )
    /// segment        =  *pchar *( ";" param )
    /// param          =  *pchar
    /// pchar          =  unreserved / escaped / ":" / "@" / "&" / "=" / "+" / "$" / ","
    /// scheme         =  ALPHA *( ALPHA / DIGIT / "+" / "-" / "." )
    /// authority      =  srvr / reg-name
    /// srvr           =  [ [ userinfo "@" ] hostport ]
    /// reg-name       =  1*( unreserved / escaped / "$" / "," / ";" / ":" / "@" / "&" / "=" / "+" )
    /// query          =  *uric
    ///
    /// SIP-URI          =  "sip:" [ userinfo ] hostport uri-parameters [ headers ]
    /// SIPS-URI         =  "sips:" [ userinfo ] hostport uri-parameters [ headers ]
    /// userinfo         =  ( user / telephone-subscriber ) [ ":" password ] "@"
    /// user             =  1*( unreserved / escaped / user-unreserved )
    /// user-unreserved  =  "&" / "=" / "+" / "$" / "," / ";" / "?" / "/"
    /// password         =  *( unreserved / escaped / "&" / "=" / "+" / "$" / "," )
    /// hostport         =  host [ ":" port ]
    /// host             =  hostname / IPv4address / IPv6reference
    /// hostname         =  *( domainlabel "." ) toplabel [ "." ]
    /// domainlabel      =  alphanum / alphanum *( alphanum / "-" ) alphanum
    /// toplabel         =  ALPHA / ALPHA *( alphanum / "-" ) alphanum
    /// IPv4address    =  1*3DIGIT "." 1*3DIGIT "." 1*3DIGIT "." 1*3DIGIT
    /// IPv6reference  =  "[" IPv6address "]"
    /// IPv6address    =  hexpart [ ":" IPv4address ]
    /// hexpart        =  hexseq / hexseq "::" [ hexseq ] / "::" [ hexseq ]
    /// hexseq         =  hex4 *( ":" hex4)
    /// hex4           =  1*4HEXDIG
    /// port           =  1*DIGIT
    ///
    /// The BNF for telephone-subscriber can be found in RFC 2806 [9].  Note,
    /// however, that any characters allowed there that are not allowed in
    /// the user part of the SIP URI MUST be escaped.
    /// 
    /// uri-parameters    =  *( ";" uri-parameter)
    /// uri-parameter     =  transport-param / user-param / method-param / ttl-param / maddr-param / lr-param / other-param
    /// transport-param   =  "transport=" ( "udp" / "tcp" / "sctp" / "tls" / other-transport)
    /// other-transport   =  token
    /// user-param        =  "user=" ( "phone" / "ip" / other-user)
    /// other-user        =  token
    /// method-param      =  "method=" Method
    /// ttl-param         =  "ttl=" ttl
    /// maddr-param       =  "maddr=" host
    /// lr-param          =  "lr"
    /// other-param       =  pname [ "=" pvalue ]
    /// pname             =  1*paramchar
    /// pvalue            =  1*paramchar
    /// paramchar         =  param-unreserved / unreserved / escaped
    /// param-unreserved  =  "[" / "]" / "/" / ":" / "&" / "+" / "$"
    ///
    /// headers         =  "?" header *( "&" header )
    /// header          =  hname "=" hvalue
    /// hname           =  1*( hnv-unreserved / unreserved / escaped )
    /// hvalue          =  *( hnv-unreserved / unreserved / escaped )
    /// hnv-unreserved  =  "[" / "]" / "/" / "?" / ":" / "+" / "$"
    /// </bnf>
    /// <remarks>
    /// Specific parameters for URIs: transport, maddr, ttl, user, method, lr.
    /// </remarks>
```