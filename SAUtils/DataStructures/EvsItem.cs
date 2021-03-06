﻿using System.Text;
using VariantAnnotation.DataStructures.SupplementaryAnnotations;
using VariantAnnotation.FileHandling.JSON;
using VariantAnnotation.Interface;

namespace SAUtils.DataStructures
{
	public sealed class EvsItem:SupplementaryDataItem
	{
		#region members

	    private string RsId { get; }
	    
	    private string AfrFreq { get; }
	    private string AllFreq { get; }
	    private string EurFreq { get; }
	    private string Coverage { get; }
	    private string NumSamples { get; }

		#endregion

		public EvsItem(string chromosome,
			int position,
			string rsId,
			string refAllele,
			string alternateAllele,
			string allFreq,
			string afrFreq,
			string eurFreq,
			string coverage,
			string numSamples
			)
		{
			Chromosome = chromosome;
			Start = position;
			RsId = rsId;
			ReferenceAllele = refAllele;
			AlternateAllele = alternateAllele;
			AfrFreq = afrFreq;
			EurFreq = eurFreq;
			AllFreq = allFreq;
			NumSamples = numSamples;
			Coverage = coverage;

		}



		public override SupplementaryInterval GetSupplementaryInterval(IChromosomeRenamer renamer)
		{
			throw new System.NotImplementedException();
		}



		public override bool Equals(object other)
		{
			// If parameter is null return false.

			// if other cannot be cast into OneKGenItem, return false
			var otherItem = other as EvsItem;
			if (otherItem == null) return false;

			// Return true if the fields match:
			return string.Equals(Chromosome, otherItem.Chromosome)
				&& Start == otherItem.Start
				&& AlternateAllele.Equals(otherItem.AlternateAllele)
				;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = RsId?.GetHashCode() ?? 0;
				hashCode = (hashCode * 397) ^ (ReferenceAllele?.GetHashCode() ?? 0);
				hashCode = (hashCode * 397) ^ (AlternateAllele?.GetHashCode() ?? 0);

				return hashCode;
			}
		}

		public string GetVcfString()
		{
			if (string.IsNullOrEmpty(AllFreq)) return null;

			return $"{AllFreq}|{Coverage}|{NumSamples}";
		}
		public string GetJsonString()
		{
			var sb = new StringBuilder();
			var jsonObject = new JsonObject(sb);

			jsonObject.AddStringValue("sampleCount", NumSamples, false);
			jsonObject.AddStringValue("coverage", Coverage, false);
			jsonObject.AddStringValue("allAf", AllFreq, false);
			jsonObject.AddStringValue("afrAf", AfrFreq, false);
			jsonObject.AddStringValue("eurAf", EurFreq, false);

			return sb.ToString();
		}
	}
}
