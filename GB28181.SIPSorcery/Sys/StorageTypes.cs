using System;
using GB28181.Logger4Net;

//SIPSorcery.Sys中有相同的类型但是不支持SQLite，所以扩展
namespace GB28181.Sys
{
    public enum StorageTypes
    {
        Unknown,
        MSSQL,
        Postgresql,
        MySQL,
        Oracle,
        XML,
        DBLinqMySQL,
        DBLinqPostgresql,
        SimpleDBLinq,
        SQLLinqMySQL,
        SQLLinqPostgresql,
        SQLLinqMSSQL,
        SQLLinqOracle,
        SQLite
    }

    public class StorageTypesConverter
    {
        private static ILog logger = AppState.logger;

        public static StorageTypes GetStorageType(string storageType)
        {
            try
            {
                return (StorageTypes)Enum.Parse(typeof(StorageTypes), storageType, true);
            }
            catch
            {
                logger.Error("StorageTypesConverter " + storageType + " unknown.");
                return StorageTypes.Unknown;
            }
        }
    }
}
