// ============================================================================
// FileName: SIPAssetPersistor.cs
//
// Description:
// Base class for retrieving and persisting SIP asset objects from a persistent 
// data store such as a relational database or XML file.
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
using System.Collections.Generic;
using System.Linq.Expressions;
using GB28181.App;
using GB28181.Sys;
using GB28181.Logger4Net;
using System.Data;
using System.Data.Common;


namespace GB28181.Persistence
{
    public delegate T SIPAssetGetFromDirectQueryDelegate<T>(string sqlQuery, params IDbDataParameter[] sqlParameters);

    public class SIPAssetPersistor<T>
    {
        protected static ILog logger = AppState.logger;

        protected DbProviderFactory m_dbProviderFactory;
        protected string m_dbConnectionStr;
        protected ObjectMapper<T> m_objectMapper;

        public virtual event SIPAssetDelegate<T> Added;
        public virtual event SIPAssetDelegate<T> Updated;
        public virtual event SIPAssetDelegate<T> Deleted;
        public virtual event SIPAssetsModifiedDelegate Modified;

        public virtual T Add(T asset)
        {
            Added?.Invoke(asset);
            throw new NotImplementedException("Method " + System.Reflection.MethodBase.GetCurrentMethod().Name + " in " + System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString() + " not implemented.");
        }

        public virtual List<T> Add(List<T> assets)
        {
            throw new NotImplementedException("Method " + System.Reflection.MethodBase.GetCurrentMethod().Name + " in " + System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString() + " not implemented.");
        }

        public virtual T Update(T asset)
        {
            Updated?.Invoke(asset);
            throw new NotImplementedException("Method " + System.Reflection.MethodBase.GetCurrentMethod().Name + " in " + System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString() + " not implemented.");
        }

        public virtual void UpdateProperty(Guid id, string propertyName, object value)
        {
            Modified.Invoke();
            throw new NotImplementedException("Method " + System.Reflection.MethodBase.GetCurrentMethod().Name + " in " + System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString() + " not implemented.");
        }

        public virtual void IncrementProperty(Guid id, string propertyName)
        {
            throw new NotImplementedException("Method " + System.Reflection.MethodBase.GetCurrentMethod().Name + " in " + System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString() + " not implemented.");
        }

        public virtual void DecrementProperty(Guid id, string propertyName)
        {
            throw new NotImplementedException("Method " + System.Reflection.MethodBase.GetCurrentMethod().Name + " in " + System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString() + " not implemented.");
        }


        public virtual void Delete()
        {
      
            throw new NotImplementedException("Method " + System.Reflection.MethodBase.GetCurrentMethod().Name + " in " + System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString() + " not implemented.");
        }
        public virtual void Delete(T asset)
        {
            Deleted?.Invoke(asset);
            throw new NotImplementedException("Method " + System.Reflection.MethodBase.GetCurrentMethod().Name + " in " + System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString() + " not implemented.");
        }

        public virtual void Delete(Expression<Func<T, bool>> where)
        {
            throw new NotImplementedException("Method " + System.Reflection.MethodBase.GetCurrentMethod().Name + " in " + System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString() + " not implemented.");
        }

        public virtual T Get(Guid id)
        {
            throw new NotImplementedException("Method " + System.Reflection.MethodBase.GetCurrentMethod().Name + " in " + System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString() + " not implemented.");
        }

        public virtual object GetProperty(Guid id, string propertyName)
        {
            throw new NotImplementedException("Method " + System.Reflection.MethodBase.GetCurrentMethod().Name + " in " + System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString() + " not implemented.");
        }

        public virtual int Count(Expression<Func<T, bool>> where)
        {
            throw new NotImplementedException("Method " + System.Reflection.MethodBase.GetCurrentMethod().Name + " in " + System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString() + " not implemented.");
        }

        public virtual T Get(Expression<Func<T, bool>> where)
        {
            throw new NotImplementedException("Method " + System.Reflection.MethodBase.GetCurrentMethod().Name + " in " + System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString() + " not implemented.");
        }

        public virtual List<T> Get(Expression<Func<T, bool>> where, string orderByField, int offset, int count)
        {
            throw new NotImplementedException("Method " + System.Reflection.MethodBase.GetCurrentMethod().Name + " in " + System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString() + " not implemented.");
        }

        public virtual List<T> Get()
        {
            throw new NotImplementedException("Method " + System.Reflection.MethodBase.GetCurrentMethod().Name + " in " + System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString() + " not implemented.");
        }

        protected DbParameter GetParameter(DbProviderFactory dbProviderFactory, MetaDataMember member, object parameterValue, string parameterName)
        {
            DbParameter dbParameter = dbProviderFactory.CreateParameter();
            dbParameter.ParameterName = parameterName;
            //dbParameter.DbType = EntityTypeConversionTable.LookupDbType(member.DbType);

            //if (parameterValue == null)
            //{
            //    dbParameter.Value = null;
            //}
            //else if (member.Type == typeof(DateTimeOffset) || member.Type == typeof(Nullable<DateTimeOffset>))
            //{
            //    dbParameter.Value = ((DateTimeOffset)parameterValue).ToString("o");
            //}
            //else
            //{
            //    dbParameter.Value = parameterValue;
            //}

            return dbParameter;
        }

        protected void Increment(Guid id, string propertyName)
        {
            try
            {
                MetaDataMember member = m_objectMapper.GetMember(propertyName);
                string commandText = "update " + m_objectMapper.TableName + " set " + propertyName + " = " + propertyName + " + 1 where id = '" + id + "'";
                ExecuteCommand(commandText);
            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPAssetPersistor IncrementProperty (for " + typeof(T).Name + "). " + excp.Message);
                throw;
            }
        }

        protected void Decrement(Guid id, string propertyName)
        {
            try
            {
                MetaDataMember member = m_objectMapper.GetMember(propertyName);
                string commandText = "update " + m_objectMapper.TableName + " set " + propertyName + " = " + propertyName + "- 1 where id = '" + id + "'";
                ExecuteCommand(commandText);
            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPAssetPersistor DecrementProperty (for " + typeof(T).Name + "). " + excp.Message);
                throw;
            }
        }

        private void ExecuteCommand(string commandText)
        {
            try
            {
                using (IDbConnection connection = m_dbProviderFactory.CreateConnection())
                {
                    connection.ConnectionString = m_dbConnectionStr;
                    connection.Open();

                    IDbCommand command = connection.CreateCommand();

                    command.CommandText = commandText;
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPAssetPersistor ExecuteCommand (for " + typeof(T).Name + "). " + excp.Message);
                throw;
            }
        }

    }
}
