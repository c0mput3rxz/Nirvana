﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine.Utilities;
using SAUtils.DataStructures;
using SAUtils.InputFileParsers;
using SAUtils.InputFileParsers.IntermediateAnnotation;
using VariantAnnotation.DataStructures.CompressedSequence;
using VariantAnnotation.DataStructures.SupplementaryAnnotations;
using VariantAnnotation.FileHandling;
using VariantAnnotation.FileHandling.SA;
using VariantAnnotation.FileHandling.SupplementaryAnnotations;
using VariantAnnotation.Interface;
using VariantAnnotation.Utilities;


namespace SAUtils.MergeInterimTsvs
{
    public class MergeInterimTsvs
    {
        private List<SaTsvReader> _tsvReaders;
        private List<IntervalTsvReader> _intervalReaders;
        private SaMiscellaniesReader _miscReader;
        private readonly List<InterimSaHeader> _interimSaHeaders;
        private readonly List<InterimIntervalHeader> _intervalHeaders;
        private readonly string _outputDirectory;
        private readonly GenomeAssembly _genomeAssembly;
        private readonly ChromosomeRenamer _chromosomeRenamer;
        private List<string> _allRefNames;

        /// <summary>
        /// constructor
        /// </summary>
        public MergeInterimTsvs(List<string> annotationFiles, List<string> intervalFiles,string miscFile, string compressedReference, string outputDirectory, List<string> chrWhiteList = null)
        {
            _outputDirectory       = outputDirectory;
            var compressedSequence = new CompressedSequence();
            // ReSharper disable once UnusedVariable
            var reader             = new CompressedSequenceReader(FileUtilities.GetReadStream(compressedReference), compressedSequence);
            _chromosomeRenamer     = compressedSequence.Renamer;
            _genomeAssembly        = compressedSequence.GenomeAssembly;
            _interimSaHeaders      = new List<InterimSaHeader>();
            _intervalHeaders       = new List<InterimIntervalHeader>();
            _allRefNames           = new List<string>();

            var headers = new List<InterimHeader>();
            SetSaTsvReaders(annotationFiles, headers);
            SetIntervalReaders(intervalFiles, headers);
            SetMiscTsvReader(miscFile);
            DisplayDataSources(headers);

            SetChrWhiteList(chrWhiteList);
            CheckAssemblyConsistancy();
        }

        private void DisplayDataSources(List<InterimHeader> headers)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Data sources:\n");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Name                     Version       Release Date          Misc");
            Console.WriteLine("=======================================================================");
            Console.ResetColor();

            foreach (var header in headers.OrderBy(h => h.GetDataSourceVersion().Name))
            {
                Console.WriteLine(header);
            }

