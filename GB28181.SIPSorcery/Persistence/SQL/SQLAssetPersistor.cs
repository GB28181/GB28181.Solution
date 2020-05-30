// ============================================================================
// FileName: SQLAssetPersistor.cs
//
// Description:
// An asset persistor for Amazon's SimpleDB data store. 
//
// Author(s):
// Aaron Clauson
//
// History:
// 24 Oct 2009	Aaron Clauson	Created.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//


using GB28181.Logger4Net;
using GB28181.Persistence;
using GB28181.App;
using GB28181.Sys;
using SIPSorcery.Sys;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace GB28181.Persistence
{
    public class SQLAssetPersistor<T> : SIPAssetPersistor<T> where T : class, ISIPAsset, new()
    {
        public override event SIPAssetDelegate<T> Added;
        public override event SIPAssetDelegate<T> Updated;
        public override event SIPAssetDelegate<T> Deleted;
        public override event SIPAssetsModifiedDelegate Modified;
        //private static ILog logger = AppState.logger;
        //protected DbProviderFactory m_dbProviderFactory;
        //protected string m_dbConnectionStr;
        //protected ObjectMapper<T> m_objectMapper;
        public SQLAssetPersistor(DbProviderFactory factory, string dbConnStr)
        {
            m_dbProviderFactory = factory;
            m_dbConnectionStr = dbConnStr;
            m_objectMapper = new ObjectMapper<T>();
        }

        public override List<T> Add(List<T> assets)
        {
            using IDbConnection connection = m_dbProviderFactory.CreateConnection();
            connection.ConnectionString = m_dbConnectionStr;
            connection.Open();
            using (IDbTransaction trans = connection.BeginTransaction())
            {
                try
                {
                    IDbCommand insertCommand = connection.CreateCommand();
                    insertCommand.Transaction = trans;
                    foreach (var asset in assets)
                    {


                        var insertQuery = new StringBuilder("insert into " + m_objectMapper.TableName + " (");
                        var parametersStr = new StringBuilder("(");
                        List<DbParameter> dbParameters = new List<DbParameter>();

                        int paramNumber = 1;
                        Dictionary<MetaDataMember, object> allPropertyValues = m_objectMapper.GetAllValues(asset);
                        foreach (KeyValuePair<MetaDataMember, object> propertyValue in allPropertyValues)
                        {
                            DbParameter dbParameter = base.GetParameter(m_dbProviderFactory, propertyValue.Key, propertyValue.Value, paramNumber.ToString());
                            insertCommand.Parameters.Add(dbParameter);

                            insertQuery.Append(propertyValue.Key + ",");
                            parametersStr.Append("?" + paramNumber + ",");
                            paramNumber++;
                        }

                        string insertCommandText = insertQuery.ToString().TrimEnd(',') + ") values " + parametersStr.ToString().TrimEnd(',') + ")";

                        //logger.Debug("SQLAssetPersistor insert SQL: " + insertCommandText + ".");

                        insertCommand.CommandText = insertCommandText;
                        insertCommand.ExecuteNonQuery();
                        Added?.Invoke(asset);
                    }
                    trans.Commit();

                }
                catch (Exception excp)
                {
                    trans.Rollback();
                    logger.Error("Exception SQLAssetPersistor Add (for " + typeof(T).Name + "). " + excp.Message);
                    throw;
                }
            }
            return assets;
        }

        public override T Add(T asset)
        {
            using (IDbConnection connection = m_dbProviderFactory.CreateConnection())
            {
                connection.ConnectionString = m_dbConnectionStr;
                connection.Open();
                using (IDbTransaction trans = connection.BeginTransaction())
                {
                    try
                    {

                        IDbCommand insertCommand = connection.CreateCommand();
                        insertCommand.Transaction = trans;
                        StringBuilder insertQuery = new StringBuilder("insert into " + m_objectMapper.TableName + " (");
                        StringBuilder parametersStr = new StringBuilder("(");
                        List<DbParameter> dbParameters = new List<DbParameter>();

                        int paramNumber = 1;
                        Dictionary<MetaDataMember, object> allPropertyValues = m_objectMapper.GetAllValues(asset);
                        foreach (KeyValuePair<MetaDataMember, object> propertyValue in allPropertyValues)
                        {
                            DbParameter dbParameter = base.GetParameter(m_dbProviderFactory, propertyValue.Key, propertyValue.Value, paramNumber.ToString());
                            insertCommand.Parameters.Add(dbParameter);

                            insertQuery.Append(propertyValue.Key + ",");
                            parametersStr.Append("?" + paramNumber + ",");
                            paramNumber++;
                        }

                        string insertCommandText = insertQuery.ToString().TrimEnd(',') + ") values " + parametersStr.ToString().TrimEnd(',') + ")";

                        //logger.Debug("SQLAssetPersistor insert SQL: " + insertCommandText + ".");

                        insertCommand.CommandText = insertCommandText;
                        insertCommand.ExecuteNonQuery();
                        trans.Commit();
                        if (Added != null)
                        {
                            Added(asset);
                        }
                    }
                    catch (Exception excp)
                    {
                        trans.Rollback();
                        logger.Error("Exception SQLAssetPersistor Add (for " + typeof(T).Name + "). " + excp.Message);
                        throw;
                    }
                }
                return asset;
            }
        }

        public override T Update(T asset)
        {
            try
            {
                using (var connection = m_dbProviderFactory.CreateConnection())
                {
                    connection.ConnectionString = m_dbConnectionStr;
                    connection.Open();

                    IDbCommand insertCommand = connection.CreateCommand();

                    IDbCommand updateCommand = connection.CreateCommand();

                    StringBuilder updateQuery = new StringBuilder("update " + m_objectMapper.TableName + " set ");
                    List<DbParameter> dbParameters = new List<DbParameter>();

                    int paramNumber = 1;
                    Dictionary<MetaDataMember, object> allPropertyValues = m_objectMapper.GetAllValues(asset);
                    foreach (KeyValuePair<MetaDataMember, object> propertyValue in allPropertyValues)
                    {
                       // if (!propertyValue.Key.IsPrimaryKey)
                        {
                            DbParameter dbParameter = base.GetParameter(m_dbProviderFactory, propertyValue.Key, propertyValue.Value, paramNumber.ToString());
                            updateCommand.Parameters.Add(dbParameter);

                            updateQuery.Append(propertyValue.Key + "= ?" + paramNumber + ",");
                            paramNumber++;
                        }
                    }

                    string updateCommandText = updateQuery.ToString().TrimEnd(',') + " where id = '" + asset.Id + "'";

                    //logger.Debug("SQLAssetPersistor update SQL: " + updateCommandText + ".");

                    updateCommand.CommandText = updateCommandText;
                    updateCommand.ExecuteNonQuery();

                    if (Updated != null)
                    {
                        Updated(asset);
                    }

                    return asset;
                }
            }
            catch (Exception excp)
            {
                logger.Error("Exception SQLAssetPersistor Update (for " + typeof(T).Name + "). " + excp.Message);
                throw;
            }
          //  return null;
        }

        public override void UpdateProperty(Guid id, string propertyName, object value)
        {
            try
            {
                using IDbConnection connection = m_dbProviderFactory.CreateConnection();
                connection.ConnectionString = m_dbConnectionStr;
                connection.Open();

                IDbCommand updateCommand = connection.CreateCommand();

                MetaDataMember member = m_objectMapper.GetMember(propertyName);
                string parameterName = "1";
                DbParameter dbParameter = base.GetParameter(m_dbProviderFactory, member, value, parameterName);
                updateCommand.Parameters.Add(dbParameter);

                updateCommand.CommandText = "update " + m_objectMapper.TableName + " set " + propertyName + " = ?" + parameterName + " where id = '" + id + "'";
                updateCommand.ExecuteNonQuery();
            }
            catch (Exception excp)
            {
                logger.Error("Exception SQLAssetPersistor UpdateProperty (for " + typeof(T).Name + "). " + excp.Message);
                throw;
            }
        }

        public override void IncrementProperty(Guid id, string propertyName)
        {
            base.Increment(id, propertyName);
        }

        public override void DecrementProperty(Guid id, string propertyName)
        {
            base.Decrement(id, propertyName);
        }

        public override void Delete()
        {

            using (IDbConnection connection = m_dbProviderFactory.CreateConnection())
            {
                connection.ConnectionString = m_dbConnectionStr;
                connection.Open();
                using (IDbTransaction trans = connection.BeginTransaction())
                {
                    try
                    {

                        IDbCommand deleteCommand = connection.CreateCommand();
                        deleteCommand.Transaction = trans;
                        deleteCommand.CommandText = "delete from " + m_objectMapper.TableName;
                        deleteCommand.ExecuteNonQuery();
                        trans.Commit();
                    }
                    catch (Exception excp)
                    {
                        trans.Rollback();
                        logger.Error("Exception SQLAssetPersistor Delete (for " + typeof(T).Name + "). " + excp.Message);
                        throw;
                    }
                }
            }

        }

        public override void Delete(T asset)
        {
            using (IDbConnection connection = m_dbProviderFactory.CreateConnection())
            {
                connection.ConnectionString = m_dbConnectionStr;
                connection.Open();
                using (IDbTransaction trans = connection.BeginTransaction())
                {
                    try
                    {


                        IDbCommand deleteCommand = connection.CreateCommand();
                        deleteCommand.Transaction = trans;
                        deleteCommand.CommandText = "delete from " + m_objectMapper.TableName + " where id = '" + asset.Id + "'";
                        deleteCommand.ExecuteNonQuery();
                        trans.Commit();
                     //   Deleted?.(asset);

                    }
                    catch (Exception excp)
                    {
                        trans.Rollback();
                        logger.Error("Exception SQLAssetPersistor Delete (for " + typeof(T).Name + "). " + excp.Message);
                        throw;
                    }
                }
            }

        }

        public override void Delete(Expression<Func<T, bool>> where)
        {

            SQLQueryProvider sqlQueryProvider = new SQLQueryProvider(m_dbProviderFactory, m_dbConnectionStr, m_objectMapper.TableName, m_objectMapper.SetValue);
            string whereStr = sqlQueryProvider.GetQueryText(where);

            using (IDbConnection connection = m_dbProviderFactory.CreateConnection())
            {
                connection.ConnectionString = m_dbConnectionStr;
                connection.Open();
                using (IDbTransaction trans = connection.BeginTransaction())
                {
                    try
                    {

                        IDbCommand deleteCommand = connection.CreateCommand();
                        deleteCommand.Transaction = trans;
                        deleteCommand.CommandText = "delete from " + m_objectMapper.TableName + " where " + whereStr;
                        deleteCommand.ExecuteNonQuery();
                        trans.Commit();
                    }
                    catch (Exception excp)
                    {
                        trans.Rollback();
                        logger.Error("Exception SQLAssetPersistor Delete (for " + typeof(T).Name + "). " + excp.Message);
                        throw;
                    }
                }
            }

        }

        public override T Get(Guid id)
        {
            try
            {
                using (IDbConnection connection = m_dbProviderFactory.CreateConnection())
                {
                    connection.ConnectionString = m_dbConnectionStr;
                    connection.Open();

                    IDbCommand command = connection.CreateCommand();
                    command.CommandText = "select * from " + m_objectMapper.TableName + " where id = '" + id + "'";
                    IDbDataAdapter adapter = m_dbProviderFactory.CreateDataAdapter();
                    adapter.SelectCommand = command;
                    var resultSet = new DataSet();
                    adapter.Fill(resultSet);

                    if (resultSet != null && resultSet.Tables[0].Rows.Count == 1)
                    {
                        T instance = new T();
                        instance.Load(resultSet.Tables[0].Rows[0]);
                        return instance;
                    }
                    else if (resultSet != null && resultSet.Tables[0].Rows.Count > 1)
                    {
                        throw new ApplicationException("Multiple rows were returned for Get with id=" + id + ".");
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception excp)
            {
                logger.Error("Exception SQLAssetPersistor Get (id) (for " + typeof(T).Name + "). " + excp.Message);
                throw;
            }
        }

        public override object GetProperty(Guid id, string propertyName)
        {
            try
            {
                using (IDbConnection connection = m_dbProviderFactory.CreateConnection())
                {
                    connection.ConnectionString = m_dbConnectionStr;
                    connection.Open();

                    IDbCommand command = connection.CreateCommand();
                    command.CommandText = "select " + propertyName + " from " + m_objectMapper.TableName + " where id = '" + id + "'";

                    return command.ExecuteScalar();
                }
            }
            catch (Exception excp)
            {
                logger.Error("Exception SQLAssetPersistor GetProperty (for " + typeof(T).Name + "). " + excp.Message);
                throw;
            }
        }

        public override int Count(Expression<Func<T, bool>> whereClause)
        {
            try
            {
                SQLQueryProvider sqlQueryProvider = new SQLQueryProvider(m_dbProviderFactory, m_dbConnectionStr, m_objectMapper.TableName, m_objectMapper.SetValue);
                Query<T> assets = new Query<T>(sqlQueryProvider);
                if (whereClause != null)
                {
                    return assets.Where(whereClause).Count();
                }
                else
                {
                    return assets.Count();
                }
            }
            catch (Exception excp)
            {
                logger.Error("Exception SQLAssetPersistor Count (for " + typeof(T).Name + "). " + excp.Message);
                throw;
            }
        }

        public override T Get(Expression<Func<T, bool>> whereClause)
        {
            try
            {
                SQLQueryProvider sqlQueryProvider = new SQLQueryProvider(m_dbProviderFactory, m_dbConnectionStr, m_objectMapper.TableName, m_objectMapper.SetValue);
                Query<T> assets = new Query<T>(sqlQueryProvider);
                IQueryable<T> getList = null;
                if (whereClause != null)
                {
                    getList = from asset in assets.Where(whereClause) select asset;
                }
                else
                {
                    getList = from asset in assets select asset;
                }
                return getList.FirstOrDefault();
            }
            catch (Exception excp)
            {
                string whereClauseStr = (whereClause != null) ? whereClause.ToString() + ". " : null;
                logger.Error("Exception SQLAssetPersistor Get (where) (for " + typeof(T).Name + "). " + whereClauseStr + excp);
                throw;
            }
        }

        public override List<T> Get(Expression<Func<T, bool>> whereClause, string orderByField, int offset, int count)
        {
            try
            {
                SQLQueryProvider sqlQueryProvider = new SQLQueryProvider(m_dbProviderFactory, m_dbConnectionStr, m_objectMapper.TableName, m_objectMapper.SetValue);
                Query<T> assetList = new Query<T>(sqlQueryProvider);
                //IQueryable<T> getList = from asset in assetList.Where(whereClause) orderby orderByField select asset;
                IQueryable<T> getList = null;
                if (whereClause != null)
                {
                    getList = from asset in assetList.Where(whereClause) select asset;
                }
                else
                {
                    getList = from asset in assetList select asset;
                }

                if (!orderByField.IsNullOrBlank())
                {
                    sqlQueryProvider.OrderBy = orderByField;
                }

                if (offset != 0)
                {
                    sqlQueryProvider.Offset = offset;
                }

                if (count != Int32.MaxValue)
                {
                    sqlQueryProvider.Count = count;
                }

                return getList.ToList() ?? new List<T>();
            }
            catch (Exception excp)
            {
                string whereClauseStr = (whereClause != null) ? whereClause.ToString() + ". " : null;
                logger.Error("Exception SQLAssetPersistor Get (list) (for " + typeof(T).Name + "). " + whereClauseStr + excp.Message);
                throw;
            }
        }

        public override List<T> Get()
        {
            try
            {
                SQLQueryProvider sqlQueryProvider = new SQLQueryProvider(m_dbProviderFactory, m_dbConnectionStr, m_objectMapper.TableName, m_objectMapper.SetValue);
                Query<T> assetList = new Query<T>(sqlQueryProvider);
                //IQueryable<T> getList = from asset in assetList.Where(whereClause) orderby orderByField select asset;
                IQueryable<T> getList = null;
                getList = from asset in assetList select asset;


                return getList.ToList() ?? new List<T>();
            }
            catch (Exception excp)
            {
                logger.Error("Exception SQLAssetPersistor Get (list) (for " + excp.Message);
                throw;
            }
        }
    }

    #region Unit testing.

#if UNITTEST

    [TestFixture]
    public class SQLAssetPersistorUnitTest {

        // [Table(Name="table")]
        private class MockSIPAsset : ISIPAsset {

            private Guid m_id;
            public Guid Id {
                get { return m_id; }
                set { m_id = value; }
            }

            public DataTable GetTable() {
                throw new NotImplementedException();
            }

            public void Load(DataRow row) {
                throw new NotImplementedException();
            }

            public Dictionary<Guid, object> Load(System.Xml.XmlDocument dom) {
                throw new NotImplementedException();
            }

            public string ToXML() {
                throw new NotImplementedException();
            }

            public string ToXMLNoParent() {
                throw new NotImplementedException();
            }

            public string GetXMLElementName() {
                throw new NotImplementedException();
            }

            public string GetXMLDocumentElementName() {
                throw new NotImplementedException();
            }
        }
     
        [TestFixtureSetUp]
        public void Init() { }

        [TestFixtureTearDown]
        public void Dispose() { }

        [Test]
        public void SampleTest() {
            Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
        }

        /*[Test]
        public void BuildSingleParameterSelectQueryUnitTest() {
            Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
            SimpleDBAssetPersistor<MockSIPAsset> persistor = new SimpleDBAssetPersistor<MockSIPAsset>(null, null);
            string selectQuery = persistor.BuildSelectQuery("select * from table where inserted < ?1", new SqlParameter("1", DateTime.Now));
            Console.WriteLine(selectQuery);
        }

        [Test]
        public void BuildMultipleParameterSelectQueryUnitTest() {
            Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
            SimpleDBAssetPersistor<MockSIPAsset> persistor = new SimpleDBAssetPersistor<MockSIPAsset>(null, null);
            SqlParameter[] parameters = new SqlParameter[2];
            parameters[0] = new SqlParameter("1", DateTime.Now);
            parameters[1] = new SqlParameter("2", "test");
            string selectQuery = persistor.BuildSelectQuery("select * from table where inserted < ?1 and name = ?2", parameters);
            Console.WriteLine(selectQuery);
        }*/
    }

#endif

    #endregion
}
