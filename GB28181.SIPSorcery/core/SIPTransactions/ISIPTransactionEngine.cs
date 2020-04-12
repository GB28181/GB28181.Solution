
namespace GB28181.SIPSorcery.SIP
{
    public interface ISIPTransactionEngine
    {

        void AddTransaction(SIPTransaction sipTransaction);

        void RemoveExpiredTransactions();
        bool Exists(SIPResponse sipResponse);

        SIPTransaction GetTransaction(string transactionId);

        SIPTransaction GetTransaction(SIPResponse sipResponse);

        SIPTransaction GetTransaction(SIPRequest sipRequest);


    }
}