            Console.WriteLine();
        }

        private void SetChrWhiteList(List<string> chrWhiteList)
        {
            if (chrWhiteList != null)
            {
                Console.WriteLine("Creating SA for the following chromosomes only:");
                foreach (var refSeq in chrWhiteList)
                {
                    InputFileParserUtilities.ChromosomeWhiteList.Add(_chromosomeRenamer.GetEnsemblReferenceName(refSeq));
                    Console.Write(refSeq + ",");
                }
                Console.WriteLine();
            }
            else InputFileParserUtilities.ChromosomeWhiteList = null;
        }

        private List<IEnumerator<InterimInterval>> GetIntervalEnumerators(string refName)
        {
            if (_intervalReaders == null) return null;

            var interimIntervalEnumerators = new List<IEnumerator<InterimInterval>>();
            foreach (var intervalReader in _intervalReaders)
            {
                var dataEnumerator = intervalReader.GetEnumerator(refName);
                if (!dataEnumerator.MoveNext()) continue;

                interimIntervalEnumerators.Add(dataEnumerator);
            }
            return interimIntervalEnumerators;
        }



        private IEnumerator<IInterimSaItem> GetMiscEnumerator(string refName)
        {
            if (_miscReader == null) return null;

            var dataEnumerator = _miscReader.GetEnumerator(refName);
            if (!dataEnumerator.MoveNext()) return null;
            return dataEnumerator;
        }

        private List<IEnumerator<IInterimSaItem>> GetSaEnumerators(string refName)
        {
            var saItemsList = new List<IEnumerator<IInterimSaItem>>();
            if (_tsvReaders == null) return saItemsList;
            foreach (var tsvReader in _tsvReaders)
            {
                var dataEnumerator = tsvReader.GetEnumerator(refName);
                if (!dataEnumerator.MoveNext()) continue;

                saItemsList.Add(dataEnumerator);
            }

            return saItemsList;
        }

        private void SetIntervalReaders(List<string> intervalFiles, List<InterimHeader> headers)
        {
            if (intervalFiles == null) return;

            _intervalReaders = new List<IntervalTsvReader>(intervalFiles.Count);
            foreach (var fileName in intervalFiles)
            {
                var intervalReader = new IntervalTsvReader(new FileInfo(fileName));

                var header = intervalReader.GetHeader();
                if (header == null) throw new InvalidDataException("Data file lacks version information!!");
                headers.Add(header);
                _intervalHeaders.Add(header);

                _allRefNames.AddRange(intervalReader.GetAllRefNames());
                _intervalReaders.Add(intervalReader);
            }
        }

        private void SetSaTsvReaders(List<string> annotationFiles, List<InterimHeader> headers)
        {
            if (annotationFiles == null) return;
            _tsvReaders = new List<SaTsvReader>(annotationFiles.Count);

            foreach (var fileName in annotationFiles)
            {
                var tsvReader = new SaTsvReader(new FileInfo(fileName));

                var header = tsvReader.GetHeader();
                if (header == null) throw new InvalidDataException("Data file lacks version information!!");
                headers.Add(header);
                _interimSaHeaders.Add(header);

                _allRefNames.AddRange(tsvReader.GetAllRefNames());
                _tsvReaders.Add(tsvReader);
            }

            _allRefNames = _allRefNames.Distinct().ToList();
        }

        private void SetMiscTsvReader(string miscFile)
        {
            if(string.IsNullOrEmpty(miscFile)) return;
            
            _miscReader = new SaMiscellaniesReader(new FileInfo(miscFile));
            _allRefNames.AddRange(_miscReader.GetAllRefNames());
            
        
        }

        private void CheckAssemblyConsistancy()
        {
            var assembly = GenomeAssembly.Unknown;
            if (_interimSaHeaders != null && _interimSaHeaders.Count > 0)
            {
                assembly = _interimSaHeaders[0].GenomeAssembly;

                for (int i = 1; i < _interimSaHeaders.Count; i++)
                    if (_interimSaHeaders[i].GenomeAssembly != assembly)
                        throw new InvalidDataException($"ERROR: The genome assembly for all data sources should be the same. Found {_interimSaHeaders[i].GenomeAssembly} and {_interimSaHeaders[i + 1].GenomeAssembly} and {assembly}");
            }

            if (_intervalHeaders != null && _intervalHeaders.Count > 0)
            {
                if (assembly == GenomeAssembly.Unknown)//there were no interim SA headers
                    assembly = _intervalHeaders[0].GenomeAssembly;

                for (int i = 0; i < _intervalHeaders.Count; i++)
                    if (_intervalHeaders[i].GenomeAssembly != assembly)
                        throw new InvalidDataException($"ERROR: The genome assembly for all data sources should be the same. Found {_intervalHeaders[i].GenomeAssembly} and {_intervalHeaders[i + 1].GenomeAssembly} and {assembly}");
            }

        }

        public void Merge()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("SA File Creation:\n");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Name                     Annotations Intervals RefMinors Creation Time");
            Console.WriteLine("=======================================================================================");
            Console.ResetColor();

            _allRefNames = _allRefNames.Distinct().ToList();
            Parallel.ForEach(_allRefNames, new ParallelOptions { MaxDegreeOfParallelism = 4 }, MergeChrom);

            //foreach (var refName in _allRefNames)
            //{
            //     if (refName !="1") continue;
            //    MergeChrom(refName);
            //}
        }

        private void MergeChrom(string refName)
        {
            var creationBench = new Benchmark();
            var currentChrAnnotationCount = 0;
            int refMinorCount;            

            var iInterimSaItemsList = GetSaEnumerators(refName);
            var miscEnumerator = GetMiscEnumerator(refName);
            if(miscEnumerator!=null)
                iInterimSaItemsList.Add(miscEnumerator);

            var ucscRefName = _chromosomeRenamer.GetUcscReferenceName(refName);
            var dataSourceVersions = GetDataSourceVersions(_interimSaHeaders, _intervalHeaders);

            var header = new SupplementaryAnnotationHeader(ucscRefName, DateTime.Now.Ticks,
                SupplementaryAnnotationCommon.DataVersion, dataSourceVersions, _genomeAssembly);

            var interimIntervalEnumerators = GetIntervalEnumerators(refName);
            var intervals = GetIntervals(interimIntervalEnumerators).OrderBy(x => x.Start).ThenBy(x => x.End).ToList();

            var smallVariantIntervals = GetSpecificIntervals(ReportFor.SmallVariants, intervals);
            var svIntervals           = GetSpecificIntervals(ReportFor.StructuralVariants, intervals);
            var allVariantsIntervals  = GetSpecificIntervals(ReportFor.AllVariants, intervals);

            var saPath = Path.Combine(_outputDirectory, $"{ucscRefName}.nsa");

            using (var stream        = FileUtilities.GetCreateStream(saPath))
            using (var idxStream     = FileUtilities.GetCreateStream(saPath + ".idx"))
            using (var blockSaWriter = new SaWriter(stream, idxStream, header, smallVariantIntervals, svIntervals, allVariantsIntervals))
            {
                InterimSaPosition currPosition;
                while ((currPosition = GetNextInterimPosition(iInterimSaItemsList)) != null)
                {
                    var saPosition = currPosition.Convert();
                    blockSaWriter.Write(saPosition, currPosition.Position, currPosition.IsReferenceMinor);
                    currentChrAnnotationCount++;
                }

                refMinorCount = blockSaWriter.RefMinorCount;
            }

            double lookupsPerSecond;
            Console.WriteLine($"{ucscRefName,-23}  {currentChrAnnotationCount,10:n0}   {intervals.Count,6:n0}    {refMinorCount,6:n0}   {creationBench.GetElapsedIterationTime(currentChrAnnotationCount, "variants", out lookupsPerSecond)}");
        }

        private static List<IInterimInterval> GetSpecificIntervals(ReportFor reportFor, IEnumerable<IInterimInterval> intervals)
        {
            return intervals.Where(interval => interval.ReportingFor == reportFor).ToList();
        }

        private static IEnumerable<IDataSourceVersion> GetDataSourceVersions(List<InterimSaHeader> interimSaHeaders,
            List<InterimIntervalHeader> intervalHeaders)
        {
            var versions = new List<IDataSourceVersion>();

            foreach (var header in interimSaHeaders)
            {
                var version = header.GetDataSourceVersion();
                versions.Add(version);
            }

            foreach (var header in intervalHeaders)
            {
                var version = header.GetDataSourceVersion();
                versions.Add(version);
            }

            return versions;
        }

        private List<InterimInterval> GetIntervals(List<IEnumerator<InterimInterval>> interimIntervalEnumerators)
        {
            var intervals = new List<InterimInterval>();
            if (interimIntervalEnumerators == null || interimIntervalEnumerators.Count == 0) return intervals;

            foreach (var intervalEnumerator in interimIntervalEnumerators)
            {
                InterimInterval currInterval;
                while ((currInterval = intervalEnumerator.Current) != null)
                {
                    intervals.Add(currInterval);
                    if (intervalEnumerator.MoveNext()) continue;
                    break;
                }
            }

            return intervals;
        }

        private InterimSaPosition GetNextInterimPosition(List<IEnumerator<IInterimSaItem>> interimSaItemsList)
        {
            var minItems = GetMinItems(interimSaItemsList);
            if (minItems == null) return null;

            var interimSaPosition = new InterimSaPosition();
            interimSaPosition.AddSaItems(minItems);

            return interimSaPosition;
        }

        private List<IInterimSaItem> GetMinItems(List<IEnumerator<IInterimSaItem>> interimSaItemsList)
        {
            if (interimSaItemsList.Count == 0) return null;

            var minItem    = GetMinItem(interimSaItemsList);
            var minItems   = new List<IInterimSaItem>();
            var removeList = new List<IEnumerator<IInterimSaItem>>();

            foreach (var saEnumerator in interimSaItemsList)
            {
                if (minItem.CompareTo(saEnumerator.Current) < 0) continue;

                while (minItem.CompareTo(saEnumerator.Current) == 0)
                {
                    minItems.Add(saEnumerator.Current);
                    if (saEnumerator.MoveNext()) continue;
                    removeList.Add(saEnumerator);
                    break;
                }
            }

            RemoveEnumerators(removeList, interimSaItemsList);
            return minItems.Count == 0 ? null : minItems;
        }

        private IInterimSaItem GetMinItem(List<IEnumerator<IInterimSaItem>> interimSaItemsList)
        {
            var minItem = interimSaItemsList[0].Current;
            foreach (var saEnumerator in interimSaItemsList)
            {
                if (minItem.CompareTo(saEnumerator.Current) > 0)
                    minItem = saEnumerator.Current;
            }
            return minItem;
        }

        private void RemoveEnumerators(List<IEnumerator<IInterimSaItem>> removeList, List<IEnumerator<IInterimSaItem>> interimSaItemsList)
        {
            if (removeList.Count == 0) return;

            foreach (var enumerator in removeList)
            {
                interimSaItemsList.Remove(enumerator);
            }
        }
    }
}
