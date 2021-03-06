﻿/* BatchRecordReader -- считывание записей большими порциями.
 */

#region Using directives

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using JetBrains.Annotations;

using MoonSharp.Interpreter;

#endregion

namespace ManagedClient
{
    /// <summary>
    /// Считывание записей с сервера большими порциями
    /// для увеличения производительности.
    /// Сами записи отдаются по одной.
    /// </summary>
    [PublicAPI]
    [MoonSharpUserData]
    public sealed class BatchRecordReader
        : IEnumerable<IrbisRecord>
    {
        #region Constants

        /// <summary>
        /// Размер порции по умолчанию.
        /// </summary>
        public const int DefaultBatchSize = 1000;

        #endregion

        #region Events

        /// <summary>
        /// Вызывается во время считывания очередной порции.
        /// </summary>
        public event EventHandler BatchRead;

        /// <summary>
        /// Вызывается при возникновении исключения.
        /// </summary>
        public event EventHandler<ExceptionEventArgs<Exception>> Exception;

        #endregion

        #region Properties

        /// <summary>
        /// Размер порции.
        /// </summary>
        [DefaultValue(DefaultBatchSize)]
        public int BatchSize { get; private set; }

        /// <summary>
        /// ИРБИС-клиент.
        /// </summary>
        public ManagedClient64 Client { get; private set; }

        #endregion

        #region Construction

        /// <summary>
        /// Конструктор.
        /// </summary>
        public BatchRecordReader
            (
                ManagedClient64 client
            )
            : this 
                (
                    client,
                    DefaultBatchSize
                )
        {
        }

        /// <summary>
        /// Конструктор.
        /// </summary>
        public BatchRecordReader
            (
                ManagedClient64 client,
                int batchSize
            )
        {
            if (ReferenceEquals(client, null))
            {
                throw new ArgumentNullException("client");
            }
            if (batchSize < 1)
            {
                throw new ArgumentOutOfRangeException("batchSize");
            }
            Client = client;
            BatchSize = batchSize;
            IEnumerable<int> range = Enumerable.Range(1, Client.GetMaxMfn());
            _InitializePackages(range);
        }

        /// <summary>
        /// Конструктор.
        /// </summary>
        public BatchRecordReader
            (
                ManagedClient64 client,
                string format,
                params object[] args
            )
        {
            if (ReferenceEquals(client, null))
            {
                throw new ArgumentNullException("client");
            }
            if (string.IsNullOrEmpty(format))
            {
                throw new ArgumentNullException("format");
            }
            Client = client;
            BatchSize = DefaultBatchSize;
            int[] found = Client.Search(format, args);
            _InitializePackages(found);
        }

        /// <summary>
        /// Конструктор.
        /// </summary>
        public BatchRecordReader
            (
                ManagedClient64 client,
                int batchSize,
                IEnumerable<int> range
            )
        {
            if (ReferenceEquals(client, null))
            {
                throw new ArgumentNullException("client");
            }
            if (batchSize < 1)
            {
                throw new ArgumentOutOfRangeException("batchSize");
            }
            if (ReferenceEquals(range, null))
            {
                throw new ArgumentNullException("range");
            }
            Client = client;
            BatchSize = batchSize;
            _InitializePackages(range);
        }

        /// <summary>
        /// Конструктор.
        /// </summary>
        public BatchRecordReader
            (
                ManagedClient64 client,
                IEnumerable<int> range
            )
            : this
                (
                    client,
                    DefaultBatchSize,
                    range
                )
        {
        }

        #endregion

        #region Private members

        private IEnumerable<int[]> _packages;

        private void _InitializePackages
            (
                IEnumerable<int> range
            )
        {
            _packages = range.Slice(BatchSize);
        }

        private void _OnBatchRead ()
        {
            EventHandler handler = BatchRead;
            if (handler != null)
            {
                handler ( this, EventArgs.Empty );
            }
        }

        private void _OnException
            (
                Exception ex
            )
        {
            EventHandler<ExceptionEventArgs<Exception>> handler = Exception;
            if (handler != null)
            {
                ExceptionEventArgs<Exception> args = new ExceptionEventArgs<Exception>(ex);
                handler(this, args);
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Считывает все записи сразу.
        /// </summary>
        public List<IrbisRecord> ReadAll()
        {
            List<IrbisRecord> result = new List<IrbisRecord>();

            foreach (IrbisRecord record in this)
            {
                result.Add(record);
            }

            return result;
        }

        #endregion

        #region IEnumerable members

        public IEnumerator<IrbisRecord> GetEnumerator()
        {
            foreach (int[] package in _packages)
            {
                IrbisRecord[] records = null;
                try
                {
                    records = Client.ReadRecords(package);
                    _OnBatchRead();
                }
                catch (Exception ex)
                {
                    _OnException(ex);
                }
                if (records != null)
                {
                    foreach (IrbisRecord record in records)
                    {
                        yield return record;
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
