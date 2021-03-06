﻿using System;
using System.Collections.Generic;
using System.IO;
using SAUtils.DataStructures;
using VariantAnnotation.FileHandling.JSON;
using VariantAnnotation.FileHandling.SupplementaryAnnotations;
using VariantAnnotation.Interface;

namespace SAUtils.TsvWriters
{
	public class EvsTsvWriter : ISaItemTsvWriter
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

		public EvsTsvWriter(DataSourceVersion version, string outputDirectory, GenomeAssembly genomeAssembly, string refSequencePath):this(new SaTsvWriter(outputDirectory, version, genomeAssembly.ToString(),
				JsonCommon.OneKgenSchemaVersion, InterimSaCommon.EvsTag, InterimSaCommon.EvsVcfTag, true, refSequencePath))
		{
			Console.WriteLine(version.ToString());
		}

		public EvsTsvWriter(SaTsvWriter writer)
		{
			_writer = writer;
		}

		public void WritePosition(IEnumerable<SupplementaryDataItem> saItems)
		{
			if (saItems == null) return;

			var evsItems = new List<EvsItem>();
			foreach (var item in saItems)
			{
				var evsItem = item as EvsItem;
				if (evsItem == null)
					throw new InvalidDataException("Expected EvsItems list!!");
				evsItems.Add(evsItem);
			}

		    SupplementaryDataItem.RemoveConflictedAlleles(evsItems);

            foreach (var evsItem in evsItems)
			{
				_writer.AddEntry(evsItem.Chromosome, evsItem.Start, evsItem.ReferenceAllele, evsItem.AlternateAllele, evsItem.GetVcfString(),
					new List<string> {evsItem.GetJsonString()});
			}
		}


    }
}

