
namespace GB28181
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
