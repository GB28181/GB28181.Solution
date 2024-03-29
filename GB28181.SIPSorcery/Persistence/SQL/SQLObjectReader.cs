﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using GB28181.App;
using GB28181.Logger4Net;
using GB28181.Sys;

namespace GB28181.Persistence
{

    public class SQLObjectReader<T> : IEnumerable<T>, IEnumerable where T : class, ISIPAsset, new()
    {

        private static ILog logger = AppState.logger;
        private Enumerator enumerator;
        private DataSet m_selectResult;
        private SetterDelegate m_setter;

        public SQLObjectReader(DataSet selectResult)
        {
            m_selectResult = selectResult;
        }

        public SQLObjectReader(DataSet selectResult, SetterDelegate setter)
        {
            m_selectResult = selectResult;
            m_setter = setter;
            this.enumerator = new Enumerator(selectResult, setter);
        }

        public IEnumerator<T> GetEnumerator()
        {
            Enumerator e = this.enumerator;

            if (e == null)
            {
                throw new InvalidOperationException("Cannot enumerate more than once");
            }
            this.enumerator = null;
            return e;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public T First()
        {
            T instance = new T();
            instance.Load(m_selectResult.Tables[0].Rows[0]);
            return instance;
        }

        private class Enumerator : IEnumerator<T>, IEnumerator, IDisposable
        {

            private DataSet m_selectResult;
            private SetterDelegate m_setter;
            private int m_selectIndex;
            private T current;

            internal Enumerator(DataSet selectResult, SetterDelegate setter)
            {
                m_selectResult = selectResult;
                m_setter = setter;
            }

            public T Current
            {
                get { return this.current; }
            }

            object IEnumerator.Current
            {
                get { return this.current; }
            }

            public bool MoveNext()
            {

                if (m_selectIndex < m_selectResult.Tables[0].Rows.Count)
                {
                    T instance = new T();
                    instance.Load(m_selectResult.Tables[0].Rows[m_selectIndex]);
                    this.current = instance;
                    m_selectIndex++;
                    return true;
                }
                return false;
            }

            public void Reset() { }

            public void Dispose() { }
        }
    }
}
