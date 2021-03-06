﻿using System;
using System.Collections.Generic;
using System.IO;
using Jasix.DataStructures;
using Newtonsoft.Json;

namespace Jasix
{
	public class QueryProcessor:IDisposable
	{
		#region members
		private readonly StreamReader _jsonReader;
		private readonly Stream _indexStream;
		private readonly JasixIndex _jasixIndex;

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
				_jsonReader.Dispose();
				_indexStream.Dispose();
			}

			// Free any unmanaged objects here.
			//
			_disposed = true;
			// Free any other managed objects here.

		}
		#endregion

		public QueryProcessor(StreamReader jsonReader, Stream indexStream)
		{
			_jsonReader  = jsonReader;
			_indexStream = indexStream;
			_jasixIndex  = new JasixIndex(_indexStream);

		}

		public string GetHeader()
		{
			return _jasixIndex.HeaderLine;
		}

		
		public void PrintChromosomeList()
		{
			foreach (var chrName in _jasixIndex.GetChromosomeList())
			{
				Console.WriteLine(chrName);
			}
		}

		public void PrintHeader()
		{

			var headerString = _jasixIndex.HeaderLine;
			Console.WriteLine("{" + headerString+"}");
		}

		
		public void ProcessQuery(string queryString, bool printHeader = false)
		{
			var query = Utilities.ParseJasixPosition(queryString);
			
			Console.Write("{");
			if (printHeader)
			{
				var headerString = _jasixIndex.HeaderLine;
				Console.Write(headerString + ",");
			}
			Utilities.PrintQuerySectionOpening(JasixCommons.SectionToIndex);

			var needComma = PrintLargeVariantsExtendingIntoQuery(query);
			PrintAllVariantsFromQueryBegin(query,needComma);

			Utilities.PrintQuerySectionClosing();
			Console.WriteLine("}");

		}

		private void PrintAllVariantsFromQueryBegin(Tuple<string, int, int> query, bool needComma)
		{
			foreach (var line in ReadOverlappingJsonLines(query))
			{
				Utilities.PrintJsonEntry(line, needComma);
				needComma = true;
			}

		}
		private bool PrintLargeVariantsExtendingIntoQuery(Tuple<string, int, int> query)
		{
			var needComma = false;
			foreach (var line in ReadJsonLinesExtendingInto(query))
			{
				Utilities.PrintJsonEntry(line, needComma);
				needComma = true;
			}

			return needComma;
		}

		internal IEnumerable<string> ReadJsonLinesExtendingInto(Tuple<string, int, int> query)
		{
			// query for large variants like chr1:100-99 returns all overlapping large variants that start before 100
			var locations = _jasixIndex.LargeVariantPositions(query.Item1, query.Item2, query.Item2 - 1);

			if (locations == null || locations.Count == 0) yield break;

			foreach (var location in locations)
			{
				RepositionReader(location);

				string line;
				if ((line = _jsonReader.ReadLine()) == null) continue;
				line = line.TrimEnd(',');

				yield return line;

			}
		}

		private void RepositionReader(long location)
		{
			_jsonReader.DiscardBufferedData();
			_jsonReader.BaseStream.Position = location;
		}

		internal IEnumerable<string> ReadOverlappingJsonLines(Tuple<string, int, int> query)
		{
			var position = _jasixIndex.GetFirstVariantPosition(query.Item1, query.Item2, query.Item3);

			if (position == -1) yield break;

			RepositionReader(position);

			string line;
			while ((line = _jsonReader.ReadLine()) != null && !line.StartsWith("]"))
				//The array of positions entry end with "]," Going past it will cause the json parser to crash
			{
				line = line.TrimEnd(',');
				JsonSchema jsonEntry;
				try
				{
					jsonEntry = JsonConvert.DeserializeObject<JsonSchema>(line);
				}
				catch (Exception)
				{
					Console.WriteLine($"Error in line:\n{line}");
					throw;
				}

				if (jsonEntry.chromosome != query.Item1) break;

				jsonEntry.Start = jsonEntry.position;
				jsonEntry.End = Utilities.GetJsonEntryEnd(jsonEntry);

				if (jsonEntry.Start > query.Item3) break;

				if (!jsonEntry.Overlaps(query.Item2, query.Item3)) continue;
				// if there is an SV that starts before the query start that is printed by the large variant printer
				if (Utilities.IsLargeVariant(jsonEntry.Start, jsonEntry.End) && jsonEntry.Start < query.Item2) continue;
				yield return line;
			}
		}

		
	}
}
