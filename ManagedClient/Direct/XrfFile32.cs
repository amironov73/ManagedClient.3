﻿// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++ and C#: http://www.viva64.com

/* XrfFile32.cs
 */

#region Using directives

using System;
using System.IO;

using JetBrains.Annotations;

#endregion

namespace ManagedClient.Direct
{
    /// <summary>
    /// Файл перекрестных ссылок XRF представляет собой
    /// таблицу ссылок на записи файла документов.
    /// Первая ссылка соответствует записи файла документов
    /// с номером 1, вторая – 2  и тд.
    /// </summary>
    public sealed class XrfFile32
        : IDisposable
    {
        #region Constants

        /// <summary>
        /// Block size.
        /// </summary>
        public const int XrfBlockSize = 512;

        /// <summary>
        /// Block capacity.
        /// </summary>
        public const int XrfBlockCapacity = 127;

        #endregion

        #region Properties

        /// <summary>
        /// File name.
        /// </summary>
        [NotNull]
        public string FileName { get; private set; }

        #endregion

        #region Construction

        /// <summary>
        /// Constructor.
        /// </summary>
        public XrfFile32
            (
                [NotNull] string fileName
            )
        {
            FileName = fileName;

            _stream = new FileStream
                (
                    fileName,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite
                );
        }

        #endregion

        #region Private members

        [NotNull]
        private readonly Stream _stream;

        private long _GetOffset
            (
                int mfn
            )
        {
            unchecked
            {
                int blockNumber = (mfn - 1) / XrfBlockCapacity;
                int blockOffset = (mfn - 1) % XrfBlockCapacity * 4;
                long result = blockNumber * XrfBlockSize + blockOffset + 4;

                return result;
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Decode the record.
        /// </summary>
        [NotNull]
        public XrfRecord32 Decode
            (
                int value
            )
        {
            unchecked
            {
                RecordStatus status = 0;
                if ((value & 0xFFFFF800) == 0xFFFFF800)
                {
                    status = RecordStatus.PhysicallyDeleted;
                }
                else if (value < 0)
                {
                    status = RecordStatus.LogicallyDeleted;
                }

                int blockNumber = value < 0
                    ? (-(int)(value & 0xFFFFF800) >> 11) - 1
                    : (value & 0x7FFFF800) >> 11;
                int blockOffset = value < 0
                    ? -value & 0x7FF
                    : value & 0x7FF;

                XrfRecord32 result = new XrfRecord32
                {
                    BlockNumber = blockNumber,
                    BlockOffset = blockOffset,
                    AbsoluteOffset = MstRecord32.MstBlockSize * (blockNumber - 1) 
                        + blockOffset,
                    Status = status
                };

                return result;
            }
        }

        /// <summary>
        /// Read the record.
        /// </summary>
        [NotNull]
        public XrfRecord32 ReadRecord
            (
                int mfn
            )
        {
            if (mfn <= 0)
            {
                throw new ArgumentOutOfRangeException("mfn");
            }

            long offset = _GetOffset(mfn);
            if (offset >= _stream.Length)
            {
                throw new ArgumentOutOfRangeException("mfn");
            }

            if (_stream.Seek(offset, SeekOrigin.Begin) != offset)
            {
                throw new IOException();
            }

            //int coded = _stream.ReadInt32Network();
            int coded = _stream.ReadInt32Host();

            return Decode(coded);
        }

        /// <summary>
        /// Write the record.
        /// </summary>
        public void WriteRecord
            (
                [NotNull] XrfRecord32 record
            )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Lock/unlock the record.
        /// </summary>
        public void LockRecord
            (
                int mfn,
                bool flag
            )
        {
            XrfRecord32 record = ReadRecord(mfn);
            if (flag != record.Locked)
            {
                WriteRecord(record);
            }
        }

        #endregion

        #region IDisposable members

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public void Dispose()
        {
            _stream.Dispose();
        }

        #endregion
    }
}
