﻿using System.Collections.Generic;
using CacheUtils.DataDumperImport.Parser;
using CacheUtils.DataDumperImport.Utilities;
using ErrorHandling.Exceptions;

namespace CacheUtils.DataDumperImport.Import
{
    internal static class PairGenomic
    {
        #region members

        private const string GenomicKey = "GENOME";

        private static readonly HashSet<string> KnownKeys;

        #endregion

        // constructor
        static PairGenomic()
        {
            KnownKeys = new HashSet<string>
            {
                GenomicKey
            };
        }

        /// <summary>
        /// parses the relevant data from each pair genomic object
        /// </summary>
        public static DataStructures.PairGenomic Parse(ObjectValue objectValue, ImportDataStore dataStore)
        {
            var pairGenomic = new DataStructures.PairGenomic();

            foreach (AbstractData ad in objectValue)
            {
                // sanity check: make sure we know about the keys are used for
                if (!KnownKeys.Contains(ad.Key))
                {
                    throw new GeneralException($"Encountered an unknown key in the pair genomic object: {ad.Key}");
                }

                // handle each key
                switch (ad.Key)
                {
                    case GenomicKey:
                        var genomicNode = ad as ListObjectKeyValue;
                        if (genomicNode != null)
                        {
                            pairGenomic.Genomic = MapperPair.ParseList(genomicNode.Values, dataStore);
                        }
                        else if (DumperUtilities.IsUndefined(ad))
                        {
                            pairGenomic.Genomic = null;
                        }
                        else
                        {
                            throw new GeneralException(
                                $"Could not transform the AbstractData object into an ObjectKeyValue: [{ad.GetType()}]");
                        }
                        break;
                    default:
                        throw new GeneralException($"Unknown key found: {ad.Key}");
                }
            }

            return pairGenomic;
        }
    }
}
