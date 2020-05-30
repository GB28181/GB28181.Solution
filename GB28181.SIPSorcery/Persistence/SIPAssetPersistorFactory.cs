// ============================================================================
// FileName: SIPAssetPersistorFactory.cs
//
// Description:
// Creates SIPAssetPersistor objects depending on the storage type specified. This
// class implements the standard factory design pattern in conjunction with the
// SIPAssetPersistor template class.
//
// Author(s):
// Aaron Clauson
//
// History:
// 01 Oct 2008	Aaron Clauson	Created.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//


using System;
using GB28181.App;
using GB28181.Sys;
using GB28181.Logger4Net;
using GB28181.Persistence.XML;
using GB28181.Persistence;

namespace GB28181.Persistence
{
    public static class SIPAssetPersistorFactory<T> where T : class, ISIPAsset, new()
    {
        private static ILog logger = AppState.logger;

        public static SIPAssetPersistor<T> CreateSIPAssetPersistor(StorageTypes storageType, string storageConnectionStr, string filename)
        {
            try
            {
                if (storageType == StorageTypes.XML)
                {
                    if (!storageConnectionStr.EndsWith(@"/"))
                    {
                        storageConnectionStr += @"/";
                    }
                    return new XMLAssetPersistor<T>(storageConnectionStr + filename);
                }
                else if (storageType == StorageTypes.SQLite)
                {
                    return new SQLAssetPersistor<T>(null, storageConnectionStr);
                }
                //else if (storageType == StorageTypes.SQLLinqPostgresql)
                //{
                //    return new SQLAssetPersistor<T>(Npgsql.NpgsqlFactory.Instance, storageConnectionStr);
                //}
                //else if (storageType == StorageTypes.SimpleDBLinq)
                //{
                //    return new SimpleDBAssetPersistor<T>(storageConnectionStr);
                //}
                //else if (storageType == StorageTypes.SQLLinqMSSQL)
                //{
                //    return new MSSQLAssetPersistor<T>(System.Data.SqlClient.SqlClientFactory.Instance, storageConnectionStr);
                //}
                //else if (storageType == StorageTypes.SQLLinqOracle)
                //{
                //    return new SQLAssetPersistor<T>(Oracle.DataAccess.Client.OracleClientFactory.Instance, storageConnectionStr);
                //}
                else
                {
                    throw new ApplicationException(storageType + " is not supported as a CreateSIPAssetPersistor option.");
                }
            }
            catch (Exception excp)
            {
                logger.Error("Exception CreateSIPAssetPersistor for " + storageType + ". " + excp.Message);
                throw;
            }
        }
    }
}
