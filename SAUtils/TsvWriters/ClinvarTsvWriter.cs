﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SAUtils.DataStructures;
using VariantAnnotation.DataStructures.SupplementaryAnnotations;
using VariantAnnotation.FileHandling.JSON;
using VariantAnnotation.FileHandling.SupplementaryAnnotations;
using VariantAnnotation.Interface;

namespace SAUtils.TsvWriters
{
	public class ClinvarTsvWriter:ISaItemTsvWriter
	{
		#region members
		private readonly SaTsvWriter _writer;
		#endregion

		#region IDisposable
		bool _disposed;

		/// <summary>
		/// public implementation of Dispose pattern callable by consumers. 
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}


		/// <summary>
		/// protected implementation of Dispose pattern. 
		/// </summary>
		private void Dispose(bool disposing)
		{
			if (_disposed)
				return;

			if (disposing)
			{
				// Free any other managed objects here.
				_writer.Dispose();
			}

			// Free any unmanaged objects here.
			//
			_disposed = true;
			// Free any other managed objects here.

		}
		#endregion

		public ClinvarTsvWriter(SaTsvWriter saTsvWriter)
		{
			_writer = saTsvWriter;
		}
	
		public ClinvarTsvWriter(DataSourceVersion version, string outputDirectory, GenomeAssembly genomeAssembly, string refSequencePath) :this(new SaTsvWriter(outputDirectory, version, genomeAssembly.ToString(),
				JsonCommon.ClinvarSchemaVersion, InterimSaCommon.ClinvarTag, InterimSaCommon.ClinvarVcfTag, false, refSequencePath, true))
		{
			Console.WriteLine(version.ToString());
			
		}
		public void WritePosition(IEnumerable<SupplementaryDataItem> saItems)
		{
			if (saItems == null ) return;
			var clinvarItems = new List<ClinVarItem>();
			foreach (var item in saItems)
			{
				var clinvarItem = item as ClinVarItem;
				if (clinvarItem == null)
					throw new InvalidDataException("Expected ClinvarItems list!!");
				clinvarItems.Add(clinvarItem);
			}

			if (clinvarItems.Count == 0) return;
            var alleleGroupDict = GroupByAltAllele(clinvarItems);
            foreach (var kvp in alleleGroupDict)
            {
                var refAllele = kvp.Key.Item1;
                var altAllele = kvp.Key.Item2;

                var groupedItems = kvp.Value;
                var vcfString = string.Join(",", Enumerable.Select(groupedItems.OrderBy(x => x.ID), x => SupplementaryAnnotationUtilities.ConvertToVcfInfoString(x.Significance)));
                var jsonStrings = groupedItems.OrderBy(x => x.ID).Select(x => x.GetJsonString()).ToList();

                var firstItem = groupedItems[0];
                _writer.AddEntry(firstItem.Chromosome,
                    firstItem.Start,
                    refAllele,
                    altAllele, vcfString, jsonStrings);
            }

   //         var alleleGroupedItems = clinvarItems.GroupBy(x => x.AlternateAllele);
			//foreach (var groupedItem in alleleGroupedItems)
			//{
			//	var uniqueItems = groupedItem.GroupBy(p => p.ID).Select(x => x.First()).ToList();
			//	var vcfString = string.Join(",", uniqueItems.Select(x => SupplementaryAnnotationUtilities.ConvertToVcfInfoString(x.Significance)));
			//	var jsonStrings = uniqueItems.Select(x => x.GetJsonString()).ToList();

			//	// since the reference allele for different items in the group may be different, we only use the first base as it is supposed to be the common padding base.
			//	_writer.AddEntry(groupedItem.First().Chromosome,
			//		groupedItem.First().Start,
			//		groupedItem.First().ReferenceAllele, 
			//		groupedItem.Key, vcfString, jsonStrings);
			//}
			

		}

        private Dictionary<Tuple<string, string>, List<ClinVarItem>> GroupByAltAllele(List<ClinVarItem> clinVarItems)
        {
            var groups = new Dictionary<Tuple<string, string>, List<ClinVarItem>>();

            foreach (var clinVarItem in clinVarItems)
            {
                var alleleTuple = Tuple.Create(clinVarItem.ReferenceAllele, clinVarItem.AlternateAllele);
                if (groups.ContainsKey(alleleTuple))
                    groups[alleleTuple].Add(clinVarItem);
                else groups[alleleTuple] = new List<ClinVarItem> { clinVarItem };
            }

            return groups;
        }

    }
}
