﻿using System;
using System.Collections.Generic;
using System.IO;
using ErrorHandling.Exceptions;
using VariantAnnotation.DataStructures.SupplementaryAnnotations;
using VariantAnnotation.FileHandling.Binary;
using VariantAnnotation.FileHandling.Compression;
using VariantAnnotation.FileHandling.SupplementaryAnnotations;
using VariantAnnotation.Interface;

namespace VariantAnnotation.FileHandling.SA
{
    public class SaReader : ISupplementaryAnnotationReader, IDisposable
    {
        private readonly Stream _stream;
        private readonly ExtendedBinaryReader _reader;

        private readonly MemoryStream _memoryStream;
        private readonly ExtendedBinaryReader _msReader;

        private readonly ISaIndex _index;
        private readonly SaReadBlock _block;
        private long _fileOffset = -1;

        private int _cachedPosition = -1;
        private ISaPosition _cachedSaPosition;

        public IEnumerable<Interval<IInterimInterval>> SmallVariantIntervals { get; }
        public IEnumerable<Interval<IInterimInterval>> SvIntervals { get; }
        public IEnumerable<Interval<IInterimInterval>> AllVariantIntervals { get; }
        public ISupplementaryAnnotationHeader Header { get; }

        /// <summary>
        /// constructor
        /// </summary>
        public SaReader(Stream stream, Stream idxStream)
        {
            _stream = stream;
            _reader = new ExtendedBinaryReader(stream);

            _block = new SaReadBlock(new Zstandard());

            _memoryStream = new MemoryStream(_block.UncompressedBlock, false);
            _msReader     = new ExtendedBinaryReader(_memoryStream);

            _index = SaIndex.Read(idxStream);

            Header                = GetHeader(_reader);
            SmallVariantIntervals = GetIntervals();
            SvIntervals           = GetIntervals();
            AllVariantIntervals   = GetIntervals();
        }

        public bool IsRefMinor(int position) => _index.IsRefMinor(position);

        public void Dispose()
        {
            _msReader.Dispose();
            _reader.Dispose();
            _stream.Dispose();
            _memoryStream.Dispose();
        }

        public static ISupplementaryAnnotationHeader GetHeader(ExtendedBinaryReader reader)
        {
            var header         = reader.ReadAsciiString();
            var dataVersion    = reader.ReadUInt16();
            var schemaVersion  = reader.ReadUInt16();
            var genomeAssembly = (GenomeAssembly)reader.ReadByte();

            if (header != SupplementaryAnnotationCommon.DataHeader ||
                schemaVersion != SupplementaryAnnotationCommon.SchemaVersion)
            {
                throw new UserErrorException($"The header check failed for the supplementary annotation file: ID: exp: {SupplementaryAnnotationCommon.DataHeader} obs: {header}, schema version: exp:{SupplementaryAnnotationCommon.SchemaVersion} obs: {schemaVersion}");
            }

            var creationTimeTicks     = reader.ReadInt64();
            var referenceSequenceName = reader.ReadAsciiString();

            var dataSourceVersions    = new HashSet<IDataSourceVersion>();
            var numDataSourceVersions = reader.ReadOptInt32();
            for (var i = 0; i < numDataSourceVersions; i++) dataSourceVersions.Add(DataSourceVersion.Read(reader));

            var saHeader = new SupplementaryAnnotationHeader(referenceSequenceName, creationTimeTicks, dataVersion,
                dataSourceVersions, genomeAssembly);

            return saHeader;
        }

        private IEnumerable<Interval<IInterimInterval>> GetIntervals()
        {
            var numIntervals = _reader.ReadOptInt32();
            var intervals    = new List<Interval<IInterimInterval>>(numIntervals);

            for (int i = 0; i < numIntervals; i++)
            {
                var interimInterval = new InterimInterval(_reader);
                intervals.Add(new Interval<IInterimInterval>(interimInterval.Start, interimInterval.End, interimInterval));
            }

            return intervals;
        }

        public ISaPosition GetAnnotation(int position)
        {
	        // this is used 5400 times in Mother_chr1.genome.vcf.gz
			if (position == _cachedPosition) return _cachedSaPosition;

            var fileOffset = _index.GetOffset(position);
            if (fileOffset < 0) return null;

            if (fileOffset != _fileOffset) SetFileOffset(fileOffset);

            var blockOffset = _block.GetBlockOffset(position);
            if (blockOffset < 0) return null;

            _cachedSaPosition = GetSaPosition(blockOffset);
            _cachedPosition   = position;

            return _cachedSaPosition;
        }

        private ISaPosition GetSaPosition(int blockOffset)
        {
            _memoryStream.Position = blockOffset;
            return SaPosition.Read(_msReader);
        }

        private void SetFileOffset(long fileOffset)
        {
            _stream.Position = fileOffset;
            _fileOffset      = fileOffset;
            _block.Read(_stream);
        }
    }
}
